using System;
using System.Collections.Generic;

namespace shome.scene.provider.yml.config
{
    public class SceneConfig
    {
        public IEnumerable<SceneAction> Actions { get; set; } = Array.Empty<SceneAction>();

        public class SceneAction
        {
            public string Name { get; set; }
            public IEnumerable<SceneIf> If { get; set; } = Array.Empty<SceneIf>();
            public IEnumerable<SceneThen> Then { get; set; } = Array.Empty<SceneThen>();
            public string DependsOn { get; set; }
        }

        public class SceneIf
        {
            public string Topic { get; set; }
            public string Value { get; set; }
            public string JsonMember { get; set; }
            public string JsonValue { get; set; }
            //todo verify?
        }

        public class SceneThen
        {
            public string Topic { get; set; }
            public string Message { get; set; }
        }
    }

   
}

