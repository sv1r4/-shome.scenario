using System;
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
        private readonly IList<SubscriptionBase> _subs;
        private readonly ILogger _logger;

        public PubSubActor(IMqttBasicClient mqttClient, ILogger<PubSubActor> logger)
        {
            _mqttClient = mqttClient;
            _logger = logger;
            _subs = new List<SubscriptionBase>();
            ReceiveAsync<SubscriptionBase>(async e =>
            {
                if (!_subs.Contains(e))
                {
                    _subs.Add(e);
                }

                switch (e.Type)
                {
                    case SubscriptionType.Mqtt:
                        await _mqttClient.Subscribe(((SubscriptionMqtt) e).Topic);
                        break;
                    case SubscriptionType.Action:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                _logger.LogDebug($"Sub received. Type='{e.Type.ToString()}'. Subscribers count = {_subs.Count}");
            });
            Receive<UnSub>(e =>
            {
                var sub = _subs.FirstOrDefault(x => x.Subscriber.Equals(e.Actor));
                if (sub != null)
                {
                    _subs.Remove(sub);
                }

                _logger.LogDebug($"UnSub received. Subscribers count = {_subs.Count}");
            });
            Receive<MqttReceivedMessage>(e =>
            {
                var i = 0;
                foreach (var sub in _subs.Where(x=>x.Type==SubscriptionType.Mqtt 
                                                          //todo mach topic 
                                                          && !x.Subscriber.IsNobody()))
                {
                    i++;
                    sub.Subscriber.Tell(e);
                }
                _logger.LogDebug($"MqttMessage delivered to {i} actors");
            });
        }

        public class MqttReceivedMessage
        {
            public string Topic { get; set; }
            public string Message { get; set; }
        }

        

        public class UnSub
        {
            public IActorRef Actor;
        }

        public abstract class SubscriptionBase
        {
            protected SubscriptionBase(SubscriptionType type)
            {
                Type = type;
            }
            public SubscriptionType Type { get;}
            public IActorRef Subscriber { get; set; }
        }

        public class SubscriptionMqtt:SubscriptionBase
        {
            
            public string Topic { get; set; }

            public SubscriptionMqtt() : base(SubscriptionType.Mqtt)
            {
            }
        }

        public class SubscriptionAction : SubscriptionBase
        {
            public string ActionName { get; set; }
            public SubscriptionAction() : base(SubscriptionType.Action)
            {
            }
        }

        public enum ActionResult
        {
            Success,
            Fail
        }

        public enum SubscriptionType
        {
            Mqtt,
            Action
        }
    }

    
}
