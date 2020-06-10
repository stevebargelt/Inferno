using System;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Inferno.Common.Models
{
    public class SmokerStatus
    {
        /// <summary>
        /// The partitionKey property represents a synthetic composite partition key for the
        /// Cosmos DB container, consisting of the device ID + current year/month. Using a composite
        /// key instead of simply the device ID provides us with the following benefits:
        /// (1) Distributing the write workload at any given point in time over a high cardinality
        /// of partition keys.
        /// (2) Ensuring efficient routing on queries on a given deice ID - you can spread these across
        /// time, e.g. SELECT * FROM c WHERE c.partitionKey IN (“123-2020-01”, “123-2020-02”, …)
        /// (3) Scale beyond the 10GB quota for a single partition key value.
        /// </summary>
        [JsonProperty("partitionKey")] public string PartitionKey { get; set; }
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty] public string SmokerId { get; set; }
        [JsonProperty] public bool AugerOn { get; set; }
        [JsonProperty] public bool BlowerOn { get; set; }
        [JsonProperty] public bool IgniterOn { get; set; }
        [JsonProperty] public Temps Temps { get; set; }
        [JsonProperty] public bool FireHealthy { get; set; }
        [JsonProperty] public string Mode { get; set; }
        [JsonProperty] public int SetPoint { get; set; }
        [JsonProperty] public DateTime ModeTime { get; set; }
        [JsonProperty] public DateTime CurrentTime { get; set; }
    }
}