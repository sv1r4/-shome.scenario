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

        #region behaviours

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


        /// <summary>
        /// Waiting for 'then' done
        /// or scheduler
        /// </summary>
        private void BecomeActive()
        {
            BecomeState(ActionStateEnum.Active, () =>
            {
                _logger.Debug($"Action {_sceneAction.Name} become Active. Waiting for 'then' done");
                Become(() =>
                {
                    Receive<DoThen>(e =>
                    {
                        PubSubPub(new PubSubProxyActor.MqttDoPublish
                        {
                            Topic = e.Then.Topic,
                            Message = new ThenMessageBuilder()
                                .WithRawMessage(e.Then.Message)
                                .WithTriggersState(_stateObj.TriggersState)
                                .Build()
                        });

                        _stateObj.Update(new ThenDoneEvent(e.Then));
                        ProcessStateObj();
                    });
                });
                ProcessStateObj();
            });
        }
        #endregion

        #region messages

        public class DoThen
        {
            public SceneConfig.SceneThen Then { get; }

            public DoThen(SceneConfig.SceneThen then)
            {
                Then = then;
            }
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
                BecomeActive();

                //queue/schedule 'then's
                foreach (var mqttAction in _sceneAction.Then)
                {
                    _logger.Debug($"Action '{_sceneAction.Name}' Perform 'then' action publish to  {mqttAction.Topic} {(mqttAction.Delay==null?string.Empty:$"with delay {mqttAction.Delay}")}");
                    if (mqttAction.Delay == null)
                    {
                        Self.Tell(new DoThen(mqttAction));
                    }
                    else
                    {
                        Context.System.Scheduler.ScheduleTellOnce(mqttAction.Delay.Value, Self, new DoThen(mqttAction),Self);
                    }
                }
            }
            else if (state == ActionStateEnum.Done)
            {
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
