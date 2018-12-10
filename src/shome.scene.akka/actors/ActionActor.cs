using Akka.Actor;
using Microsoft.Extensions.Logging;

namespace shome.scene.akka.actors
{
    public class ActionActor:ReceiveActor
    {
        private readonly ILogger _logger;
        private readonly KnownPaths _knownPaths;

        public ActionActor(ILogger<ActionActor> logger, KnownPaths knownPaths)
        {
            _logger = logger;
            _knownPaths = knownPaths;
        }


       
    }
}
