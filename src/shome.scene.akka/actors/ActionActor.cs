using Akka.Actor;
using Akka.Event;
using shome.scene.akka.util;
using shome.scene.core.model;

namespace shome.scene.akka.actors
{
    public class ActionActor:ReceiveActor
    {
        private readonly ILoggingAdapter _logger = Context.GetLogger(); 
        private readonly KnownPaths _knownPaths;
        private readonly SceneConfig.SceneAction _sceneAction;

        protected override void PreStart()
        {
            base.PreStart();
            foreach (var sceneIf in _sceneAction.If)
            {

                // _logger.LogDebug($"Tell Subscribe to topic='{topic}'");
                //subscribe scene actor for messages
                Context.System.ActorSelection(_knownPaths.PubSubActorPath)
                    .Tell(new SubscriptionBuilder()
                        .FromSceneIf(sceneIf)
                        .WithActor(Self)
                        .Build());
            }
            foreach (var dependency in _sceneAction.DependsOn)
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

        public ActionActor(SceneConfig.SceneAction sceneAction, KnownPaths knownPaths)
        {
            _knownPaths = knownPaths;
            _sceneAction = sceneAction;
           

            Receive<ITrigger>(t =>
            {
                switch (t)
                {
                    case TriggerMqtt triggerMqtt:
                        break;
                    case TriggerAction triggerAction:
                        break;
                    default:
                        _logger.Debug($"Trigger {t.GetType().Name} is not supported");
                        break;
                        
                }
            });
        }

        public static Props Props(SceneConfig.SceneAction sceneAction, KnownPaths knownPaths)
        {
            return Akka.Actor.Props.Create(() => new ActionActor(sceneAction, knownPaths));
        }


        protected override void PostStop()
        {
            Context.ActorSelection(_knownPaths.PubSubActorPath).Tell(new PubSubActor.UnSub { Actor = Self });
            _logger.Debug($"ActionActor - '{Self.Path.Name}' shutdown");
            base.PostStop();
        }

        public interface ITrigger
        {
            
        }

        public class TriggerMqtt:ITrigger
        {
            public string Topic { get; set; }
            public string Message { get; set; }
        }

        public class TriggerAction:ITrigger
        {
            public string Action { get; set; }
            public ActionResult? Result { get; set; }
        }

    }
}
