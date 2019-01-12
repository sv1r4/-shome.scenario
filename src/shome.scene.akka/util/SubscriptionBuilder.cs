using System;
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
            #region validate input

            if (sceneIf == null) { throw new ArgumentNullException(nameof(sceneIf));}

            if (string.IsNullOrWhiteSpace(sceneIf.Topic))
            {
                throw new ArgumentException($"{nameof(sceneIf.Topic)} should not be both empty", nameof(sceneIf));
            }

            #endregion

            return FromTopic(sceneIf.Topic);
        }

        private SubscriptionBuilder FromTopic(string topic)
        {
            _subscription = new PubSubProxyActor.SubToMqtt
            {
                Topic = topic
            };

            return this;
        }

        public SubscriptionBuilder FromCron(string cronString)
        {
            _subscription = new PubSubProxyActor.SubToTime
            {
                Cron = cronString
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
