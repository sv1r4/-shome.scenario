using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Quartz;
using shome.scene.akka.actors;
using IScheduler = Quartz.IScheduler;

namespace shome.scene.akka.util.quartz
{
    public class QuartzActionScheduler : ISceneActionScheduler
    {
        private readonly IScheduler _quartzScheduler;
        private readonly ILogger _logger;

        public QuartzActionScheduler(IScheduler quartzScheduler, ILogger<QuartzActionScheduler> logger)
        {
            _quartzScheduler = quartzScheduler;
            _logger = logger;
        }


        public async Task ScheduleAction(PubSubProxyActor.SubToTime sub)
        {
            var jobData = new JobDataMap((IDictionary<string, object>)new Dictionary<string, object>
            {
                {TellScheduleJob.JobDataActor, sub.Subscriber}
            });

            var jobName = sub.GetJobName();
            var triggerName = sub.GetTriggerName();
            var cron = sub.Cron;

            var job = JobBuilder.Create<TellScheduleJob>()
                .WithIdentity(jobName)
                .SetJobData(jobData)
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity(triggerName)
                .WithCronSchedule(cron)
                .Build();

            _logger?.LogInformation($"Schedule tell to '{sub.Subscriber.Path.Name}' {CronExpressionDescriptor.ExpressionDescriptor.GetDescription(sub.Cron)}");
            await _quartzScheduler.ScheduleJob(job, trigger);
        }

        public async Task UnScheduleAction(PubSubProxyActor.SubBase sub)
        {
            var triggerName = sub.GetTriggerName();
            _logger?.LogInformation($"UnSchedule job with trigger '{triggerName}'.");
            await _quartzScheduler.UnscheduleJob(new TriggerKey(triggerName));
        }

    }
}
