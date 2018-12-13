using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Akka.Event;
using shome.scene.akka.messages.common.events;
using shome.scene.akka.util;
using shome.scene.core.model;
using shome.scene.mqtt.core.contract;

namespace shome.scene.akka.actors
{
    public class PubSubProxyActor:ReceiveActor
    {
        private readonly IMqttBasicClient _mqttClient;
        private readonly IList<SubBase> _subs;
        private readonly ILoggingAdapter _logger = Context.GetLogger();

        public PubSubProxyActor(IMqttBasicClient mqttClient)
        {
            _mqttClient = mqttClient;
            _subs = new List<SubBase>();
            #region manages subscribtions
            ReceiveAsync<SubBase>(async e =>
            {
                if (!_subs.Contains(e))
                {
                    _subs.Add(e);
                }

                switch (e.Type)
                {
                    case TriggerType.Mqtt:
                        await _mqttClient.Subscribe(((SubToMqtt) e).Topic);
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
            #endregion
            #region transfer messages to subscribers
            Receive<MqttMessageEvent>(mqttEvent =>
            {
                var i = 0;
                foreach (var sub in _subs
                    .Where(x => x.Type == TriggerType.Mqtt
                                && x is SubToMqtt mx
                                && MqttHelper.IsSubscribed(patternTopic: mx.Topic, actualTopic: mqttEvent.Topic)
                                && !x.Subscriber.IsNobody()))
                {
                    i++;
                    sub.Subscriber.Tell(mqttEvent);
                }

                _logger.Debug($"MqttMessage delivered to {i} actors");
            });
            Receive<ActionResultEvent>(actionEvent =>
            {
                var i = 0;
                foreach (var sub in _subs
                    .Where(x => x.Type == TriggerType.Action
                                && x is SubToAction ma
                                && actionEvent.ActionName.Equals(ma.ActionName, StringComparison.InvariantCultureIgnoreCase))
                                //todo check action result
                    )
                {
                    i++;
                    sub.Subscriber.Tell(actionEvent);
                }

                _logger.Debug($"ActionMessage delivered to {i} actors");
            });
            #endregion
            #region Proxy to Mqtt brocker
            ReceiveAsync<MqttDoPublish>(async e =>
            {
                await _mqttClient.Publish(e.Topic, e.Message, e.Retained, e.Qos);
            });
            #endregion
        }

        public class MqttDoPublish
        {
            public string Topic { get; set; }
            public string Message { get; set; }
            public bool Retained { get; set; }
            public int Qos { get; set; }
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

        public class SubToMqtt:SubBase
        {
            public string Topic { get; set; }

            public SubToMqtt() : base(TriggerType.Mqtt)
            {
            }
        }

        public class SubToAction : SubBase
        {
            public string ActionName { get; set; }

            public SubToAction() : base(TriggerType.Action)
            {
            }
        }


       
    }

    
}
