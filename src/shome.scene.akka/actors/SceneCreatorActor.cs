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
            //create scene actor
            //todo remove old actors
            //Context.GetChildren().Select(x=>x.)
            var sceneActor = Context.ActorOf(Context.DI().Props<SceneActor>());

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

        public class CreateScene
        {
            public  SceneConfig SceneConfig { get; set; }
        }
    }
}
