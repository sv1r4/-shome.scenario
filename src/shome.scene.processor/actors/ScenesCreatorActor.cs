using Akka.Actor;
using Microsoft.Extensions.Logging;

namespace shome.scene.processor.actors
{
    public class ScenesCreatorActor: ReceiveActor
    {
        public ScenesCreatorActor(ILogger<ScenesCreatorActor> logger)
        {
            Receive<string>(s =>
            {
                logger.LogDebug($"dotnetlog {s}"); 
            });
        }
    }
}
