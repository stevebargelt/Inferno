using System;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Inferno.Common.Models
{
    public class Smoker
    {
        [JsonProperty] public string PartitionKey => Id;
        [JsonProperty] public string Id { get; set; }
        [JsonProperty] public string Name { get; set; }
    }
}