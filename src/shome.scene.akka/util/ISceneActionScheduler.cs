using System.Threading.Tasks;
using shome.scene.akka.actors;

namespace shome.scene.akka.util
{
    public interface ISceneActionScheduler
    {
        Task ScheduleAction(PubSubProxyActor.SubToTime sub);
        Task UnScheduleAction(PubSubProxyActor.SubBase sub);
    }

}
