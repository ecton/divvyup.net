using System;
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

        public static string Namespace { get; } = "divvyup";

        public static Service Service { get => new Service(RedisDatabase, Namespace); }
    }
}
