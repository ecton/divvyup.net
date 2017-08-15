using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DivvyUp
{
    public abstract class Job
    {
        public abstract string Queue { get; }
        public object[] Arguments { get; }

        public Job(params object[] arguments)
        {
            Arguments = arguments;
        }

        public abstract Task Execute();
    }
}
