using System;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.DI.Core;
using Akka.DI.Microsoft;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using Quartz;
using Quartz.Impl;
using shome.scene.akka;
using shome.scene.akka.actors;
using shome.scene.akka.util;
using shome.scene.akka.util.quartz;
using shome.scene.core.contract;
using shome.scene.core.events;
using shome.scene.mqtt.core.config;
using shome.scene.mqtt.core.contract;
using shome.scene.mqtt.mqttnet;
using shome.scene.processor.quartz;
using shome.scene.provider.yml;
using shome.scene.provider.yml.config;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using SchedulerException = Akka.Actor.SchedulerException;

namespace shome.scene.processor
{
    public class Program
    {
        private static ILogger _logger;
        private static IManagedMqttClient _mqtt;
        private static Quartz.IScheduler _quartzScheduler;
        private static IFileProvider _fileProvider;
        private static ActorSystem _actorSystem;
        private static IActorRef _actorConfigReader;
        private static IActorRef _actorPubSub;

        private static KnownPaths _knownPaths;

        public static void Main()
        {
            using (var cts = new CancellationTokenSource())
            {
                var token = cts.Token;
                Console.CancelKeyPress += (sender, args) =>
                {
                    Console.WriteLine("App graceful stop");
                    args.Cancel = true;
                    try
                    {
                        Stop().GetAwaiter().GetResult();
                    }
                    finally
                    {
                        // ReSharper disable once AccessToDisposedClosure
                        cts.Cancel();
                    }
                };
                Console.WriteLine("App start");

                Start(token).GetAwaiter().GetResult();
                token.WaitHandle.WaitOne();
            }
        }

        public static async Task Stop()
        {
            if (_mqtt != null)
            {
                await _mqtt.StopAsync();
            }

            if (_quartzScheduler != null)
            {
                try
                {
                    await _quartzScheduler.Shutdown();
                }
                catch (SchedulerException)
                {

                }
            }

            _actorSystem?.Dispose();
        }

