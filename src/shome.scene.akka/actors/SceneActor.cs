using Akka.Actor;
using Microsoft.Extensions.Logging;

namespace shome.scene.akka.actors
{
    public class SceneActor:ReceiveActor //todo FSM actor
    {
        private readonly ILogger _logger;

        public SceneActor(ILogger<SceneActor> logger)
        {
            _logger = logger;
            Receive<PubSubActor.MqttReceivedMessage>(e =>
            {
                _logger.LogDebug($"todo handle message {e.Topic}@{e.Message}");
            });
        }
    }
}
