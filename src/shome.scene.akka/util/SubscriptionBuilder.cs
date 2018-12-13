using Akka.Actor;
using shome.scene.akka.actors;
using shome.scene.core.model;

namespace shome.scene.akka.util
{
    public class SubscriptionBuilder
    {
        private IActorRef _actor;
        private PubSubProxyActor.SubBase _subscription;

        public SubscriptionBuilder WithActor(IActorRef actor)
        {
            _actor = actor;
            return this;
        }
        public SubscriptionBuilder FromSceneIf(SceneConfig.SceneIf sceneIf)
        {

            _subscription = new PubSubProxyActor.SubToMqtt
            {
                Topic = sceneIf.Topic
            };

            return this;
        }

        public SubscriptionBuilder FromDependency(SceneConfig.SceneDependency sceneDependency)
        {
            _subscription = new PubSubProxyActor.SubToAction
            {
                ActionName = sceneDependency.Action
            };
            return this;
        }

        public PubSubProxyActor.SubBase Build()
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
