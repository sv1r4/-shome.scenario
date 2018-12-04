using Microsoft.Extensions.FileProviders;
using shome.scene.provider.contract;

namespace shome.scene.provider.yml
{
    public class YmlSceneProvider:ISceneProvider
    {
        private readonly IFileProvider _filesProvider;
    }
}
