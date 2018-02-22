using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace DivvyUp
{
    public class JobStatus
    {
        [JsonProperty("started_at")]
        public DateTimeOffset? StartedAt { get; set; }

        [JsonProperty("class")]
        public string Class { get; set; }
        [JsonProperty("queue")]
        public string Queue { get; set; }
        [JsonProperty("args")]
        public string[] Arguments { get; set; }
        [JsonProperty("retries")]
        public int? Retries { get; set; }
    }
}
