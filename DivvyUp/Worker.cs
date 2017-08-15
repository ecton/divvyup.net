using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DivvyUp
{
    public class Worker
    {
        private Service _service;
        public string[] Queues { get; }
        public string WorkerId { get; }

        public int CheckinInterval { get; set; } = 30;
        public int DelayAfterInternalError { get; set; } = 5;

        public Worker(params string[] queues) : this(DivvyUp.Service, queues) { }
        public Worker(Service service, params string[] queues)
        {
            _service = service;
            Queues = queues;
            WorkerId = $"{Dns.GetHostName()}:{Guid.NewGuid()}";
        }

        public delegate void OnErrorDelegate(Exception exc);
        public event OnErrorDelegate OnError;

        public async Task Work(bool forever = true)
        {
            StartupBackgroundCheckin(forever);
            while (true)
            {
                try
                {
                    await RetrieveAndExecuteWork();
                }
                catch (Exception exc)
                {
                    OnError(exc);
                    Thread.Sleep(DelayAfterInternalError * 1000);
                }
                if (!forever) break;
            }
        }

        private Thread _backgroundCheckinThread;
        private void StartupBackgroundCheckin(bool forever)
        {
            if (forever)
            {
                _backgroundCheckinThread = new Thread(() => BackgroundCheckin(forever));
                _backgroundCheckinThread.Start();
            }
            else
            {
                BackgroundCheckin(forever);
            }
        }

        private void BackgroundCheckin(bool forever)
        {
            while (true)
            {
                try
                {
                    _service.Checkin(this).Wait();
                }
                catch (Exception exc)
                {
                    OnError(exc);
                }
                if (!forever) break;
                Thread.Sleep(CheckinInterval * 1000);
            }
        }

        private async Task RetrieveAndExecuteWork()
        {
            var job = await _service.GetWork(this);
            if (job != null)
            {
                try
                {
                    await _service.StartWork(this, job);
                    await job.Execute();
                    await _service.CompleteWork(this, job);
                }
                catch (Exception exc)
                {
                    await _service.FailWork(this, job, exc);
                    OnError(exc);
                }
            }
            else
            {
                Thread.Sleep(5 * 1000);
            }
        }
    }
}
