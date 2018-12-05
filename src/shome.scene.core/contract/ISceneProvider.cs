using System.Collections.Generic;
using shome.scene.core.model;

namespace shome.scene.core.contract
{
    public interface ISceneProvider
    {
        IEnumerable<SceneConfig> GetConfigs();
    }
}
