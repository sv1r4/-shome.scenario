using System;
using System.Linq;
using Akka.Actor;
using Akka.DI.Core;
using Microsoft.Extensions.Logging;
using shome.scene.core.model;

namespace shome.scene.akka.actors
{
    public class SceneCreatorActor: ReceiveActor
    {
        private readonly ILogger _logger;
        public SceneCreatorActor(ILogger<SceneCreatorActor> logger)
        {
            _logger = logger;
            Receive<CreateScene>(e =>
            {
                SubscribeToSceneTriggers(e.SceneConfig);
            });
        }

        private void SubscribeToSceneTriggers(SceneConfig sceneConfig)
        {
            //stop prev version if exists
            var old = Context.System.ActorSelection($"/user/$a/$a/{sceneConfig.Name}-*");
            old.Tell(PoisonPill.Instance);

            //start new
            //todo pass config to scene actor
            var sceneActor = Context.ActorOf(Context.DI().Props<SceneActor>(), $"{sceneConfig.Name}-{Guid.NewGuid()}");
            sceneActor.Tell(new SceneActor.Init
            {
                Config =  sceneConfig
            });
        }

        public class CreateScene
        {
            public  SceneConfig SceneConfig { get; set; }
        }
    }
}
