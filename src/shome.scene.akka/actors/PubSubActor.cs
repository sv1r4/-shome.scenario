using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Akka.Event;
using shome.scene.core.model;
using shome.scene.mqtt.contract;

namespace shome.scene.akka.actors
{
    public class PubSubActor:ReceiveActor
    {
        private readonly IMqttBasicClient _mqttClient;
        private readonly IList<SubscriptionBase> _subs;
        private readonly ILoggingAdapter _logger = Context.GetLogger();

        public PubSubActor(IMqttBasicClient mqttClient)
        {
            _mqttClient = mqttClient;
            _subs = new List<SubscriptionBase>();
            ReceiveAsync<SubscriptionBase>(async e =>
            {
                if (!_subs.Contains(e))
                {
                    _subs.Add(e);
                }

                switch (e.Type)
                {
                    case TriggerType.Mqtt:
                        await _mqttClient.Subscribe(((SubscriptionMqtt) e).Topic);
                        break;
                    case TriggerType.Action:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                _logger.Debug($"Sub received. Type='{e.Type.ToString()}'. Subscribers count = {_subs.Count}");
            });
            Receive<UnSub>(e =>
            {
                var subs = _subs.Where(x => x.Subscriber.Equals(e.Actor)).ToList();
                foreach (var sub in subs)
                {
                    _subs.Remove(sub);
                }

                _logger.Debug($"UnSub received. Subscribers count = {_subs.Count}");
            });
            Receive<MqttReceivedMessage>(e =>
            {
                var i = 0;
                foreach (var sub in _subs
                    .Where(x => x.Type == TriggerType.Mqtt
                                //todo mach topic wildcards
                                && x is SubscriptionMqtt mx
                                && e.Topic.Equals(mx.Topic, StringComparison.InvariantCultureIgnoreCase)
                                && !x.Subscriber.IsNobody()))
                {
                    i++;
                    sub.Subscriber.Tell(new ActionActor.TriggerMqtt
                    {
                        Topic = e.Topic,
                        Message = e.Message
                    });
                }

                _logger.Debug($"MqttMessage delivered to {i} actors");
            });
            //todo receive action finish event
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
            protected SubscriptionBase(TriggerType type)
            {
                Type = type;
            }
            public TriggerType Type { get;}
            public IActorRef Subscriber { get; set; }
        }

        public class SubscriptionMqtt:SubscriptionBase
        {
            
            public string Topic { get; set; }

            public SubscriptionMqtt() : base(TriggerType.Mqtt)
            {
            }
        }

        public class SubscriptionAction : SubscriptionBase
        {
            public string ActionName { get; set; }

            public SubscriptionAction() : base(TriggerType.Action)
            {
            }
        }


       
    }

    
}
