using System;
using System.Collections.Generic;

namespace shome.scene.core.model
{
    public class SceneConfig
    {
        public string Name { get; set; }
        public IEnumerable<SceneAction> Actions { get; set; } = Array.Empty<SceneAction>();

        public class SceneAction
        {
            public string Name { get; set; }
            public IEnumerable<SceneIf> If { get; set; } = Array.Empty<SceneIf>();
            public IEnumerable<SceneThen> Then { get; set; } = Array.Empty<SceneThen>();
            public IEnumerable<SceneDependency> DependsOn { get; set; } = Array.Empty<SceneDependency>();
            public string Schedule { get; set; }
            public TimeSpan? Timeout { get; set; }
        }

        public class SceneIf
        {
            //todo validate if topic or cron only one allowed
            public string Topic { get; set; }
            public string Value { get; set; }
            public string JsonMember { get; set; }
        }
        
        public class SceneDependency
        {
            public string Action { get; set; }
            public ActionResultEnum? When { get; set; }
        }


        public class SceneThen
        {
            public string Topic { get; set; }
            public string Message { get; set; }
            public bool Retained { get; set; }
            public TimeSpan? Delay { get; set; }

            public bool Equals(SceneThen other)
            {
                if (other == null)
                {
                    return false;
                }

                return this.Delay == other.Delay
                       && string.Equals(this.Message, other.Message, StringComparison.Ordinal)
                       && string.Equals(this.Topic, other.Topic, StringComparison.OrdinalIgnoreCase);
            }

        }
    }

   
}

