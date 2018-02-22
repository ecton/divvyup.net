using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using StackExchange.Redis;

namespace DivvyUp
{
    public class DivvyUp
    {
        private static IConnectionMultiplexer _redis;
        public static IConnectionMultiplexer Redis
        {
            get
            {
                if (_redis == null)
                {
                    _redis = ConnectionMultiplexer.Connect("localhost:5379");
                }
                return _redis;
            }
            set => _redis = value;
        }

        public static IDatabase RedisDatabase { get => Redis.GetDatabase(); }

        public static string Namespace { get; set; } = "divvyup";

        public static Service Service { get => new Service(RedisDatabase, Namespace); }

        private static Dictionary<string, Type> _workers = new Dictionary<string, Type>();
        public static void RegisterJobsFromAssembly(Type type)
        {
            RegisterJobsFromAssembly(type.GetTypeInfo().Assembly);
        }
        public static void RegisterJobsFromAssembly(Assembly assembly)
        {
            lock (_workers)
            {
                foreach (var type in assembly.GetTypes().Where(t => t.GetTypeInfo().IsSubclassOf(typeof(Job))))
                {
                    _workers[type.FullName] = type;
                }
            }
        }
        internal static Type GetWorker(string name)
        {
            Type workerType;
            lock (_workers)
            {
                _workers.TryGetValue(name, out workerType);
                return workerType;
            }
        }
    }
}
