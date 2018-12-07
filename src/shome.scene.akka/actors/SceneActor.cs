using Akka.Actor;
using Microsoft.Extensions.Logging;

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
        }

        protected override void PreStart()
        {
            //todo self subscribe to pubsub ?
            base.PreStart();
            _logger.LogDebug($"SceneActor - '{this.Self.Path.Name}' starting");
        }

        protected override void PostStop()
        {
            Context.ActorSelection(_knownPaths.PubSubActorPath).Tell(new PubSubActor.UnSub{Subscriber = Self});
            _logger.LogDebug($"SceneActor - '{this.Self.Path.Name}' shutdown");
            base.PostStop();
        }
    }

}
