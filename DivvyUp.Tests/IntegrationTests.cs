using System;
using System.Collections.Generic;
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
                lock (_counters)
                {
                    if (_command == "fail") throw new ArgumentException("Expected to fail");

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
                @class = typeof(TestJob).AssemblyQualifiedName,
                queue = "test",
                args = new object[0]
            }));

            var service = new Service(redis);
            var worker = new Worker(service, "test");
            worker.CheckinInterval = 1;
            worker.DelayAfterInternalError = 0;
            await worker.Work(false);

            Assert.Equal(1, TestJob.Count("default"));
        }
    }
}