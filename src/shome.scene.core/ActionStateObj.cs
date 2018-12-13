using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using shome.scene.core.contract;
using shome.scene.core.events;
using shome.scene.core.model;

namespace shome.scene.core
{
    public class ActionStateObj
    {
        private readonly IDictionary<SceneConfig.SceneDependency, DepStatus> _depState;
        private readonly IList<(SceneConfig.SceneIf SceneIf, IfStatus Status)> _triggerState;

        public ActionStateObj(SceneConfig.SceneAction sceneAction)
        {
            _depState = sceneAction.DependsOn.ToDictionary(x => x, _ => new DepStatus());
            _triggerState = sceneAction.If.Select(x => (x, new IfStatus())).ToList();
        }

        public ActionStateEnum State()
        {
            //check dependencies
            if (!_depState.All(x => x.Value.IsRaised)) return ActionStateEnum.Idle;

            //check triggers
            return _triggerState.All(x => x.Status.IsRaised)
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
            foreach (var trigger in _triggerState.ToList())
            {
                if (IsMatch(trigger.SceneIf, e, out var eventValue))
                {
                    trigger.Status.IsRaised = true;
                    trigger.Status.EventValue = eventValue;
                }
            }
        }

        //todo is matches to extensions and test
        private static bool IsMatch(SceneConfig.SceneIf i, MqttMessageEvent e, out string eventValue)
        {
            var iValue = i.Value;
            eventValue = e.Message;
            #region verify topic
            if (!MqttHelper.IsSubscribed(i.Topic, e.Topic))
            {
                return false;
            }
            #endregion
            #region no value defined - true
            if (string.IsNullOrWhiteSpace(iValue))
            {
                return true;
            }
            #endregion
            #region  json member defined - look into message as json
            if (!string.IsNullOrWhiteSpace(i.JsonMember))
            {
                try
                {
                    var jo = JObject.Parse(e.Message);
                    if (jo.TryGetValue(i.JsonMember, out var jValue))
                    {
                        eventValue = jValue.Value<string>();
                    }
                }
                catch (JsonReaderException) //todo json exception
                {
                    //todo handle
                    return false;
                }
            }
            #endregion
            #region no special - check equal
            if (!iValue.StartsWith(Specials.Key))
            {
                return iValue.Equals(eventValue, StringComparison.InvariantCultureIgnoreCase);
            }
            #endregion
            #region special operators - look deepper

            //order from longest to shortest prefix to avoid invalid matches
            foreach (var co in CompareOperatorsMap.OrderByDescending(x=>x.Key.Length))
            {
                if (iValue.StartsWith(co.Key))
                {
                    iValue = iValue.Remove(0, co.Key.Length).Trim();
                    if (decimal.TryParse(iValue, out var diValue) && decimal.TryParse(eventValue, out var deValue))
                    {
                        return co.Value(diValue, deValue);
                    }
                }
            }

            //todo match regex specials
            return false;

            #endregion
        }

        private static readonly IDictionary<string, Func<decimal, decimal, bool>> CompareOperatorsMap =
            new Dictionary<string, Func<decimal, decimal, bool>>
            {
                {Specials.PrefixGreater, (iValue, eValue) => eValue > iValue},
                {Specials.PrefixGreaterEqual, (iValue, eValue) => eValue >= iValue},
                {Specials.PrefixLess, (iValue, eValue) => eValue < iValue},
                {Specials.PrefixLessEqual, (iValue, eValue) => eValue <= iValue},
            };

        private static bool IsMatch(SceneConfig.SceneDependency i, ActionResultEvent e)
        {
            return i.Action.Equals(e.ActionName, StringComparison.InvariantCultureIgnoreCase)
                && (i.When == null
                || i.When == e.Result);
        }

        private class IfStatus
        {
            public bool IsRaised { get; set; }
            public string EventValue { get; set; }
        }

        private class DepStatus
        {
            public bool IsRaised { get; set; }
        }

        public string GetThenMessage(string thenMessage)
        {
            #region regular return what defined in message
            if (!thenMessage.StartsWith(Specials.Key))
            {
                return thenMessage;
            }
            #endregion
            #region process specials - proxy from 'if'

            if (thenMessage.StartsWith(Specials.Proxy))
            {
                //if proxy index specified in message i.e. '@proxy1', '@proxy2'
                //get message from corresponding 'if' otherwise from first one
                if (!int.TryParse(thenMessage.Remove(0, Specials.Proxy.Length), out var proxyIndex))
                {
                    proxyIndex = 0;
                }
                //check interval
                if (proxyIndex >= 0 && proxyIndex < _triggerState.Count)
                {
                    return _triggerState[proxyIndex].Status.EventValue;
                }
                throw new IndexOutOfRangeException($"Specified index={proxyIndex} ('{thenMessage}') is out of range 'If' collection size [{0}..{_triggerState.Count-1}]");
                
            }
            #endregion
            throw new InvalidOperationException($"Unsupported @special message '{thenMessage}'");
        }
    }
}
