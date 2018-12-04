using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using shome.scene.provider.contract;
using shome.scene.provider.yml.config;

namespace shome.scene.provider.yml.extensions
{
    public static class YamlProviderExtensions
    {
        public static IServiceCollection UseYamlSceneConfiguration(this IServiceCollection services,
            IConfigurationRoot config)
        {
            services.AddTransient<ISceneProvider, YmlSceneProvider>();
            services.Configure<SceneYamlConfig>(config.GetSection(nameof(SceneYamlConfig)));
            return services;
        }
    }
}
