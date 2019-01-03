using System;
using System.Threading.Tasks;
using Akka.Actor;
using Microsoft.Extensions.Logging;
using Quartz;
using shome.scene.core.events;

namespace shome.scene.akka.util.quartz
{
    public class TellScheduleJob : IJob
    {
        public static readonly string JobDataActor = "actor";
        private readonly ILogger _logger;

        public TellScheduleJob(ILogger<TellScheduleJob> logger)
        {
            _logger = logger;
        }

        public Task Execute(IJobExecutionContext context)
        {
            if (!(context.MergedJobDataMap.Get(JobDataActor) is IActorRef actor))
            {
                throw new InvalidOperationException($"JobDataMap[{JobDataActor}] should not be empty");
            }

            _logger.LogInformation($"tell '{nameof(ScheduleEvent)}' to {actor.Path.Name}");
            actor.Tell(new ScheduleEvent());
            return Task.CompletedTask;
        }
    }
}
