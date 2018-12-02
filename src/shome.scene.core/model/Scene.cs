using System;
using System.Collections.Generic;

namespace shome.scene.core.model
{
    public class Scene
    {
        public IReadOnlyList<SceneAction> Actions { get; set; } = Array.Empty<SceneAction>();
    }

    public class SceneAction
    {
        public string Name { get; set; }
        public IReadOnlyList<SceneIf> If { get; set; } = Array.Empty<SceneIf>();
        public IReadOnlyList<SceneThen> Then { get; set; } = Array.Empty<SceneThen>();
        public string DependsOn { get; set; }
    }

    public class SceneIf
    {
        public string Topic { get; set; }
        public string Value { get; set; }
        public string JsonMember { get; set; }
        public string JsonValue { get; set; }
    }

    public class SceneThen
    {
        public string Topic { get; set; }
        public string Message { get; set; }
    }
}

