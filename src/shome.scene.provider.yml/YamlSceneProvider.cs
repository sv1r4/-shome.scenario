using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.FileProviders;
using shome.scene.core.contract;
using shome.scene.core.model;
using YamlDotNet.Serialization;

namespace shome.scene.provider.yml
{
    public class YamlSceneProvider:ISceneProvider
    {
        private readonly IFileProvider _fileProvider;
        private readonly Deserializer _deserializer;

        public YamlSceneProvider(IFileProvider fileProvider, Deserializer deserializer)
        {
            _fileProvider = fileProvider;
            _deserializer = deserializer;
        }

        public IEnumerable<SceneConfig> GetConfigs()
        {
            foreach (var f in _fileProvider.GetDirectoryContents(string.Empty).Where(x => !x.IsDirectory))
            {
                using (var stream = f.CreateReadStream())
                {
                    using (var reader = new StreamReader(stream))
                    {
                        var sceneConfig = _deserializer.Deserialize<SceneConfig>(reader);
                        if (sceneConfig != null)
                        {
                            yield return sceneConfig;
                        }
                    }
                }
            }
        }
    }
}
