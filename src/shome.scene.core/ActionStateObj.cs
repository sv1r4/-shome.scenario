using System.Collections.Generic;
using System.Linq;
using shome.scene.core.events;
using shome.scene.core.model;
using shome.scene.core.util;

namespace shome.scene.core
{
    public class ActionStateObj
    {
        private readonly IDictionary<SceneConfig.SceneDependency, DepStatus> _depState;
        public IReadOnlyList<(SceneConfig.SceneIf SceneIf, IfStatus Status)> TriggersState { get; }
       
        public ActionStateObj(SceneConfig.SceneAction sceneAction)
        {
            _depState = sceneAction.DependsOn.ToDictionary(x => x, _ => new DepStatus());
            TriggersState = sceneAction.If.Select(x => (x, new IfStatus())).ToList();            
        }

        public ActionStateEnum State()
        {
            //check dependencies
            if (!_depState.All(x => x.Value.IsRaised)) return ActionStateEnum.Idle;

            //check triggers
            return TriggersState.All(x => x.Status.IsRaised)
                ? ActionStateEnum.Active
                : ActionStateEnum.Pending;
        }

        public void Update(ActionResultEvent e)
        {
            foreach (var dep in _depState.Where(x => x.Key.IsMatch(e)).ToList())
            {
                _depState[dep.Key] = new DepStatus
                {
                    IsRaised = true
                }; 
            }
        } 

        public void Update(MqttMessageEvent e)
        {
            foreach (var trigger in TriggersState)
            {
                if (trigger.SceneIf.IsMatch(e, out var eventValue))
                {
                    trigger.Status.IsRaised = true;
                    trigger.Status.EventValue = eventValue;
                }
            }
        }
        
        
       

        
    }
}
