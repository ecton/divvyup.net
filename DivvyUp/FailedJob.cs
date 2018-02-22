using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace DivvyUp
{
    public class FailedJob
    {
        [JsonProperty("worker")]
        public string WorkerId { get; set; }

        [JsonProperty("work")]
        public JobStatus Job { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("backtrace")]
        public string[] Backtrace { get; set; }
    }
}
