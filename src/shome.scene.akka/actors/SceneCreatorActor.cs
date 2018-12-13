using System;
using Akka.Actor;
using shome.scene.core.model;

namespace shome.scene.akka.actors
{
    public class SceneCreatorActor: ReceiveActor
    {
        public SceneCreatorActor(KnownPaths knownPaths)
        {
            //upsert
            Receive<CreateScene>(e =>
            {
                //stop prev version if exists
                var old = Context.System.ActorSelection($"/user/$a/$a/{e.SceneConfig.Name}-*");
                old.Tell(PoisonPill.Instance);

                var salt = DateTime.Now.Ticks.ToString();
                //start new
                Context.ActorOf(SceneActor.Props(e.SceneConfig, knownPaths), $"{e.SceneConfig.Name}-{salt}");
            });
            //delete
            Receive<RemoveScene>(e =>
            {
                //stop prev version if exists
                var old = Context.System.ActorSelection($"/user/$a/$a/{e.SceneConfig.Name}-*");
                old.Tell(PoisonPill.Instance);
            });
        }


        public class CreateScene
        {
            public  SceneConfig SceneConfig { get; set; }
        }

        public class RemoveScene
        {
            public SceneConfig SceneConfig { get; set; }
        }


    }
}
