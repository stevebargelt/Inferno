using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using IoTHubTrigger = Microsoft.Azure.WebJobs.EventHubTriggerAttribute;
using Inferno.Common.Models;


namespace Inferno.Functions
{
    public static class InfernoIoTHubTrigger
    {
        [FunctionName("InfernoIoTHubTrigger")]
        public static async Task IoTHubTrigger([IoTHubTrigger("messages/events", Connection = "IoTHubConnection")] SmokerStatus[] smokerStatuses,
            [CosmosDB(
                databaseName: "Inferno",
                collectionName: "status",
                ConnectionStringSetting = "CosmosDBConnection")]
            IAsyncCollector<SmokerStatus> smokerStatusOut,
            ILogger log)
        {
            var exceptions = new List<Exception>();
            log.LogInformation($"IoT Hub trigger function processing {smokerStatuses.Length} events.");

            foreach (var smokerStatus in smokerStatuses)
            {
                try
                {
                    //var messageBody = Encoding.UTF8.GetString(smokerStatusData.Body.Array, smokerStatusData.Body.Offset, smokerStatusData.Body.Count);
                    var smokerStatusString = JsonConvert.SerializeObject(smokerStatus);
                    log.LogInformation($"SmokerStatus: {smokerStatusString}"); 
                    smokerStatus.PartitionKey = $"inferno1-{DateTime.UtcNow:yyyy-MM}";
                    // Set the TTL to expire the document after 15 days if it's not set elsewhere.
                    //  - if it's set elsewhere the data might be part of a "cook" /session, if not
                    //    we can purge the data in 15 days.
                    if (smokerStatus.ttl is null || smokerStatus.ttl == 0 || smokerStatus.ttl == -1) {
                        smokerStatus.ttl = 60 * 60 * 24 * 15;
                    }
                    // vehicleEvent.timestamp = DateTime.UtcNow;
                    await smokerStatusOut.AddAsync(smokerStatus);
                }
                catch (Exception e)
                {
                    // We need to keep processing the rest of the batch - capture this exception and continue.
                    // Also, consider capturing details of the message that failed processing so it can be processed again later.
                    exceptions.Add(e);
                }
            }
 
            // Once processing of the batch is complete, if any messages in the batch failed processing throw an exception so that 
            //      there is a record of the failure.
            if (exceptions.Count > 1)
                throw new AggregateException(exceptions);

            if (exceptions.Count == 1)
                throw exceptions.Single();

        }
    }

}