using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using Inferno.Common.Models;

namespace Inferno.Functions
{
    public class SessionStatus
    {
        private readonly CosmosClient _cosmosClient;

        // Use Dependency Injection to inject the HttpClientFactory service and Cosmos DB client that were configured in Startup.cs.
        public SessionStatus(CosmosClient cosmosClient)
        {
            _cosmosClient = cosmosClient;
        }

        [FunctionName("EndSession")]
        public async void Run([CosmosDBTrigger(
            databaseName: "Inferno",
            collectionName: "sessions",
            ConnectionStringSetting = "CosmosDBConnection",
            LeaseCollectionName = "leases",
            LeaseCollectionPrefix = "sessions",
            CreateLeaseCollectionIfNotExists = true,
            StartFromBeginning = true)]IReadOnlyList<Document> sessionEvents,
            ILogger log)
        {
            var sessionEvent = sessionEvents.FirstOrDefault();

            var session = JsonConvert.DeserializeObject<Session>(sessionEvents.FirstOrDefault().ToString());

            log.LogInformation("SmokerId "+session.SmokerId);
            log.LogInformation("SessionId "+session.Id);
            var StatusPartitionKey = $"{session.SmokerId}-{session.StartTime:yyyy-MM}";
            log.LogInformation($"Status PartitionKey = {StatusPartitionKey}");

            var EndTime = session.EndTime.HasValue ? session.EndTime : DateTime.UtcNow;
            log.LogInformation($"StartTime = {session.StartTime} EndTime = {EndTime}");
            log.LogInformation($"UTC StartTime = {session.StartTime} UTC EndTime = {EndTime}");

            var container = _cosmosClient.GetContainer("Inferno", "status");
            // Create a query, defining the partition key so we don't execute a fan-out query (saving RUs), 
            //      where the entity type is a Trip and the status is not Completed, Canceled, or Inactive.
            var query = container.GetItemLinqQueryable<SmokerStatus>(requestOptions: new QueryRequestOptions { PartitionKey = new Microsoft.Azure.Cosmos.PartitionKey(StatusPartitionKey) })
                .Where(p => p.CurrentTime >= session.StartTime
                            && p.CurrentTime <= EndTime)                            
                .ToFeedIterator();

            List<Task> concurrentTasks = new List<Task>();
            var count = 0;

            while (query.HasMoreResults)
            {
                foreach(var status in await query.ReadNextAsync())
                {
                    count++;
                    // log.LogInformation("StatusID "+ status.Id);
                    status.SessionId = session.Id;
                    var tsk = container.UpsertItemAsync(status, new Microsoft.Azure.Cosmos.PartitionKey(StatusPartitionKey));
                    concurrentTasks.Add(tsk);
                }
            }
            log.LogInformation("Statuses " + count);
            count = 0;
            log.LogInformation("Started : " + DateTime.Now.ToLongTimeString());
            await Task.WhenAll(concurrentTasks);
            log.LogInformation("Ended : " + DateTime.Now.ToLongTimeString());

        }        
        
    }
}
