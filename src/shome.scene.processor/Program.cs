using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.DI.Core;
using Akka.DI.Microsoft;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using shome.scene.akka;
using shome.scene.akka.actors;
using shome.scene.core.contract;
using shome.scene.mqtt.config;
using shome.scene.mqtt.contract;
using shome.scene.processor.mqtt;
using shome.scene.provider.yml;
using shome.scene.provider.yml.config;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace shome.scene.processor
{
    public class Program
    {
        private static ILogger _logger;
        private static IManagedMqttClient _mqtt;
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

                _actorSystem = InitActorSystem();
                
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
                services.AddSingleton<IManagedMqttClient>(sp=>
                {
                    _mqtt = InitMqtt(cfgMqtt).GetAwaiter().GetResult();
                    return _mqtt;
                });
                
                services.AddSingleton<ActorSystem>(_actorSystem);
                services.AddSingleton<IFileProvider>(_fileProvider);
                services.AddSingleton<Deserializer>(new DeserializerBuilder()
                    .WithNamingConvention(new CamelCaseNamingConvention())
                    .Build());
                // ReSharper restore RedundantTypeArgumentsOfMethod

                services.AddTransient<IMqttBasicClient, MqttNetAdapter>();
                services.Scan(scan =>
                    scan.FromAssembliesOf(typeof(SceneCreatorActor))
                        .AddClasses(x=>x.AssignableTo<ReceiveActor>())
                        .AsSelf()
                        .WithScopedLifetime());
                services.AddSingleton(new KnownPaths());

                var serviceProvider = services.BuildServiceProvider();

                #endregion

                InitActorSystemDI(_actorSystem, serviceProvider);

                //initial read

                _actorConfigReader = _actorSystem.ActorOf(_actorSystem.DI().Props<SceneConfigReaderActor>());
                _actorPubSub = _actorSystem.ActorOf(_actorSystem.DI().Props<PubSubActor>());
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

        private static ActorSystem InitActorSystem()
        {
            //todo config akka
            #region config

            var config = ConfigurationFactory.ParseString(@"
  akka : {
    version : ""0.0.1 Akka""
    home : 
    loggers : [Akka.Event.DefaultLogger]
    loggers-dispatcher : akka.actor.default-dispatcher
    logger-startup-timeout : 5s
    loglevel : DEBUG
    suppress-json-serializer-warning : on
    stdout-loglevel : DEBUG
    log-config-on-start : off
    log-dead-letters : 10
    log-dead-letters-during-shutdown : on
    extensions : []
    daemonic : off
    actor : {
      provider : Akka.Actor.LocalActorRefProvider
      guardian-supervisor-strategy : Akka.Actor.DefaultSupervisorStrategy
      creation-timeout : 20s
      reaper-interval : 5
      serialize-messages : off
      serialize-creators : off
      unstarted-push-timeout : 10s
      ask-timeout : infinite
      typed : {
        timeout : 5
      }
      inbox : {
        inbox-size : 1000
        default-timeout : 5s
      }
      router : {
        type-mapping : {
          from-code : Akka.Routing.NoRouter
          round-robin-pool : Akka.Routing.RoundRobinPool
          round-robin-group : Akka.Routing.RoundRobinGroup
          random-pool : Akka.Routing.RandomPool
          random-group : Akka.Routing.RandomGroup
          smallest-mailbox-pool : Akka.Routing.SmallestMailboxPool
          broadcast-pool : Akka.Routing.BroadcastPool
          broadcast-group : Akka.Routing.BroadcastGroup
          scatter-gather-pool : Akka.Routing.ScatterGatherFirstCompletedPool
          scatter-gather-group : Akka.Routing.ScatterGatherFirstCompletedGroup
          consistent-hashing-pool : Akka.Routing.ConsistentHashingPool
          consistent-hashing-group : Akka.Routing.ConsistentHashingGroup
          tail-chopping-pool : Akka.Routing.TailChoppingPool
          tail-chopping-group : Akka.Routing.TailChoppingGroup
        }
      }
      deployment : {
        default : {
          dispatcher : 
          mailbox : 
          router : from-code
          nr-of-instances : 1
          within : ""5 s""
          virtual-nodes-factor : 10
          routees : {
            paths : []
          }
          resizer : {
            enabled : off
            lower-bound : 1
            upper-bound : 10
            pressure-threshold : 1
            rampup-rate : 0.2
            backoff-threshold : 0.3
            backoff-rate : 0.1
            messages-per-resize : 10
          }
        }
      }
      synchronized-dispatcher : {
        type : SynchronizedDispatcher
        executor : current-context-executor
        throughput : 10
      }
      task-dispatcher : {
        type : TaskDispatcher
        executor : task-executor
        throughput : 30
      }
      default-fork-join-dispatcher : {
        type : ForkJoinDispatcher
        executor : fork-join-executor
        throughput : 30
        dedicated-thread-pool : {
          thread-count : 3
          threadtype : background
        }
      }
      default-dispatcher : {
        type : Dispatcher
        executor : default-executor
        default-executor : {
        }
        thread-pool-executor : {
        }
        fork-join-executor : {
          dedicated-thread-pool : {
            thread-count : 3
            threadtype : background
          }
        }
        current-context-executor : {
        }
        shutdown-timeout : 1s
        throughput : 30
        throughput-deadline-time : 0ms
        attempt-teamwork : on
        mailbox-requirement : 
      }
      default-mailbox : {
        mailbox-type : Akka.Dispatch.UnboundedMailbox
        mailbox-capacity : 1000
        mailbox-push-timeout-time : 10s
        stash-capacity : -1
      }
      mailbox : {
        requirements : {
          Akka.Dispatch.IUnboundedMessageQueueSemantics : akka.actor.mailbox.unbounded-queue-based
          Akka.Dispatch.IBoundedMessageQueueSemantics : akka.actor.mailbox.bounded-queue-based
          Akka.Dispatch.IDequeBasedMessageQueueSemantics : akka.actor.mailbox.unbounded-deque-based
          Akka.Dispatch.IUnboundedDequeBasedMessageQueueSemantics : akka.actor.mailbox.unbounded-deque-based
          Akka.Dispatch.IBoundedDequeBasedMessageQueueSemantics : akka.actor.mailbox.bounded-deque-based
          Akka.Dispatch.IMultipleConsumerSemantics : akka.actor.mailbox.unbounded-queue-based
          Akka.Event.ILoggerMessageQueueSemantics : akka.actor.mailbox.logger-queue
        }
        unbounded-queue-based : {
          mailbox-type : Akka.Dispatch.UnboundedMailbox
        }
        bounded-queue-based : {
          mailbox-type : Akka.Dispatch.BoundedMailbox
        }
        unbounded-deque-based : {
          mailbox-type : Akka.Dispatch.UnboundedDequeBasedMailbox
        }
        bounded-deque-based : {
          mailbox-type : Akka.Dispatch.BoundedDequeBasedMailbox
        }
        logger-queue : {
          mailbox-type : Akka.Event.LoggerMailboxType
        }
      }
      debug : {
        receive : off
        autoreceive : off
        lifecycle : off
        fsm : off
        event-stream : off
        unhandled : off
        router-misconfiguration : off
      }
      serializers : {
        json : ""Akka.Serialization.NewtonSoftJsonSerializer, Akka""
        bytes : ""Akka.Serialization.ByteArraySerializer, Akka""
      }
      serialization-bindings : {
        System.Byte[] : bytes
        System.Object : json
      }
      serialization-identifiers : {
        ""Akka.Serialization.ByteArraySerializer, Akka"" : 4
        ""Akka.Serialization.NewtonSoftJsonSerializer, Akka"" : 1
      }
      serialization-settings : {
      }
    }
    scheduler : {
      tick-duration : 10ms
      ticks-per-wheel : 512
      implementation : Akka.Actor.HashedWheelTimerScheduler
      shutdown-timeout : 5s
    }
    io : {
      pinned-dispatcher : {
        type : PinnedDispatcher
        executor : fork-join-executor
      }
      tcp : {
        direct-buffer-pool : {
          class : ""Akka.IO.Buffers.DirectBufferPool, Akka""
          buffer-size : 512
          buffers-per-segment : 500
          initial-segments : 1
          buffer-pool-limit : 1024
        }
        buffer-pool : akka.io.tcp.direct-buffer-pool
        nr-of-socket-async-event-args : 32
        max-channels : 256000
        selector-association-retries : 10
        batch-accept-limit : 10
        register-timeout : 5s
        max-received-message-size : unlimited
        trace-logging : off
        selector-dispatcher : akka.io.pinned-dispatcher
        worker-dispatcher : akka.actor.default-dispatcher
        management-dispatcher : akka.actor.default-dispatcher
        file-io-dispatcher : akka.actor.default-dispatcher
        file-io-transferTo-limit : 524288
        finish-connect-retries : 5
        windows-connection-abort-workaround-enabled : off
      }
      udp : {
        direct-buffer-pool : {
          class : ""Akka.IO.Buffers.DirectBufferPool, Akka""
          buffer-size : 512
          buffers-per-segment : 500
          initial-segments : 1
          buffer-pool-limit : 1024
        }
        buffer-pool : akka.io.udp.direct-buffer-pool
        nr-of-socket-async-event-args : 32
        max-channels : 4096
        select-timeout : infinite
        selector-association-retries : 10
        receive-throughput : 3
        direct-buffer-size : 18432
        direct-buffer-pool-limit : 1000
        received-message-size-limit : unlimited
        trace-logging : off
        selector-dispatcher : akka.io.pinned-dispatcher
        worker-dispatcher : akka.actor.default-dispatcher
        management-dispatcher : akka.actor.default-dispatcher
      }
      udp-connected : {
        direct-buffer-pool : {
          class : ""Akka.IO.Buffers.DirectBufferPool, Akka""
          buffer-size : 512
          buffers-per-segment : 500
          initial-segments : 1
          buffer-pool-limit : 1024
        }
        buffer-pool : akka.io.udp-connected.direct-buffer-pool
        nr-of-socket-async-event-args : 32
        max-channels : 4096
        select-timeout : infinite
        selector-association-retries : 10
        receive-throughput : 3
        direct-buffer-size : 18432
        direct-buffer-pool-limit : 1000
        received-message-size-limit : unlimited
        trace-logging : off
        selector-dispatcher : akka.io.pinned-dispatcher
        worker-dispatcher : akka.actor.default-dispatcher
        management-dispatcher : akka.actor.default-dispatcher
      }
      dns : {
        dispatcher : akka.actor.default-dispatcher
        resolver : inet-address
        inet-address : {
          provider-object : Akka.IO.InetAddressDnsProvider
          positive-ttl : 30s
          negative-ttl : 10s
          cache-cleanup-interval : 120s
        }
      }
    }
    coordinated-shutdown : {
      default-phase-timeout : ""5 s""
      terminate-actor-system : on
      exit-clr : off
      run-by-clr-shutdown-hook : on
      phases : {
        before-service-unbind : {
        }
        service-unbind : {
          depends-on : [before-service-unbind]
        }
        service-requests-done : {
          depends-on : [service-unbind]
        }
        service-stop : {
          depends-on : [service-requests-done]
        }
        before-cluster-shutdown : {
          depends-on : [service-stop]
        }
        cluster-sharding-shutdown-region : {
          timeout : ""10 s""
          depends-on : [before-cluster-shutdown]
        }
        cluster-leave : {
          depends-on : [cluster-sharding-shutdown-region]
        }
        cluster-exiting : {
          timeout : ""10 s""
          depends-on : [cluster-leave]
        }
        cluster-exiting-done : {
          depends-on : [cluster-exiting]
        }
        cluster-shutdown : {
          depends-on : [cluster-exiting-done]
        }
        before-actor-system-terminate : {
          depends-on : [cluster-shutdown]
        }
        actor-system-terminate : {
          timeout : ""10 s""
          depends-on : [before-actor-system-terminate]
        }
      }
    }
  }

")
                .WithFallback(ConfigurationFactory.Default());

            #endregion
            return ActorSystem.Create("shome-scene-actor-system", config);
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
            _actorSystem.ActorSelection(_knownPaths.PubSubActorPath).Tell(new PubSubActor.MqttReceivedMessage
            {
                Topic = e.ApplicationMessage.Topic,
                Message = strMessage
            });
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
