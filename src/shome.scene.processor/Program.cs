using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using shome.scene.mqtt.config;
using shome.scene.mqtt.contract;
using shome.scene.processor.mqtt;
using shome.scene.provider.contract;
using shome.scene.provider.yml;
using shome.scene.provider.yml.config;

namespace shome.scene.processor
{
    public class Program
    {
        private static ILogger _logger;
        private static IManagedMqttClient _mqtt;
        private static IFileProvider _fileProvider;
        private static ActorSystem _actorSystem;

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

        private static async Task Stop()
        {
            if (_mqtt != null)
            {
                await _mqtt.StopAsync();
            }
            _actorSystem?.Dispose();
        }
        
        private static async Task Start(CancellationToken cancellationToken)
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

                _actorSystem = InitActorSystem();
                _mqtt = await InitMqtt(cfgMqtt);
                _fileProvider = await InitYamlFileProvider(cfgYaml, cancellationToken);

                #endregion

                #region services

                var services = new ServiceCollection();
                services.AddLogging(logging =>
                {
                    logging.AddConfiguration(config.GetSection("Logging"));
                    logging.AddConsole();
                });
                services.AddSingleton<ISceneProvider, YamlSceneProvider>();
                // ReSharper disable RedundantTypeArgumentsOfMethod
                services.AddSingleton<IFileProvider>(_fileProvider);
                services.AddSingleton<IManagedMqttClient>(_mqtt);
                services.AddSingleton<ActorSystem>(_actorSystem);
                // ReSharper restore RedundantTypeArgumentsOfMethod
                services.AddTransient<IMqttBasicClient, MqttNetAdapter>();

                var sp = services.BuildServiceProvider();

                #endregion

                _logger = sp.GetService<ILogger<Program>>();
                _logger.LogInformation("Scene Processor Start");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        #region infrastructure akka

        private static ActorSystem InitActorSystem()
        {
            return ActorSystem.Create($"shome-scene-actor-system");
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
            _logger.LogDebug($"message received.\n\ttopic='{e.ApplicationMessage.Topic}'\n\tmessage='{Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}'");
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

                _logger.LogDebug("changed");
            }
            // ReSharper disable once FunctionNeverReturns - expected infinit task
        }

        #endregion
    }

}
