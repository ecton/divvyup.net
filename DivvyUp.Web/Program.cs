using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.CommandLineUtils;
using StackExchange.Redis;

namespace DivvyUp.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var app = new CommandLineApplication(false);
            var redisOption = app.Option("-r|--redis <host:port>", "Specify the redis host and port to connect to", CommandOptionType.SingleValue);
            var namespaceOption = app.Option("-n|--namespace <namespace>", "Specify the DivvyUp namespace inside of redis", CommandOptionType.SingleValue);
            app.OnExecute(() => StartupApplication(args, redisOption, namespaceOption));
            app.Execute(args);
        }

        private static int StartupApplication(string[] args, CommandOption redisOption, CommandOption namespaceOption)
        {
            if (redisOption.HasValue())
            {
                DivvyUp.Redis = ConnectionMultiplexer.Connect(redisOption.Value());
            }
            else if (Environment.GetEnvironmentVariable("DIVVYUP_REDIS") != null)
            {
                DivvyUp.Redis = ConnectionMultiplexer.Connect(Environment.GetEnvironmentVariable("DIVVYUP_REDIS"));
            }

            if (namespaceOption.HasValue())
            {
                DivvyUp.Namespace = namespaceOption.Value();
            }
            else if (Environment.GetEnvironmentVariable("DIVVYUP_NAMESPACE") != null)
            {
                DivvyUp.Redis = ConnectionMultiplexer.Connect(Environment.GetEnvironmentVariable("DIVVYUP_NAMESPACE"));
            }

            BuildWebHost(args).Run();

            return 0;
        }

        private static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .Build();
    }
}
