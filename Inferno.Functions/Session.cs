using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Inferno.Common.Models;

namespace Inferno.Functions
{
    public static class Sessions
    {
        [FunctionName("CreateSession")]
        public static async Task<IActionResult> CreateSession(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session")] HttpRequest req, 
                [CosmosDB(
                databaseName: "Inferno",
                collectionName: "sessions",
                ConnectionStringSetting = "CosmosDBConnection")] IAsyncCollector<Session> sessions,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            try 
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var input = JsonConvert.DeserializeObject<Session>(requestBody);

                var session = new Session {
                    PartitionKey = $"{input.SmokerId}-{DateTime.UtcNow:yyyy-MM}",
                    SmokerId = input.SmokerId,
                    Title = input.Title,
                    Description = input.Description,
                    StartTime = input.StartTime,
                    EndTime = input.EndTime,
                    TimeStamp = DateTime.UtcNow
                };
                await sessions.AddAsync(session);
                return new OkObjectResult(session);
            }
            catch (Exception ex)
            {
                log.LogError($"Couldn't insert item. Exception thrown: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        [FunctionName("GetAllSessions")]
        public static async Task<IActionResult> GetAllSessions(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "session")] HttpRequest req, 
                [CosmosDB(
                databaseName: "Inferno",
                collectionName: "sessions",
                ConnectionStringSetting = "CosmosDBConnection",
                SqlQuery ="SELECT * FROM c ORDER BY c._ts DESC")] IEnumerable<Session> sessions,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

                if (sessions == null)
                {
                    return new NotFoundResult();
                }
                return new OkObjectResult(sessions);
        }

        [FunctionName("GetSessionById")]
        public static async Task<IActionResult> GetSessionById(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "session/{id}")] HttpRequest req, 
                [CosmosDB(
                databaseName: "Inferno",
                collectionName: "sessions",
                ConnectionStringSetting = "CosmosDBConnection",
                Id = "{id}", PartitionKey = "inferno1-2020-06")] Object session,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            // log.LogInformation("Id" + Id);

            if (session == null)
            {
                return new NotFoundResult();
            }
            return new OkObjectResult(session);

        }
    }
}
