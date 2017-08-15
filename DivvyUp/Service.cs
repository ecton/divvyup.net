using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using StackExchange.Redis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DivvyUp
{
    public class Service
    {
        private IDatabase _redis;
        private string _namespace;
        public Service(IDatabase redis = null, string ns = null)
        {
            _redis = redis ?? DivvyUp.RedisDatabase;
            _namespace = ns ?? DivvyUp.Namespace;
        }

        public Task Enqueue(Job job)
        {
            return Task.WhenAll(
                _redis.SetAddAsync($"{_namespace}::queues", job.Queue),
                _redis.ListRightPushAsync($"{_namespace}::queue::{job.Queue}", SerializeWork(job))
            );
        }

        private object GetWork(Job job)
        {
            return new
            {
                @class = job.GetType().AssemblyQualifiedName,
                queue = job.Queue,
                args = job.Arguments
            };
        }

        private string SerializeWork(Job job)
        {
            return JsonConvert.SerializeObject(GetWork(job));
        }

        internal Task Checkin(Worker worker)
        {
            return Task.WhenAll(
                _redis.HashSetAsync($"{_namespace}::workers", worker.WorkerId, DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                _redis.HashSetAsync($"{_namespace}::worker::{worker.WorkerId}", "queues", JsonConvert.SerializeObject(worker.Queues))
            );
        }

        internal async Task<Job> GetWork(Worker worker)
        {
            await ReclaimStuckWork(worker);
            var payload = await RetrieveNewWork(worker);
            if (payload == null) return null;

            var jobCls = Type.GetType(payload["class"].ToString());
            if (jobCls == null) throw new TypeLoadException($"{payload["class"]} not found.");

            var jsonArguments = payload["args"] as JArray;
            foreach (var constructor in jobCls.GetConstructors())
            {
                var parameters = constructor.GetParameters();
                var arguments = new object[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (i < jsonArguments.Count)
                    {
                        arguments[i] = jsonArguments[i].ToObject(parameters[i].ParameterType);
                    }
                    else if (parameters[i].HasDefaultValue)
                    {
                        arguments[i] = parameters[i].RawDefaultValue;
                    }
                    else
                    {
                        throw new ArgumentException($"No value provided for parameter {parameters[i].Name}");
                    }
                }
                return (Job)constructor.Invoke(arguments);
            }
            throw new TargetException($"Could not find constructor for {jobCls}");
        }

        internal Task StartWork(Worker worker, Job job)
        {
            return Task.WhenAll(
                _redis.HashSetAsync($"{_namespace}::worker::{worker.WorkerId}::job", "started_at", DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                _redis.HashSetAsync($"{_namespace}::worker::{worker.WorkerId}::job", "work", SerializeWork(job))
            );
        }

        internal Task CompleteWork(Worker worker, Job job)
        {
            return _redis.KeyDeleteAsync($"{_namespace}::worker::{worker.WorkerId}::job");
        }

        internal Task FailWork(Worker worker, Job job, Exception exc)
        {
            return _redis.ListRightPushAsync($"{_namespace}::failed", JsonConvert.SerializeObject(new
            {
                work = GetWork(job),
                worker = worker.WorkerId,
                message = exc.Message,
                backtrace = exc.StackTrace.Split(new string[] { Environment.NewLine }, StringSplitOptions.None)
            }));
        }

        private async Task ReclaimStuckWork(Worker worker)
        {
            var checkinThreshold = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 30 * 10;
            foreach (var entry in await _redis.HashGetAllAsync($"{_namespace}::workers"))
            {
                long lastCheckin;
                if (entry.Value.TryParse(out lastCheckin) && lastCheckin < checkinThreshold)
                {
                    await ReapWorker(entry.Name);
                }
            }
        }

        private async Task ReapWorker(string workerId)
        {
            /*job = @redis.hget("#{@namespace}::worker::#{worker_id}::job", 'work')
    if job
      job = JSON.parse(job)
      log(:info, 'Requeuing reaped work', id: worker_id, job: job)
      @redis.lpush("#{@namespace}::queue::#{job['queue']}", job.to_json)
    end
    @redis.del("#{@namespace}::worker::#{worker_id}::job")
    @redis.del("#{@namespace}::worker::#{worker_id}")
    @redis.hdel("#{@namespace}::workers", worker_id)*/
            var jobJson = await _redis.HashGetAsync($"{_namespace}::worker::{workerId}::job", "work");
            if (jobJson.HasValue)
            {
                var job = JObject.Parse(jobJson);
                await _redis.ListRightPushAsync($"{_namespace}::queue::{job["queue"]}", jobJson);
            }
            await Task.WhenAll(
                _redis.KeyDeleteAsync($"{_namespace}::worker::{workerId}::job"),
                _redis.KeyDeleteAsync($"{_namespace}::worker::{workerId}"),
                _redis.HashDeleteAsync($"{_namespace}::workers", workerId)
            );
        }

        private async Task<JObject> RetrieveNewWork(Worker worker)
        {
            foreach (var queue in worker.Queues)
            {
                var work = await _redis.ListLeftPopAsync($"{_namespace}::queue::{queue}");
                if (!work.HasValue) continue;
                return JObject.Parse(work.ToString());
            }
            return null;
        }
    }
}
