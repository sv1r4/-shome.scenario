﻿using System;
using System.Collections.Generic;
using System.Linq;
using shome.scene.core.events;
using shome.scene.core.model;

namespace shome.scene.core
{
    public class ActionStateObj
    {
        private readonly IDictionary<SceneConfig.SceneDependency, DepStatus> _depState;
        private readonly IDictionary<SceneConfig.SceneIf, IfStatus> _triggerState;

        public ActionStateObj(SceneConfig.SceneAction sceneAction)
        {
            _depState = sceneAction.DependsOn.ToDictionary(x => x, _ => new DepStatus());
            _triggerState = sceneAction.If.ToDictionary(x => x, _ => new IfStatus());
        }

        public ActionStateEnum State()
        {
            //check dependencies
            if (!_depState.All(x => x.Value.IsRaised)) return ActionStateEnum.Idle;

            //check triggers
            return _triggerState.All(x => x.Value.IsRaised)
                ? ActionStateEnum.Active
                : ActionStateEnum.Pending;
        }

        public void Update(ActionResultEvent e)
        {
            foreach (var dep in _depState.Where(x => IsMatch(x.Key, e)).ToList())
            {
                _depState[dep.Key] = new DepStatus
                {
                    IsRaised = true
                }; 
            }
        }

        public void Update(MqttMessageEvent e)
        {
            foreach (var trigger in _triggerState.Where(x => IsMatch(x.Key, e)).ToList())
            {
                _triggerState[trigger.Key] = new IfStatus
                {
                    IsRaised = true,
                    ReceivedMessage = e.Message
                };
            }
        }

        private static bool IsMatch(SceneConfig.SceneIf i, MqttMessageEvent e)
        {
            return MqttHelper.IsSubscribed(i.Topic, e.Topic)
                   //todo value match
                   && i.Value.Equals(e.Message, StringComparison.InvariantCultureIgnoreCase);
        }

        private static bool IsMatch(SceneConfig.SceneDependency i, ActionResultEvent e)
        {
            return i.Action.Equals(e.ActionName, StringComparison.InvariantCultureIgnoreCase)
                && (i.When == null
                || i.When == e.Result);
        }

        private class IfStatus
        {
            public bool IsRaised { get; set; }
            public string ReceivedMessage { get; set; }
        }

        private class DepStatus
        {
            public bool IsRaised { get; set; }
        }
    }
}
