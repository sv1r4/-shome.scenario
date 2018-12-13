using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Akka.DI.Core;
using Akka.Event;
using shome.scene.core.contract;
using shome.scene.core.model;

namespace shome.scene.akka.actors
{
    public class SceneConfigReaderActor : ReceiveActor
    {
        private readonly IActorRef _creatorActor;
        private readonly ILoggingAdapter _logger = Context.GetLogger();
        private IReadOnlyList<SceneConfig> _configs = Array.Empty<SceneConfig>();

        public SceneConfigReaderActor(ISceneProvider sceneProvider)
        {
            Receive<GetScenesConfig>(m =>
            {
                var newConfigs = sceneProvider.GetConfigs().ToList();
                var removedConfigs = _configs.Where(x => newConfigs.All(nx => !x.Name.Equals(nx.Name))).ToList();
                foreach (var removedConfig in removedConfigs)
                {
                    _creatorActor.Tell(new SceneCreatorActor.RemoveScene
                    {
                        SceneConfig = removedConfig
                    });
                }
                _configs = newConfigs;
                foreach (var scene in _configs)
                {
                    _creatorActor.Tell(new SceneCreatorActor.CreateScene
                    {
                        SceneConfig = scene
                    });
                }
            });

            _logger.Debug($"Create scene Creator {nameof(SceneCreatorActor)}");
            var props = Context.DI().Props<SceneCreatorActor>();
            _creatorActor = Context.ActorOf(props);
        }

        public class GetScenesConfig
        {
        }
    }
}
