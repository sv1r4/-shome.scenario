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
            //todo pass config to scene actor
            var sceneActor = Context.ActorOf(Context.DI().Props<SceneActor>(), $"{sceneConfig.Name}-{Guid.NewGuid()}");
            
            foreach (var topic in sceneConfig.Actions.SelectMany(x => x.If).Select(x => x.Topic))
            {
                _logger.LogDebug($"Tell Subscribe to topic='{topic}'");
                
                //subscribe scene actor for messages
                Context.System.ActorSelection(_knownPaths.PubSubActorPath).Tell(new PubSubActor.Sub
                {
                    Topic = topic,
                    Subscriber = sceneActor
                });
            }
        }

        public class CreateScene
        {
            public  SceneConfig SceneConfig { get; set; }
        }
    }
}
