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
                    // // Update the partitionKey value.
                    // // The partitionKey property represents a synthetic composite partition key for the
                    // // Cosmos DB container, consisting of the VIN + current year/month. Using a composite
                    // // key instead of simply the VIN provides us with the following benefits:
                    // // (1) Distributing the write workload at any given point in time over a high cardinality
                    // // of partition keys.
                    // // (2) Ensuring efficient routing on queries on a given VIN - you can spread these across
                    // // time, e.g. SELECT * FROM c WHERE c.partitionKey IN (�VIN123-2019-01�, �VIN123-2019-02�, �)
                    // // (3) Scale beyond the 10GB quota for a single partition key value.
                    // vehicleEvent.partitionKey = $"{vehicleEvent.vin}-{DateTime.UtcNow:yyyy-MM}";
                    // // Set the TTL to expire the document after 60 days.
                    // vehicleEvent.ttl = 60 * 60 * 24 * 60;
                    // vehicleEvent.timestamp = DateTime.UtcNow;

                    await smokerStatusOut.AddAsync(smokerStatus);

                    //await Task.Yield();
                }
                catch (Exception e)
                {
                    // We need to keep processing the rest of the batch - capture this exception and continue.
                    // Also, consider capturing details of the message that failed processing so it can be processed again later.
                    exceptions.Add(e);
                }
            }
            // Once processing of the batch is complete, if any messages in the batch failed processing throw an exception so that there is a record of the failure.

            if (exceptions.Count > 1)
                throw new AggregateException(exceptions);

            if (exceptions.Count == 1)
                throw exceptions.Single();

        }
    }

}