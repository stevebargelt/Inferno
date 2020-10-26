using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
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
    public class Chart
    {
        private readonly CosmosClient _cosmosClient;

        // Use Dependency Injection to inject the HttpClientFactory service and Cosmos DB client that were configured in Startup.cs.
        public Chart(CosmosClient cosmosClient)
        {
            _cosmosClient = cosmosClient;
        }
        /// <summary>
        /// Get Session Charts by ID
        /// </summary>
        /// <param name="id"></param>
        /// <param name="timeseries"></param>
        /// <returns></returns>
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(Session[]))]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError, Type = typeof(Error))]
        [ProducesResponseType((int)HttpStatusCode.NotFound, Type = typeof(Error))]
        [FunctionName("GetSessionChartById")]
        public  async Task<IActionResult> GetSessionChartById(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "session/{id}/chart/{timeseries?}")] HttpRequest req, 
                [CosmosDB(
                databaseName: "Inferno",
                collectionName: "sessions",
                ConnectionStringSetting = "CosmosDBConnection",
                Id = "{id}", PartitionKey = "inferno1")] Object sessionObject,
            int? timeseries,
            ILogger log)        
        {
            if (sessionObject == null)
            {
                log.LogInformation($"sessionObject not found");
                return new NotFoundResult();
            }
            var session = JsonConvert.DeserializeObject<Session>(sessionObject.ToString());

            if (session == null)
            {
                log.LogInformation($"Session not found");
                return new NotFoundResult();
            }

            log.LogInformation("SmokerId "+session.SmokerId);
            log.LogInformation("SessionId "+session.Id);
            var StatusPartitionKey = $"{session.SmokerId}-{session.StartTime:yyyy-MM}";
            log.LogInformation($"Status PartitionKey = {StatusPartitionKey}");

            var EndTime = session.EndTime.HasValue ? session.EndTime : DateTime.UtcNow;
            log.LogInformation($"StartTime = {session.StartTime} EndTime = {EndTime}");

            var container = _cosmosClient.GetContainer("Inferno", "status");
            // Create a query, defining the partition key so we don't execute a fan-out query (saving RUs), 
            //      where the entity type is a Trip and the status is not Completed, Canceled, or Inactive.
            // var query = container.GetItemLinqQueryable<SmokerStatus>(requestOptions: new QueryRequestOptions { PartitionKey = new Microsoft.Azure.Cosmos.PartitionKey(StatusPartitionKey) })
            //     .Where(p => p.CurrentTime >= session.StartTime
            //                 && p.CurrentTime <= EndTime)                            
            //     .ToFeedIterator();
            var query = container.GetItemLinqQueryable<SmokerStatus>(requestOptions: new QueryRequestOptions { PartitionKey = new Microsoft.Azure.Cosmos.PartitionKey(StatusPartitionKey) })
                .Where(p => p.SessionId == session.Id)
                .ToFeedIterator();

            List<SmokerStatus> SmokerStatuses = new List<SmokerStatus>();
            var count = 0;
            while (query.HasMoreResults)
            {
                foreach(var status in await query.ReadNextAsync())
                {
                    count++;
                    SmokerStatuses.Add(status);
                }
            }
            log.LogInformation("Statuses " + count);

            if (!timeseries.HasValue) {
                return new OkObjectResult(SmokerStatuses);
            }

            if (timeseries > 0 && timeseries <=60)
            {
                TimeSpan interval = new TimeSpan(0, timeseries.Value, 0); 
                List<SmokerStatus> SortedList = SmokerStatuses.OrderBy(o => o.CurrentTime).ToList();
                var result = SortedList.GroupBy(x=> x.CurrentTime.Ticks/interval.Ticks)
                        .Select(x=>x.First());
                return new OkObjectResult(result);
           
            }
            // Return a 400 bad request result to the client with additional information
            return new BadRequestObjectResult("Please pass a timeseries in range of 1 to 60");

        }        
        
                /// <summary>
        /// Get Session Charts by ID
        /// </summary>
        /// <param name="id"></param>
        /// <param name="timeseries"></param>
        /// <returns></returns>
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(Session[]))]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError, Type = typeof(Error))]
        [ProducesResponseType((int)HttpStatusCode.NotFound, Type = typeof(Error))]
        [FunctionName("GetSessionChartByIdUsingTime")]
        public  async Task<IActionResult> GetSessionChartByIdUsingTime(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "session/{id}/chartusingtime/{timeseries?}")] HttpRequest req, 
                [CosmosDB(
                databaseName: "Inferno",
                collectionName: "sessions",
                ConnectionStringSetting = "CosmosDBConnection",
                Id = "{id}", PartitionKey = "inferno1")] Object sessionObject,
            int? timeseries,
            ILogger log)        
        {
            if (sessionObject == null)
            {
                log.LogInformation($"sessionObject not found");
                return new NotFoundResult();
            }
            var session = JsonConvert.DeserializeObject<Session>(sessionObject.ToString());

            if (session == null)
            {
                log.LogInformation($"Session not found");
                return new NotFoundResult();
            }

            log.LogInformation("SmokerId "+session.SmokerId);
            log.LogInformation("SessionId "+session.Id);
            var StatusPartitionKey = $"{session.SmokerId}-{session.StartTime:yyyy-MM}";
            log.LogInformation($"Status PartitionKey = {StatusPartitionKey}");

            var EndTime = session.EndTime.HasValue ? session.EndTime : DateTime.UtcNow;
            log.LogInformation($"StartTime = {session.StartTime} EndTime = {EndTime}");

            var container = _cosmosClient.GetContainer("Inferno", "status");

            Microsoft.Azure.Cosmos.FeedIterator<SmokerStatus> query;
            if (session.EndTime.HasValue) {
                query = container.GetItemLinqQueryable<SmokerStatus>(requestOptions: new QueryRequestOptions { PartitionKey = new Microsoft.Azure.Cosmos.PartitionKey(StatusPartitionKey) })
                        .Where(p => p.CurrentTime >= session.StartTime
                                && p.CurrentTime <= EndTime)                            
                        .ToFeedIterator();
            } else {
                query = container.GetItemLinqQueryable<SmokerStatus>(requestOptions: new QueryRequestOptions { PartitionKey = new Microsoft.Azure.Cosmos.PartitionKey(StatusPartitionKey) })
                        .Where(p => p.CurrentTime >= session.StartTime)
                        .ToFeedIterator();
            }

            List<SmokerStatus> SmokerStatuses = new List<SmokerStatus>();
            var count = 0;
            while (query.HasMoreResults)
            {
                foreach(var status in await query.ReadNextAsync())
                {
                    count++;
                    SmokerStatuses.Add(status);
                }
            }
            log.LogInformation("Statuses " + count);

            if (!timeseries.HasValue) {
                return new OkObjectResult(SmokerStatuses);
            }

            if (timeseries > 0 && timeseries <=60)
            {
                TimeSpan interval = new TimeSpan(0, timeseries.Value, 0); 
                List<SmokerStatus> SortedList = SmokerStatuses.OrderBy(o => o.CurrentTime).ToList();
                var result = SortedList.GroupBy(x=> x.CurrentTime.Ticks/interval.Ticks)
                        .Select(x=>x.First());
                return new OkObjectResult(result);
           
            }
            // Return a 400 bad request result to the client with additional information
            return new BadRequestObjectResult("Please pass a timeseries in range of 1 to 60");

        }  
    }
}
