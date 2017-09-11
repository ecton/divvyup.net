using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DivvyUp
{
    public class WorkerPool
    {
        private Service _service;
        public WorkerPool(Service service = null)
        {
            _service = service ?? DivvyUp.Service;
        }

        private Dictionary<Worker,Thread> _workers = new Dictionary<Worker, Thread>();
        public IEnumerable<Worker> Workers { get => _workers.Keys; }
        public void AddWorker(params string[] queues)
        {
            AddWorkers(1, queues);
        }

        public void AddWorkers(int count, params string[] queues)
        {
            for (int i = 0; i < count; i++)
            {
                _workers.Add(new Worker(_service, queues), null);
            }
        }
        
        public void WorkInBackground()
        {
            foreach (var worker in _workers.Keys.ToArray())
            {
                if (_workers[worker] != null) continue;
                _workers[worker] = new Thread(() => worker.Work().Wait());
                _workers[worker].Name = "DivvyUp Worker";
                _workers[worker].Start();
            }
        }

        public Task Stop()
        {
            return Task.WhenAll(_workers.Select(w => w.Key.Stop()));
        }

        public void Shutdown()
        {
            foreach (var worker in _workers) worker.Key.Shutdown();
        }
    }
}
