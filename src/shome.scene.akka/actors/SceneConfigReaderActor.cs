using System;
using System.Linq;
using Akka.Actor;
using Akka.DI.Core;
using Microsoft.Extensions.Logging;
using shome.scene.core.contract;

namespace shome.scene.akka.actors
{
    public class SceneConfigReaderActor : ReceiveActor
    {
        private readonly IActorRef _creatorActor;

        public SceneConfigReaderActor(ILogger<SceneConfigReaderActor> logger, ISceneProvider sceneProvider)
        {
            Receive<GetScenesConfig>(m =>
            {
                foreach (var scene in sceneProvider.GetConfigs())
                {
                    _creatorActor.Tell(scene);
                }
            });

            logger.LogDebug($"Create scene Creator {nameof(SceneCreatorActor)}");
            var props = Context.DI().Props<SceneCreatorActor>();
            _creatorActor = Context.ActorOf(props);
        }

        public class GetScenesConfig
        {
        }
    }
}
