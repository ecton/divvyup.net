using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Newtonsoft.Json;

namespace DivvyUp.Tests
{
    public class IntegrationTests
    {
        public class TestJob : Job
        {
            string _command;
            public TestJob(string command = "default") : base(command)
            {
                _command = command;
            }
            public override string Queue => "test";

            private static Dictionary<string, int> _counters = new Dictionary<string, int>();

            public override Task Execute()
            {
                if (_command == "fail") throw new ArgumentException("Expected to fail");
                if (_command == "delay") Thread.Sleep(100);

                lock (_counters)
                {
                    int current;
                    _counters.TryGetValue(_command, out current);
                    _counters[_command] = current + 1;
                    return Task.CompletedTask;
                }
            }

            public static int Count(string command)
            {
                lock (_counters)
                {
                    int result;
                    _counters.TryGetValue(command, out result);
                    return result;
                }
            }
        }

        [Fact]
        public async Task FullTest()
        {
            var service = new Service(new MockRedisDatabase());
            service.RegisterWorkersFromAssembly(typeof(TestJob));
            var worker = new Worker(service, "test");
            Exception workerException = null;
            worker.OnError += (exc) => workerException = exc;
            worker.CheckinInterval = 1;
            worker.DelayAfterInternalError = 0;
            await service.Enqueue(new TestJob("explicit"));
            await worker.Work(false);

            Assert.Equal(1, TestJob.Count("explicit"));

            await service.Enqueue(new TestJob("fail"));
            await worker.Work(false);
            Assert.NotNull(workerException);

            await worker.Work(false);
        }

        [Fact]
        public async Task ReclaimStuckWork()
        {
            var redis = new MockRedisDatabase();
            await redis.HashSetAsync($"divvyup::workers", "badworkerbad", DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 60 * 60);
            await redis.HashSetAsync($"divvyup::worker::badworkerbad::job", "started_at", DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 60 * 60);
            await redis.HashSetAsync($"divvyup::worker::badworkerbad::job", "work", JsonConvert.SerializeObject(new
            {
                @class = typeof(TestJob).FullName,
                queue = "test",
                args = new object[0]
            }));

            var service = new Service(redis);
            service.RegisterWorkersFromAssembly(typeof(TestJob));
            var worker = new Worker(service, "test");
            worker.CheckinInterval = 1;
            worker.DelayAfterInternalError = 0;
            await worker.Work(false);

            Assert.Equal(1, TestJob.Count("default"));
        }

        [Fact]
        public async Task WorkerPool()
        {
            var service = new Service(new MockRedisDatabase());
            service.RegisterWorkersFromAssembly(typeof(TestJob));
            var pool = new WorkerPool(service);
            pool.AddWorker("test");
            pool.AddWorkers(3, "test");
            foreach (var worker in pool.Workers)
            {
                worker.CheckinInterval = 1;
                worker.DelayAfterInternalError = 0;
                worker.NoWorkCheckInterval = 0;
            }
            pool.WorkInBackground();
            for (int i = 0; i < 40; i++)
            {
                await service.Enqueue(new TestJob("delay"));
            }

            var start = DateTime.UtcNow;
            while (DateTime.UtcNow - start < TimeSpan.FromSeconds(3.5)) ;

            await pool.Stop();

            Assert.Equal(40, TestJob.Count("delay"));
        }

        [Fact]
        public async Task RetryTest()
        {
            var service = new Service(new MockRedisDatabase());
            service.RegisterWorkersFromAssembly(typeof(TestJob));
            var worker = new Worker(service, "test");
            Exception workerException = null;
            worker.OnError += (exc) => workerException = exc;
            worker.CheckinInterval = 1;
            worker.DelayAfterInternalError = 0;
            var job = new TestJob("fail");
            job.Retries = 1;
            await service.Enqueue(job);
            await worker.Work(false);
            Assert.NotNull(workerException);
            workerException = null;
            await worker.Work(false);
            Assert.NotNull(workerException);

            await worker.Work(false);
        }
    }
}