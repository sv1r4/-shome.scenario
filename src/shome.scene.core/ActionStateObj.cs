using System;
using System.Collections.Generic;
using System.Linq;
using shome.scene.core.events;
using shome.scene.core.model;

namespace shome.scene.core
{
    public class ActionStateObj
    {
        private readonly IDictionary<SceneConfig.SceneDependency, DepStatus> _deps;
        private readonly IDictionary<SceneConfig.SceneIf, IfStatus> _triggers;

        public ActionStateObj(SceneConfig.SceneAction sceneAction)
        {
            _deps = sceneAction.DependsOn.ToDictionary(x => x, _ => new DepStatus());
            _triggers = sceneAction.If.ToDictionary(x => x, _ => new IfStatus());
        }

        public ActionStateEnum State()
        {
            //check dependencies
            if (!_deps.All(x => x.Value.IsRised)) return ActionStateEnum.Idle;

            //check triggers
            return _triggers.All(x => x.Value.IsRised)
                ? ActionStateEnum.Active
                : ActionStateEnum.Pending;
        }

        public void Update(ActionResultEvent e)
        {
            foreach (var dep in _deps.Where(x => IsMatch(x.Key, e)).ToList())
            {
                _deps[dep.Key] = new DepStatus
                {
                    IsRised = true
                }; 
            }
        }

        public void Update(MqttMessageEvent e)
        {
            foreach (var trigger in _triggers.Where(x => IsMatch(x.Key, e)).ToList())
            {
                _triggers[trigger.Key] = new IfStatus
                {
                    IsRised = true,
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
            public bool IsRised { get; set; }
            public string ReceivedMessage { get; set; }
        }

        private class DepStatus
        {
            public bool IsRised { get; set; }
        }
    }
}
