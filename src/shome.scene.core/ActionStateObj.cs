using System.Collections.Generic;
using System.Linq;
using shome.scene.core.events;
using shome.scene.core.model;
using shome.scene.core.util;

namespace shome.scene.core
{
    public class ActionStateObj
    {
        private readonly IDictionary<SceneConfig.SceneDependency, DepStatus> _dependencyState;
        public IReadOnlyList<(SceneConfig.SceneIf SceneIf, IfStatus Status)> TriggersState { get; }
        public IReadOnlyList<(SceneConfig.SceneThen SceneThen, ThenStatus Status)> ThensState { get; }
        private bool _scheduleOk;

        public ActionStateObj(SceneConfig.SceneAction sceneAction)
        {
            _dependencyState = sceneAction.DependsOn.ToDictionary(x => x, _ => new DepStatus());
            TriggersState = sceneAction.If.Select(x => (x, new IfStatus())).ToList().AsReadOnly();
            ThensState = sceneAction.Then.Select(x => (x, new ThenStatus())).ToList().AsReadOnly();
            _scheduleOk = string.IsNullOrWhiteSpace(sceneAction.Schedule);
        }

        public ActionStateEnum State()
        {
            //check dependencies
            if (!_dependencyState.All(x => x.Value.IsRaised)) return ActionStateEnum.Idle;

            //check triggers and schedule
            var isAllTriggersRaised =  TriggersState.All(x => x.Status.IsRaised) && _scheduleOk; //todo refactor _scheduleOk
            if (!isAllTriggersRaised)
            {
                //not all triggers raised - wait for triggers
                return ActionStateEnum.Pending;
            }
            var isAllThenDone = ThensState.All(x=>x.Status.IsDone);
            //if all 'then' done return 'Done' status if not wait for all then to be executed
            if (isAllThenDone)
            {
                return ActionStateEnum.Done;
            }else
            {
                return ActionStateEnum.Active;
            }
        }

        public void Update(ActionResultEvent e)
        {
            foreach (var dep in _dependencyState.Where(x => x.Key.IsMatch(e)).ToList())
            {
                _dependencyState[dep.Key] = new DepStatus
                {
                    IsRaised = true
                }; 
            }
        }

        public void Update(ScheduleEvent e)
        {
            _scheduleOk = true;
        }

        public void Update(ThenDoneEvent e)
        {
            foreach (var thenState in ThensState.Where(x => x.SceneThen.Equals(e.Then)))
            {
                thenState.Status.IsDone = true;
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
                    trigger.Status.RawMessage = e.Message;
                }
            }
        }
        
        
       

        
    }
}
