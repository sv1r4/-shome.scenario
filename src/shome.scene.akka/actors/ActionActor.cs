using System;
using Akka.Actor;
using Akka.Event;
using shome.scene.akka.util;
using shome.scene.core;
using shome.scene.core.events;
using shome.scene.core.model;
using shome.scene.core.util;

namespace shome.scene.akka.actors
{
    public class ActionActor:ReceiveActor
    {
        private readonly ILoggingAdapter _logger = Context.GetLogger(); 
        private readonly KnownPaths _knownPaths;
        private readonly SceneConfig.SceneAction _sceneAction;
        private ActionStateObj _stateObj;
        private ActionStateEnum _currentState = ActionStateEnum.Undefined;

        protected override void PreStart()
        {
            base.PreStart();
            foreach (var sceneIf in _sceneAction.If)
            {
                Context.System.ActorSelection(_knownPaths.PubSubActorPath)
                    .Tell(new SubscriptionBuilder()
                        .FromSceneIf(sceneIf)
                        .WithActor(Self)
                        .Build());
            }
            foreach (var dependency in _sceneAction.DependsOn)
            {
                Context.System.ActorSelection(_knownPaths.PubSubActorPath)
                    .Tell(new SubscriptionBuilder()
                        .FromDependency(dependency)
                        .WithActor(Self)
                        .Build());
            }

            if (!string.IsNullOrWhiteSpace(_sceneAction.Schedule))
            {
                Context.System.ActorSelection(_knownPaths.PubSubActorPath)
                    .Tell(new SubscriptionBuilder()
                        .FromCron(_sceneAction.Schedule)
                        .WithActor(Self)
                        .Build());
            }
        }

        public ActionActor(SceneConfig.SceneAction sceneAction, KnownPaths knownPaths)
        {
            _knownPaths = knownPaths;
            _sceneAction = sceneAction;
           
            BecomeIdle();
            
        }

        public static Props Props(SceneConfig.SceneAction sceneAction, KnownPaths knownPaths)
        {
            return Akka.Actor.Props.Create(() => new ActionActor(sceneAction, knownPaths));
        }

        #region Behaviours

        private void BecomeState(ActionStateEnum stateEnum, Action action)
        {
            if (_currentState != stateEnum)
            {
                _currentState = stateEnum;
                action();
            }
        }

        /// <summary>
        /// Waiting for dependencies done
        /// </summary>
        private void BecomeIdle()
        {
            BecomeState(ActionStateEnum.Idle, () =>
            {
                _logger.Debug($"Action {_sceneAction.Name} become Idle");
                _stateObj = new ActionStateObj(_sceneAction);
                Become(() =>
                {
                    Receive<ActionResultEvent>(e =>
                    {
                        _stateObj.Update(e);
                        ProcessStateObj();
                    });
                });
                ProcessStateObj();
            });
        }

        /// <summary>
        /// Waiting for 'if' triggers
        /// or scheduler
        /// </summary>
        private void BecomePending()
        {
            BecomeState(ActionStateEnum.Pending, () =>
            {
                _logger.Debug($"Action {_sceneAction.Name} become Pending");
                Become(() =>
                {
                    Receive<MqttMessageEvent>(e =>
                    {
                        _stateObj.Update(e);
                        ProcessStateObj();
                    });
                    Receive<ScheduleEvent>(e =>
                    {
                        _stateObj.Update(e);
                        ProcessStateObj();
                    });
                });
                ProcessStateObj();
            });
        }

        #endregion

        private void ProcessStateObj()
        {
            var state = _stateObj.State();
            _logger.Debug($"Action '{_sceneAction.Name}' state recent/now = '{_currentState.ToString()}/{state.ToString()}'");
            //if state the same nothing to do
            if (_currentState == state) { return;}

            if (state == ActionStateEnum.Active)
            {
                _logger.Info($"Action '{_sceneAction.Name}' activated");
                //perform 'then'
                foreach (var mqttAction in _sceneAction.Then)
                {
                    _logger.Debug($"Action '{_sceneAction.Name}' Perform 'then' action publish to  {mqttAction.Topic}");
                    PubSubPub(new PubSubProxyActor.MqttDoPublish
                    {
                        Topic = mqttAction.Topic,
                        Message = new ThenMessageBuilder()
                            .WithRawMessage(mqttAction.Message)
                            .WithTriggersState(_stateObj.TriggersState)
                            .Build()
                    });
                }
                //notify done
                PubSubPub(new ActionResultEvent
                {
                    ActionName = _sceneAction.Name,
                    Result = ActionResultEnum.Success
                });
                //return to initial state
                BecomeIdle();
            }
            else if (state == ActionStateEnum.Pending)
            {
                //todo set timeout
                BecomePending();
            }
        }

        private void PubSubPub(object message)
        {
            Context.ActorSelection(_knownPaths.PubSubActorPath).Tell(message);
        }

        protected override void PostStop()
        {
            PubSubPub(new PubSubProxyActor.UnSub { Actor = Self });
            _logger.Debug($"ActionActor - '{Self.Path.Name}' shutdown");
            base.PostStop();
        }

       
    }

   

    
}
