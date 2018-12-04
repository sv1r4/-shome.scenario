using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using shome.scene.mqtt.config;
using shome.scene.provider.contract;
using shome.scene.provider.yml;
using shome.scene.provider.yml.config;

namespace shome.scene.processor
{
    public class Program
    {
        private static ILogger _logger;

        public static void Main()
        {
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, args) =>
            {
                Console.WriteLine("App graceful stop");
                try
                {
                    Stop().GetAwaiter().GetResult();
                }
                finally
                {
                    cts.Cancel();
                }
            };
            Console.WriteLine("App start");

            Start().Wait(cts.Token);
        }

        private static Task Stop()
        {
            return Task.CompletedTask;
        }

        private static async Task Start()
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
                services.AddSingleton<ISceneProvider, YamlSceneProvider>();
                var fp = new PhysicalFileProvider(cfg.DirectoryAbsolute);
                services.AddSingleton<IFileProvider>(fp);

                var sp = services.BuildServiceProvider();
                _logger = sp.GetService<ILogger<Program>>();

                await InitMqtt(config);

                _logger.LogInformation("Scene Processor");
                await InfinitMonitorChanges(fp, "*.yaml");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static async Task InitMqtt(IConfigurationRoot config)
        {
            var cfg = config.GetSection(nameof(MqttConfig)).Get<MqttConfig>();
            var optionBuilder = new MqttClientOptionsBuilder()
                .WithClientId($"shome.scene.processor-{Guid.NewGuid()}")
                .WithTcpServer(cfg.Host, cfg.Port);

            if (!string.IsNullOrWhiteSpace(cfg.User))
            {
                optionBuilder = optionBuilder.WithCredentials(cfg.User, cfg.Password);
            }

            var options = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(optionBuilder.Build())
                .Build();

            var mqttClient = new MqttFactory().CreateManagedMqttClient();
            await mqttClient.SubscribeAsync(new TopicFilterBuilder().WithTopic("#").WithAtLeastOnceQoS().Build());
            mqttClient.Connected += (sender, args) =>
            {
                _logger.LogInformation("mqtt connected"); 
            };

            mqttClient.ConnectingFailed += (sender, args) =>
            {
                _logger.LogError($"mqtt connection fail. Error {args.Exception}");
            };
            mqttClient.Disconnected += (sender, args) =>
            {
                _logger.LogWarning($"mqtt disconnected. Error {args.Exception}");
            };
            mqttClient.SynchronizingSubscriptionsFailed += (sender, args) =>
            {
                _logger.LogError($"mqtt sync subscriptions fail. Error {args.Exception}");
            };

            mqttClient.ApplicationMessageReceived += (sender, args) =>
            {
                _logger.LogDebug($"message received.\n\ttopic='{args.ApplicationMessage.Topic}'\n\tmessage='{Encoding.UTF8.GetString(args.ApplicationMessage.Payload)}'");
            };

            await mqttClient.StartAsync(options);
            //await mqttClient.SubscribeAsync(new TopicFilterBuilder().WithTopic("my/topic2").Build());
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
