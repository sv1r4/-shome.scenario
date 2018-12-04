using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using shome.scene.mqtt.config;
using shome.scene.mqtt.extensions;
using shome.scene.provider.contract;
using shome.scene.provider.yml;
using shome.scene.provider.yml.config;
using uPLibrary.Networking.M2Mqtt;

namespace shome.scene.processor
{
    class Program
    {
        private static ILogger _logger;

        static void Main()
        {
            MainAsync().GetAwaiter().GetResult();
        }

        static async Task MainAsync()
        {
            try
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables();
                var config = builder.Build();
                var cfg = config.GetSection(nameof(SceneYamlConfig)).Get<SceneYamlConfig>();

                var services = new ServiceCollection();
                services.AddLogging(logging =>
                {
                    logging.AddConfiguration(config.GetSection("Logging"));
                    logging.AddConsole();
                });
                services.AddMqttClient(config);
                services.AddSingleton<ISceneProvider, YamlSceneProvider>();
                var fp = new PhysicalFileProvider(cfg.DirectoryAbsolute);
                services.AddSingleton<IFileProvider>(fp);

                var sp = services.BuildServiceProvider();
                _logger = sp.GetService<ILogger<Program>>();

                var mqtt = sp.GetRequiredService<MqttClient>();
                var mqttConfig = sp.GetRequiredService<IOptions<MqttConfig>>().Value;

                mqtt.Connect($"scene.processor-{Guid.NewGuid()}", mqttConfig.User, mqttConfig.Password);

                

                _logger.LogInformation($"MqtConnected {mqtt.IsConnected}");

                _logger.LogInformation("Scene Processor");
                await InfinitMonitorChanges(fp, "*.yaml");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static async Task InfinitMonitorChanges(IFileProvider fp, string path)
        {
            while (true)
            {
                var w = fp.Watch(path);
                var tcs = new TaskCompletionSource<object>();
                w.RegisterChangeCallback(state =>
                {
                    ((TaskCompletionSource<object>)state).TrySetResult(null);
                }, tcs);
                await tcs.Task.ConfigureAwait(false);

                _logger.LogDebug("changed");
            }
        }
    }

}
