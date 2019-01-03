using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using Quartz;
using shome.scene.akka.util;
using shome.scene.core.events;
using shome.scene.core.model;
using shome.scene.core.util;
using shome.scene.mqtt.core.contract;
using IScheduler = Quartz.IScheduler;

namespace shome.scene.akka.actors
{
    public class PubSubProxyActor:ReceiveActor
    {
        private readonly IMqttBasicClient _mqttClient;
        private readonly IList<SubBase> _subs;
        private readonly ILoggingAdapter _logger = Context.GetLogger();
        private readonly ISceneActionScheduler _actionScheduler; 

        public PubSubProxyActor(IMqttBasicClient mqttClient, ISceneActionScheduler actionScheduler)
        {
            _mqttClient = mqttClient;
            _actionScheduler = actionScheduler;
            _subs = new List<SubBase>();

            InitManageSubscriptions();
            InitPassMessagesToSubscribers();
            InitProxyToMqttBroker();
        }

        private void InitManageSubscriptions()
        {
            ReceiveAsync<SubBase>(async e =>
            {
                if (!_subs.Contains(e))
                {
                    _subs.Add(e);
                }

                switch (e.Type)
                {
                    case TriggerTypeEnum.Mqtt:
                        await _mqttClient.Subscribe(((SubToMqtt)e).Topic);
                        break;
                    case TriggerTypeEnum.Action:
                        break;
                    case TriggerTypeEnum.Time:
                        await _actionScheduler.ScheduleAction((SubToTime)e);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                _logger.Debug($"Sub received. Type='{e.Type.ToString()}'. Subscribers count = {_subs.Count}");
            });
            ReceiveAsync<UnSub>(async e =>
            {
                var subs = _subs.Where(x => x.Subscriber.Equals(e.Actor)).ToList();
                foreach (var sub in subs)
                {
                    _subs.Remove(sub);
                    await _actionScheduler.UnScheduleAction(sub);
                }

                _logger.Debug($"UnSub received. Subscribers count = {_subs.Count}");
            });
        }


        private void InitPassMessagesToSubscribers()
        {
            Receive<MqttMessageEvent>(mqttEvent =>
            {
                var i = 0;
                //todo resolve double match topic checking inside actor and here
                foreach (var sub in _subs
                    .Where(x => x.Type == TriggerTypeEnum.Mqtt
                                && x is SubToMqtt mx
                                && MqttHelper.IsSubscribed(patternTopic: mx.Topic, actualTopic: mqttEvent.Topic)
                                && !x.Subscriber.IsNobody()))
                {
                    i++;
                    sub.Subscriber.Tell(mqttEvent);
                }

                _logger.Debug($"{nameof(MqttMessageEvent)} delivered to {i} actors");
            });
            Receive<ActionResultEvent>(actionEvent =>
            {
                var i = 0;
                //todo resolve double match action checking inside actor and here
                foreach (var sub in _subs
                    .Where(x => x.Type == TriggerTypeEnum.Action
                                && x is SubToAction ma
                                && actionEvent.ActionName.Equals(ma.ActionName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    i++;
                    sub.Subscriber.Tell(actionEvent);
                }

                _logger.Debug($"{nameof(ActionResultEvent)} delivered to {i} actors");
            });
            
        }

        private void InitProxyToMqttBroker()
        {
            ReceiveAsync<MqttDoPublish>(async e =>
            {
                await _mqttClient.Publish(e.Topic, e.Message, e.Retained, e.Qos);
            });
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
            protected SubBase(TriggerTypeEnum type)
            {
                Type = type;
            }
            public TriggerTypeEnum Type { get;}
            public IActorRef Subscriber { get; set; }
        }

        public class SubToMqtt:SubBase
        {
            public string Topic { get; set; }

            public SubToMqtt() : base(TriggerTypeEnum.Mqtt)
            {
            }
        }

        public class SubToTime : SubBase
        {
            public string Cron { get; set; }

            public SubToTime() : base(TriggerTypeEnum.Time)
            {
            }
        }

        public class SubToAction : SubBase
        {
            public string ActionName { get; set; }

            public SubToAction() : base(TriggerTypeEnum.Action)
            {
            }
        }

    }

    
}
