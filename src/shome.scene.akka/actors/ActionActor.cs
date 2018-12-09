using Akka.Actor;
using Microsoft.Extensions.Logging;

namespace shome.scene.akka.actors
{
    public class ActionActor:ReceiveActor
    {
        private readonly ILogger _logger;

        public ActionActor(ILogger<ActionActor> logger)
        {
            _logger = logger;
        }
    }
}
