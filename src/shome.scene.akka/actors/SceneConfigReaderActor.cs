using Akka.Actor;
using Akka.DI.Core;
using Microsoft.Extensions.Logging;
using shome.scene.core.contract;

namespace shome.scene.akka.actors
{
    public class SceneConfigReaderActor : ReceiveActor
    {
        
        public SceneConfigReaderActor(ILogger<SceneConfigReaderActor> logger, ISceneProvider sceneProvider)
        {
            Receive<GetScenesConfig>(m =>
            {
                logger.LogDebug("GetScenesConfig received");
                foreach (var scene in sceneProvider.GetConfigs())
                {
                    Context.ActorOf(Context.DI().Props<ScenesCreatorActor>()).Tell(scene);
                }
            });
        }

        public class GetScenesConfig
        {
        }

        
    }
}
