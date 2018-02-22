using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace DivvyUp
{
    public class WorkerStatus
    {
        public string Id { get; set; }
        public string[] Queues { get; set; }
        public DateTimeOffset LastCheckIn { get; set; }
        public JobStatus Job { get; set; }
    }
}
