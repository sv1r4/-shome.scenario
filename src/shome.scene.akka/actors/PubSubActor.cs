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
        private readonly IList<SubBase> _subs;
        private readonly ILoggingAdapter _logger = Context.GetLogger();

        public PubSubActor(IMqttBasicClient mqttClient)
        {
            _mqttClient = mqttClient;
            _subs = new List<SubBase>();
            ReceiveAsync<SubBase>(async e =>
            {
                if (!_subs.Contains(e))
                {
                    _subs.Add(e);
                }

                switch (e.Type)
                {
                    case TriggerType.Mqtt:
                        await _mqttClient.Subscribe(((SubMqtt) e).Topic);
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
                                && x is SubMqtt mx
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
            Receive<ActionResultMessage>(e =>
            {
                var i = 0;
                foreach (var sub in _subs
                    .Where(x => x.Type == TriggerType.Action
                                && x is SubAction ma
                                && e.ActionName.Equals(ma.ActionName, StringComparison.InvariantCultureIgnoreCase))
                                //todo check action result
                    )
                {
                    i++;
                    sub.Subscriber.Tell(new ActionActor.TriggerAction
                    {
                        ActionName = e.ActionName,
                        Result = e.Result
                    });
                }

                _logger.Debug($"ActionMessage delivered to {i} actors");
            });
        }

        public class MqttReceivedMessage
        {
            public string Topic { get; set; }
            public string Message { get; set; }
        }


        public class ActionResultMessage
        {
            public string ActionName { get; set; }
            public ActionResult Result { get; set; }
        }


        public class UnSub
        {
            public IActorRef Actor;
        }

        public abstract class SubBase
        {
            protected SubBase(TriggerType type)
            {
                Type = type;
            }
            public TriggerType Type { get;}
            public IActorRef Subscriber { get; set; }
        }

        public class SubMqtt:SubBase
        {
            
            public string Topic { get; set; }

            public SubMqtt() : base(TriggerType.Mqtt)
            {
            }
        }

        public class SubAction : SubBase
        {
            public string ActionName { get; set; }

            public SubAction() : base(TriggerType.Action)
            {
            }
        }


       
    }

    
}
