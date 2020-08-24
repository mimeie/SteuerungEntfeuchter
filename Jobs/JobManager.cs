
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quartz;
using Quartz.Impl;




namespace SteuerungEntfeuchter
{
    /// <summary>
    /// Handelt in der Datenbank konfigurierte Jobs (Config.Devices). Initialisiert die Jobs gemäss Konfiguration.
    /// Zudem werden wiederkehrende Systemaufgaben von dieser Klasse verwaltet.
    /// </summary>
    public class JobManager : IDisposable
    {
  
        private IScheduler _scheduler;
        private StdSchedulerFactory _factory;

        public JobManager()
        {
            
        }

        public async void Initialize()
        {
            _factory = new StdSchedulerFactory();
            _scheduler = await _factory.GetScheduler();

            await _scheduler.Start();

            InitializeJobs();
        }

        public void Dispose()
        {
            if (_scheduler != null)
            {
                _scheduler.Shutdown();
                _scheduler = null;
            }
        }

        private void InitializeJobs()
        {
            try
            {

                       
                InitializeFeuchtigkeitPollJob();
              
            }
            catch (Exception ex)
            {
                Console.WriteLine("Jobs konnten nicht initialisiert werden: {0}", ex);                
                throw;
            }
        }
            

        private void InitializeFeuchtigkeitPollJob()
        {
            IJobDetail feuchtigkeitPollJob = JobBuilder.Create<FeuchtigkeitPollJob>()
                .WithIdentity("FeuchtigkeitPollJob")
                .Build();

            // Trigger the job to run now, and then repeat every x seconds
            ITrigger feuchtigkeitPollTrigger = TriggerBuilder.Create()
                .WithIdentity("FeuchtigkeitPollJobTrigger")
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithIntervalInMinutes(SteuerungLogic.Instance.KellerSensor.CheckIntervallMinutes)
                    .RepeatForever())
                .Build();

            _scheduler.ScheduleJob(feuchtigkeitPollJob, feuchtigkeitPollTrigger);
            Console.WriteLine("FeuchtigkeitPollJob mit Interval {0}min gestartet", SteuerungLogic.Instance.KellerSensor.CheckIntervallMinutes);
        }


        
    }
}
