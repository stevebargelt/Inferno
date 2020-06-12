using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Documents.Client;
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
        public static IActionResult GetAllSessions(
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
            // log.LogInformation("Id" + d);

            if (session == null)
            {
                log.LogInformation($"Session not found");
                return new NotFoundResult();
            }
            return new OkObjectResult(session);

        }

        [FunctionName("UpdateSession")]
        public static async Task<IActionResult> UpdateSession(

            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "session/{id}")] HttpRequest req, 
                [CosmosDB(
                databaseName: "Inferno",
                collectionName: "sessions",
                ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client,
                ILogger log,
                string id)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var updatedSession = JsonConvert.DeserializeObject<Session>(requestBody);

            Uri sessionCollectionUri = UriFactory.CreateDocumentCollectionUri("Inferno", "sessions");

            var document = client.CreateDocumentQuery(sessionCollectionUri, 
                            new FeedOptions() { PartitionKey = new Microsoft.Azure.Documents.PartitionKey("inferno1-2020-06")})
                .Where(t => t.Id == id)
                .AsEnumerable()
                .FirstOrDefault();

            if (document == null)
            {
                log.LogError($"Session {id} not found. It may not exist!");
                return new NotFoundResult();
            }

            if (!string.IsNullOrEmpty(updatedSession.Description))
            {
                document.SetPropertyValue("Description", updatedSession.Description);
            }
            if (!string.IsNullOrEmpty(updatedSession.Title))
            {
                document.SetPropertyValue("Title", updatedSession.Title);
            }
            if (updatedSession.EndTime.HasValue)
            {
                document.SetPropertyValue("EndTime", updatedSession.EndTime);
            }

            await client.ReplaceDocumentAsync(document);

            Session updatedSessionDocument = (dynamic)document;

            return new OkObjectResult(updatedSessionDocument);
    
        }
    }
}
