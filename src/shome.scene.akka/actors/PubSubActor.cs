using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Microsoft.Extensions.Logging;
using shome.scene.mqtt.contract;

namespace shome.scene.akka.actors
{
    public class PubSubActor:ReceiveActor
    {
        private readonly IMqttBasicClient _mqttClient;
        private readonly IList<IActorRef> _subscribers;
        private readonly ILogger _logger;

        public PubSubActor(IMqttBasicClient mqttClient, ILogger<PubSubActor> logger)
        {
            _mqttClient = mqttClient;
            _logger = logger;
            _subscribers = new List<IActorRef>();
            ReceiveAsync<Sub>(async e =>
            {
                await _mqttClient.Subscribe(e.Topic);
                if (!_subscribers.Contains(e.Subscriber))
                {
                    _subscribers.Add(e.Subscriber);
                }

                _logger.LogDebug($"Sub received. Subscribers count = {_subscribers.Count}");
            });
            Receive<UnSub>(e =>
            {
                if (_subscribers.Contains(e.Subscriber))
                {
                    _subscribers.Remove(e.Subscriber);
                }

                _logger.LogDebug($"UnSub received. Subscribers count = {_subscribers.Count}");
            });
            Receive<MqttReceivedMessage>(e =>
            {
                var i = 0;
                foreach (var subscriber in _subscribers.Where(x=>!x.IsNobody()))
                {
                    i++;
                    subscriber.Tell(e);
                }
                _logger.LogDebug($"MqttMessage delivered to {i} actors");
            });
        }

        public class MqttReceivedMessage
        {
            public string Topic { get; set; }
            public string Message { get; set; }
        }

        public class Sub
        {
            public IActorRef Subscriber;
            public string Topic { get; set; }
        }

        public class UnSub
        {
            public IActorRef Subscriber;
        }
    }

    
}
