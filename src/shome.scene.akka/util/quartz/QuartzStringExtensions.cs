using shome.scene.akka.actors;

namespace shome.scene.akka.util.quartz
{
    public static class QuartzStringExtensions
    {
        public static string GetJobName(this PubSubProxyActor.SubBase sub)
        {
            return $"job-{sub.Subscriber.Path.Name}";
        }

        public static string GetTriggerName(this PubSubProxyActor.SubBase sub)
        {
            return $"trigger-{sub.Subscriber.Path.Name}";
        }
    }
}
