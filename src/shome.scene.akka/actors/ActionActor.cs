using System;
using System.Collections.Generic;
using System.Linq;
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
        private ActionStateObj _stateObj;
        private ActionStateEnum _currentState = ActionStateEnum.Undefined;

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
           
            BecomePending();
            
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
        private void BecomePending()
        {
            BecomeState(ActionStateEnum.Pending, () =>
            {
                _logger.Debug($"Action {_sceneAction.Name} become Pending");
                _stateObj = new ActionStateObj(_sceneAction);
                Become(() =>
                {
                    Receive<TriggerAction>(t =>
                    {
                        _stateObj.Update(t);
                        ProcessStateObj();
                    });
                });
                ProcessStateObj();
            });
        }

        /// <summary>
        /// Waiting for 'if' triggers
        /// </summary>
        private void BecomeRunning()
        {
            BecomeState(ActionStateEnum.Running, () =>
            {
                _logger.Debug($"Action {_sceneAction.Name} become Running");
                Become(() =>
                {
                    Receive<TriggerMqtt>(t =>
                    {
                        _stateObj.Update(t);
                        ProcessStateObj();
                    });
                });
                ProcessStateObj();
            });
        }

        #endregion

        private void ProcessStateObj()
        {

            var was = _currentState.ToString();
            var state = _stateObj.State();
            _logger.Debug($"Action '{_sceneAction.Name}' state was/now = '{was}/{state.ToString()}'");
            if (state == ActionStateEnum.Finished)
            {
                PubSubPub(new PubSubActor.ActionResultMessage
                {
                    ActionName = _sceneAction.Name,
                    Result = ActionResult.Success
                });
                BecomePending();
            }
            else if (state == ActionStateEnum.Running)
            {
                //todo set timeout
                BecomeRunning();
            }
        }

        private void PubSubPub(object message)
        {
            Context.ActorSelection(_knownPaths.PubSubActorPath).Tell(message);
        }

        protected override void PostStop()
        {
            PubSubPub(new PubSubActor.UnSub { Actor = Self });
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
            public string ActionName { get; set; }
            public ActionResult? Result { get; set; }
        }

    }

    public class ActionStateObj
    {
        private readonly IDictionary<SceneConfig.SceneDependency, bool> _deps;
        private readonly IDictionary<SceneConfig.SceneIf, bool> _triggers;

        public ActionStateObj(SceneConfig.SceneAction sceneAction)
        {
            _deps = sceneAction.DependsOn.ToDictionary(x => x, _=>false);
            _triggers = sceneAction.If.ToDictionary(x => x, _=>false);
        }

        public ActionStateEnum State()
        {
            //check dependencies
            if (!_deps.All(x => x.Value)) return ActionStateEnum.Pending;
            
            //check triggers
            return _triggers.All(x => x.Value) 
                ? ActionStateEnum.Finished 
                : ActionStateEnum.Running;
        }

        public void Update(ActionActor.TriggerAction t)
        {
            foreach (var dep in _deps.Where(x => x.Key.IsMatch(t)).ToList())
            {
                _deps[dep.Key] = true;
            }
        }

        internal void Update(ActionActor.TriggerMqtt t)
        {
            foreach (var trigger in _triggers.Where(x => x.Key.IsMatch(t)).ToList())
            {
                _triggers[trigger.Key] = true;
            }
        }
    }

    public static class TriggerExtensions
    {
        public static bool IsMatch(this SceneConfig.SceneIf i, ActionActor.TriggerMqtt t)
        {
            //todo math method
            return i.Topic.Equals(t.Topic, StringComparison.InvariantCultureIgnoreCase)
                && i.Value.Equals(t.Message,  StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsMatch(this SceneConfig.SceneDependency i, ActionActor.TriggerAction t)
        {
            //todo math method
            return i.Action.Equals(t.ActionName, StringComparison.InvariantCultureIgnoreCase);
        }
    }

    public enum ActionStateEnum
    {
        Undefined,
        Pending,
        Running,
        Finished
    }
}
