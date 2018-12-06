using System;
using Akka.Actor;
using Akka.DI.Core;
using Microsoft.Extensions.Logging;
using shome.scene.core.contract;

namespace shome.scene.akka.actors
{
    public class SceneConfigReaderActor : ReceiveActor
    {
        private readonly IActorRef _creatorActor;

        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(
                maxNrOfRetries: 10,
                withinTimeRange: TimeSpan.FromSeconds(5),
                localOnlyDecider: ex =>
                {
                    switch (ex)
                    {
                        case ArithmeticException ae:
                            return Directive.Resume;
                        case NullReferenceException nre:
                            return Directive.Restart;
                        case ArgumentException are:
                            return Directive.Stop;
                        default:
                            return Directive.Escalate;
                    }
                });
        }

        public SceneConfigReaderActor(ILogger<SceneConfigReaderActor> logger, ISceneProvider sceneProvider)
        {
            Receive<GetScenesConfig>(m =>
            {
                foreach (var scene in sceneProvider.GetConfigs())
                {
                    //todo stop removed actors
                    _creatorActor.Tell(new SceneCreatorActor.CreateScene
                    {
                        SceneConfig = scene
                    });
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
