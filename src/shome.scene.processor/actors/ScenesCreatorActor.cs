using Akka.Actor;
using Microsoft.Extensions.Logging;
using shome.scene.core.model;

namespace shome.scene.processor.actors
{
    public class ScenesCreatorActor: ReceiveActor
    {
        public ScenesCreatorActor(ILogger<ScenesCreatorActor> logger)
        {
            Receive<SceneConfig>(s =>
            {
                logger.LogDebug($"SceneConfig received {s}"); 
            });
        }
    }
}
