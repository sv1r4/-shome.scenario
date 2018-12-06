using System;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Microsoft.Extensions.Logging;
using shome.scene.core.model;
using shome.scene.mqtt.contract;

namespace shome.scene.akka.actors
{
    public class SceneCreatorActor: ReceiveActor
    {
        private readonly ILogger _logger;
        private readonly IMqttBasicClient _mqtt;
        public SceneCreatorActor(ILogger<SceneCreatorActor> logger, IMqttBasicClient mqtt)
        {
            _logger = logger;
            _mqtt = mqtt;
            ReceiveAsync<SceneConfig>(async s =>
            {
                await SubscribeToSceneTriggers(s);
            });
        }

        private async Task SubscribeToSceneTriggers(SceneConfig sceneConfig)
        {
            foreach (var topic in sceneConfig.Actions.SelectMany(x => x.If).Select(x => x.Topic))
            {
                _logger.LogDebug($"Subscribe topic='{topic}'");
                await _mqtt.Subscribe(topic);
            }
        }

        //protected override void PreStart()
        //{
        //    base.PreStart();
        //    _logger.LogDebug($"{nameof(SceneCreatorActor)} start");
        //}

        //protected override void PostStop()
        //{
        //    base.PostStop();
        //    _logger.LogDebug($"{nameof(SceneCreatorActor)} stop");
        //}
    }
}
