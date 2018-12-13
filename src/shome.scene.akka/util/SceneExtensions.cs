using System;
using System.Collections.Generic;
using System.Text;
using shome.scene.akka.actors;
using shome.scene.akka.messages;
using shome.scene.akka.messages.common.events;
using shome.scene.core.model;

namespace shome.scene.akka.util
{
    public static class SceneExtensions
    {
        public static bool IsMatch(this SceneConfig.SceneIf i, MqttMessageEvent e)
        {
            //todo math method
            return i.Topic.Equals(e.Topic, StringComparison.InvariantCultureIgnoreCase)
                   && i.Value.Equals(e.Message, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsMatch(this SceneConfig.SceneDependency i, ActionResultEvent e)
        {
            //todo math method
            return i.Action.Equals(e.ActionName, StringComparison.InvariantCultureIgnoreCase);
        }
    }

}
