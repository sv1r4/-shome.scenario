using Microsoft.Extensions.FileProviders;
using shome.scene.provider.contract;

namespace shome.scene.provider.yml
{
    public class YamlSceneProvider:ISceneProvider
    {
        private readonly IFileProvider _filesProvider;

        public YamlSceneProvider(IFileProvider filesProvider)
        {
            _filesProvider = filesProvider;
        }
    }
}
