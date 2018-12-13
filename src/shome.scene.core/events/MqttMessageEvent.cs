namespace shome.scene.core.events
{
    public class MqttMessageEvent
    {
        public string Topic { get; set; }
        public string Message { get; set; }
    }
}
