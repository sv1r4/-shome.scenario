using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using shome.scene.mqtt.config;
using uPLibrary.Networking.M2Mqtt;

namespace shome.scene.mqtt.extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMqttClient(this IServiceCollection services, IConfigurationRoot config)
        {
            services.Configure<MqttConfig>(config.GetSection(nameof(MqttConfig)));
            services.AddSingleton<MqttClient>(sp =>
            {
                var cfg = sp.GetRequiredService<IOptions<MqttConfig>>().Value;
                var mqtt = new MqttClient(cfg.Host, cfg.Port, false, MqttSslProtocols.None, null, null);
                
                return mqtt;
            });
            return services;
        }
    }
}
