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
        private readonly KnownPaths _knownPaths;
        public SceneCreatorActor(ILogger<SceneCreatorActor> logger, KnownPaths knownPaths)
        {
            _logger = logger;
            _knownPaths = knownPaths;
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
            Context.ActorOf(SceneActor.Props(sceneConfig, _knownPaths), $"{sceneConfig.Name}-{Guid.NewGuid()}");
        }

        public class CreateScene
        {
            public  SceneConfig SceneConfig { get; set; }
        }


    }
}
