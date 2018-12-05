using System.Threading.Tasks;

namespace shome.scene.mqtt.contract
{
    public interface IMqttBasicClient
    {
        Task Publish(string topic, string message);
        Task Publish(string topic, string message, bool retained);
        Task Publish(string topic, string message, bool retained, int qos);
        Task Subscribe(string topic);
    }
}
