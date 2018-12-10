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
        }

        public class SceneIf
        {
            public string Topic { get; set; }
            public string Value { get; set; }
            public string JsonMember { get; set; }
            public string JsonValue { get; set; }
        }

        public class SceneDependency
        {
            public string Name { get; set; }
            public ActionResult? Result { get; set; }
        }

        public class SceneThen
        {
            public string Topic { get; set; }
            public string Message { get; set; }
        }
    }

   
}

