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
        private readonly ILogger _logger;
        private IActorRef _creatorActor;

        protected override void PreStart()
        {

            _logger.LogDebug($"Start {nameof(SceneConfigReaderActor)}");
            _logger.LogDebug($"Create!! {nameof(SceneCreatorActor)}");

            var props = Context.DI().Props<SceneCreatorActor>();
            _creatorActor = Context.ActorOf(props);
        }

        // Overriding postRestart to disable the call to preStart() after restarts
        protected override void PostRestart(Exception reason)
        {
        }

        // The default implementation of PreRestart() stops all the children
        // of the actor. To opt-out from stopping the children, we
        // have to override PreRestart()
        protected override void PreRestart(Exception reason, object message)
        {
            // Keep the call to PostStop(), but no stopping of children
            PostStop();
        }

        public SceneConfigReaderActor(ILogger<SceneConfigReaderActor> logger, ISceneProvider sceneProvider)
        {
            _logger = logger;
            Receive<GetScenesConfig>(m =>
            {
                foreach (var scene in sceneProvider.GetConfigs())
                {
                    _creatorActor.Tell(scene);
                }
            });
        }

        public class GetScenesConfig
        {
        }
    }
}
