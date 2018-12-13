using System.Text;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Protocol;
using shome.scene.mqtt.core.contract;

namespace shome.scene.processor.mqtt
{
    public class MqttNetAdapter:IMqttBasicClient
    {
        private readonly IManagedMqttClient _mqtt;

        public MqttNetAdapter(IManagedMqttClient mqtt)
        {
            _mqtt = mqtt;
        }

        public Task Publish(string topic, string message)
        {
            return _mqtt.PublishAsync(CreateApplicationMessage(topic, message));
        }

        private static MqttApplicationMessage CreateApplicationMessage(string topic, string message, bool retain=false,int qos =0 )
        {
            return new MqttApplicationMessage
            {
                Payload = Encoding.UTF8.GetBytes(message),
                Topic = topic,
                Retain = retain,
                QualityOfServiceLevel = (MqttQualityOfServiceLevel)qos
            };
        }

        public Task Publish(string topic, string message, bool retained)
        {
            return _mqtt.PublishAsync(CreateApplicationMessage(topic, message, retained));
        }

        public Task Publish(string topic, string message, bool retained, int qos)
        {
            return _mqtt.PublishAsync(CreateApplicationMessage(topic, message, retained, qos));
        }

        public Task Subscribe(string topic)
        {
            return _mqtt.SubscribeAsync(topic);
        }
    }
}
