using Akka.Actor;
using shome.scene.akka.actors;
using shome.scene.core.model;

namespace shome.scene.akka.util
{
    public class SubscriptionBuilder
    {
        private IActorRef _actor;
        private PubSubActor.SubscriptionBase _subscription;

        public SubscriptionBuilder WithActor(IActorRef actor)
        {
            _actor = actor;
            return this;
        }
        public SubscriptionBuilder FromSceneIf(SceneConfig.SceneIf sceneIf)
        {

            _subscription = new PubSubActor.SubscriptionMqtt
            {
                Topic = sceneIf.Topic
            };

            return this;
        }

        public SubscriptionBuilder FromDependency(SceneConfig.SceneDependency sceneDependency)
        {
            _subscription = new PubSubActor.SubscriptionAction
            {
                ActionName = sceneDependency.Name
            };
            return this;
        }

        public PubSubActor.SubscriptionBase Build()
        {
            if (_subscription == null)
            {
                return null;
            }
            _subscription.Subscriber = _actor;
            return _subscription;
        }
    }
}