        public static async Task Start(CancellationToken cancellationToken)
        {
            try
            {
                #region Configuration

                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables();
                var config = builder.Build();
                var cfgYaml = config.GetSection(nameof(SceneYamlConfig)).Get<SceneYamlConfig>();
                var cfgMqtt = config.GetSection(nameof(MqttConfig)).Get<MqttConfig>();

                #endregion

                #region infrastructure

                _actorSystem = InitActorSystem(config);

                _fileProvider = await InitYamlFileProvider(cfgYaml, cancellationToken);

                #endregion

                #region services
                // ReSharper disable RedundantTypeArgumentsOfMethod

                var services = new ServiceCollection();

                #region log
                services.AddLogging(logging =>
                {
                    logging.AddConfiguration(config.GetSection("Logging"));
                    logging.AddConsole();
                });
                #endregion
                #region yaml
                services.AddSingleton<ISceneProvider, YamlSceneProvider>();
                services.AddSingleton<Deserializer>(new DeserializerBuilder()
                    .WithNamingConvention(new CamelCaseNamingConvention())
                    .Build());
                services.AddSingleton<IFileProvider>(_fileProvider);
                #endregion
                #region mqtt
                services.AddSingleton<IManagedMqttClient>(sp =>
                {
                    _mqtt = InitMqtt(cfgMqtt).GetAwaiter().GetResult();
                    return _mqtt;
                });
                services.AddTransient<IMqttBasicClient, MqttNetAdapter>();
                #endregion
                #region quartz
                services.Scan(scan =>
                    scan.FromAssembliesOf(typeof(TellScheduleJob))
                        .AddClasses(x => x.AssignableTo<IJob>())
                        .AsSelf()
                        .WithTransientLifetime());
                services.AddSingleton<ISceneActionScheduler>(sp =>
                {
                    _quartzScheduler = InitQuartz(sp).GetAwaiter().GetResult();
                    return new QuartzActionScheduler(_quartzScheduler, sp.GetService<ILogger<QuartzActionScheduler>>());
                });

                #endregion
                #region akka
                services.AddSingleton<ActorSystem>(_actorSystem);
                
                services.Scan(scan =>
                    scan.FromAssembliesOf(typeof(SceneCreatorActor))
                        .AddClasses(x => x.AssignableTo<ReceiveActor>())
                        .AsSelf()
                        .WithScopedLifetime());

                services.AddSingleton(new KnownPaths());
                #endregion

                var serviceProvider = services.BuildServiceProvider();
                
                // ReSharper restore RedundantTypeArgumentsOfMethod
                #endregion

                InitActorSystemDI(_actorSystem, serviceProvider);

                //initial read
                _actorConfigReader = _actorSystem.ActorOf(_actorSystem.DI().Props<SceneConfigReaderActor>());
                _actorPubSub = _actorSystem.ActorOf(_actorSystem.DI().Props<PubSubProxyActor>());
                _actorConfigReader.Tell(new SceneConfigReaderActor.GetScenesConfig());
                _knownPaths = serviceProvider.GetRequiredService<KnownPaths>();
                _knownPaths.PubSubActorPath = _actorPubSub.Path;

                _logger = serviceProvider.GetService<ILogger<Program>>();
                _logger.LogInformation("Scene Processor Start");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        #region infrastructure akka

        private static ActorSystem InitActorSystem(IConfigurationRoot config)
        {
            return ActorSystem.Create("shome-scene-actor-system", new AkkaConfigAdapter(config).GetAkkaConfig());
        }

        // ReSharper disable once UnusedMethodReturnValue.Local
        private static IDependencyResolver InitActorSystemDI(ActorSystem actorSystem, IServiceProvider sp)
        {
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
            return new MicrosoftDependencyResolver(scopeFactory, actorSystem);            
        }

        #endregion

        #region imfrastructure mqtt

        private static async Task<IManagedMqttClient> InitMqtt(MqttConfig cfg)
        {
            var optionBuilder = new MqttClientOptionsBuilder()
                .WithClientId($"shome.scene.processor-{Guid.NewGuid()}")
                .WithTcpServer(cfg.Host, cfg.Port);

            if (!string.IsNullOrWhiteSpace(cfg.User))
            {
                optionBuilder = optionBuilder.WithCredentials(cfg.User, cfg.Password);
            }

            var options = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(optionBuilder.Build())
                .Build();

            var mqttClient = new MqttFactory().CreateManagedMqttClient();
            //await mqttClient.SubscribeAsync(new TopicFilterBuilder().WithTopic("#").WithAtLeastOnceQoS().Build());
            mqttClient.Connected += (sender, args) =>
            {
                _logger.LogInformation("mqtt connected"); 
            };

            mqttClient.ConnectingFailed += (sender, args) =>
            {
                _logger.LogError($"mqtt connection fail. Error {args.Exception}");
            };
            mqttClient.Disconnected += (sender, args) =>
            {
                _logger.LogWarning($"mqtt disconnected. Error {args.Exception}");
            };
            mqttClient.SynchronizingSubscriptionsFailed += (sender, args) =>
            {
                _logger.LogError($"mqtt sync subscriptions fail. Error {args.Exception}");
            };

            mqttClient.ApplicationMessageReceived +=MqttClientOnApplicationMessageReceived; 

            await mqttClient.StartAsync(options);
            return mqttClient;
        }

        private static void MqttClientOnApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs e)
        {
            var strMessage = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
            _logger.LogDebug($"message received.\n\ttopic='{e.ApplicationMessage.Topic}'\n\tmessage='{strMessage}'");
            _actorSystem.ActorSelection(_knownPaths.PubSubActorPath).Tell(new MqttMessageEvent
            {
                Topic = e.ApplicationMessage.Topic,
                Message = strMessage
            });
        }

        #endregion

        #region imfrastructure quartz

        private static async Task<Quartz.IScheduler> InitQuartz(IServiceProvider serviceProvider)
        {
            var props = new NameValueCollection
            {
                { "quartz.serializer.type", "binary" }
            };
            var factory = new StdSchedulerFactory(props);
            Quartz.IScheduler scheduler = await factory.GetScheduler();
            scheduler.JobFactory = new DiJobFactory(serviceProvider);
            // and start it off
            await scheduler.Start();

            return scheduler;
        }

        #endregion

        #region infrastructure yaml 

        private static Task<IFileProvider> InitYamlFileProvider(SceneYamlConfig cfg,CancellationToken cancellationToken)
        {
            IFileProvider fp = new PhysicalFileProvider(cfg.DirectoryAbsolute);
            Task.Run(async () => await InfinitMonitorChanges(fp, "*.yaml"), cancellationToken);
            return Task.FromResult(fp);
        }

        private static async Task InfinitMonitorChanges(IFileProvider fp, string path)
        {
            while (true)
            {
                var w = fp.Watch(path);
                var tcs = new TaskCompletionSource<object>();
                w.RegisterChangeCallback(state =>
                {
                    ((TaskCompletionSource<object>)state).TrySetResult(null);
                }, tcs);
                await tcs.Task.ConfigureAwait(false);

                _actorConfigReader.Tell(new SceneConfigReaderActor.GetScenesConfig());
                _logger.LogDebug("changed");
            }
            // ReSharper disable once FunctionNeverReturns - expected infinit task
        }

        #endregion
    }

}
