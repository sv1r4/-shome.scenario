using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using shome.scene.core.contract;
using shome.scene.core.events;
using shome.scene.core.model;

namespace shome.scene.core.util
{
    public static class MatchingExtensions
    {
        public static bool IsMatch(this SceneConfig.SceneDependency i, ActionResultEvent e)
        {
            return i.Action.Equals(e.ActionName, StringComparison.InvariantCultureIgnoreCase)
                   && (i.When == null
                       || i.When == e.Result);
        }

        //todo refactor if else sequence
        public static bool IsMatch(this SceneConfig.SceneIf i, MqttMessageEvent e, out string eventValue)
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
                catch (JsonReaderException)
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
            #region special operators - compare - enumerate compare map
            //order from longest to shortest prefix to avoid invalid matches
            foreach (var co in CompareOperatorsMap.OrderByDescending(x => x.Key.Length))
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
            #endregion
            #region special operators - simple text match

            if (iValue.StartsWith(Specials.Match))
            {
                iValue = iValue.Remove(0, Specials.Match.Length).Trim();
                return SpecialsHelper.IsSimpleMatch(iValue, eventValue);
            }
            #endregion

            return false;
        }

        private static readonly IDictionary<string, Func<decimal, decimal, bool>> CompareOperatorsMap =
            new Dictionary<string, Func<decimal, decimal, bool>>
            {
                {Specials.Greater, (iValue, eValue) => eValue > iValue},
                {Specials.GreaterEqual, (iValue, eValue) => eValue >= iValue},
                {Specials.Less, (iValue, eValue) => eValue < iValue},
                {Specials.LessEqual, (iValue, eValue) => eValue <= iValue},
            };

    }
}
