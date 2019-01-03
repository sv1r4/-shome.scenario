using System;
using Quartz;
using Quartz.Spi;

namespace shome.scene.processor.quartz
{
    public class DiJobFactory: IJobFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public DiJobFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            return _serviceProvider.GetService(bundle.JobDetail.JobType) as IJob;
        }

        public void ReturnJob(IJob job)
        {
            throw new NotImplementedException();
        }
    }
}
