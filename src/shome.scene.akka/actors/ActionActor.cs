using Akka.Actor;
using Akka.DI.Core;
using Akka.Event;
using Microsoft.Extensions.Logging;
using shome.scene.akka.util;
using shome.scene.core.model;

namespace shome.scene.akka.actors
{
    public class ActionActor:ReceiveActor
    {
        private readonly ILoggingAdapter _logger = Context.GetLogger();
        private readonly KnownPaths _knownPaths;

        public ActionActor(SceneConfig.SceneAction sceneAction, KnownPaths knownPaths)
        {
            _knownPaths = knownPaths;

            foreach (var sceneIf in sceneAction.If)
            {

                // _logger.LogDebug($"Tell Subscribe to topic='{topic}'");
                //subscribe scene actor for messages
                Context.System.ActorSelection(_knownPaths.PubSubActorPath)
                    .Tell(new SubscriptionBuilder()
                        .FromSceneIf(sceneIf)
                        .WithActor(Self)
                        .Build());
            }
            foreach (var dependency in sceneAction.DependsOn)
            {
                // _logger.LogDebug($"Tell Subscribe to topic='{topic}'");
                //subscribe scene actor for messages
                Context.System.ActorSelection(_knownPaths.PubSubActorPath)
                    .Tell(new SubscriptionBuilder()
                        .FromDependency(dependency)
                        .WithActor(Self)
                        .Build());
            }

        }

        public static Props Props(SceneConfig.SceneAction sceneAction, KnownPaths knownPaths)
        {
            return Akka.Actor.Props.Create(() => new ActionActor(sceneAction, knownPaths));
        }


        protected override void PostStop()
        {
            Context.ActorSelection(_knownPaths.PubSubActorPath).Tell(new PubSubActor.UnSub { Actor = Self });
            _logger.Debug($"SceneActor - '{this.Self.Path.Name}' shutdown");
            base.PostStop();
        }

    }
}
