using System.Linq;
using Akka.Actor;
using Microsoft.Extensions.Logging;
using shome.scene.core.model;

namespace shome.scene.akka.actors
{
    public class SceneActor:ReceiveActor //todo FSM actor
    {
        private readonly ILogger _logger;
        private readonly KnownPaths _knownPaths;

        public SceneActor(ILogger<SceneActor> logger, KnownPaths knownPaths)
        {
            _logger = logger;
            _knownPaths = knownPaths;

            Receive<PubSubActor.MqttReceivedMessage>(e =>
            {
                _logger.LogDebug($"todo handle message {e.Topic}@{e.Message}");
            });

            Receive<Init>(e =>
            {
                foreach (var topic in e.Config.Actions.SelectMany(x => x.If).Select(x => x.Topic))
                {
                    _logger.LogDebug($"Tell Subscribe to topic='{topic}'");
                    //subscribe scene actor for messages
                    Context.System.ActorSelection(_knownPaths.PubSubActorPath).Tell(new PubSubActor.SubscriptionMqtt
                    {
                        Topic = topic,
                        Subscriber = Self
                    });
                }
            });
        }

        protected override void PreStart()
        {
            base.PreStart();
            _logger.LogDebug($"SceneActor - '{this.Self.Path.Name}' starting");
        }

        protected override void PostStop()
        {
            Context.ActorSelection(_knownPaths.PubSubActorPath).Tell(new PubSubActor.UnSub{Actor = Self});
            _logger.LogDebug($"SceneActor - '{this.Self.Path.Name}' shutdown");
            base.PostStop();
        }

        public class Init
        {
            public SceneConfig Config { get; set; }
        }

        public class Trigger
        {

        }
    }

}
