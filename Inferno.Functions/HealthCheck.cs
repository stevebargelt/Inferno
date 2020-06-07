using System;
using System.Collections.Generic;

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Inferno.Functions
{
    public static class HealthCheck
    {
        [FunctionName("healthcheck")]
        public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation("Performing health check on the Cosmos DB processing Function App.");

            // This is a very simple health check that ensures each configuration setting exists and has a value.
            // More thorough checks would validate each value against an expected format or by connecting to each service as required.
            // The function will return an HTTP status of 200 (OK) if all values contain non-zero strings.
            // If any are null or empty, the function will return an error, indicating which values are missing.

            var cosmosDbConnection = Environment.GetEnvironmentVariable("CosmosDBConnection");
            var IoTHubConnection = Environment.GetEnvironmentVariable("IoTHubConnection");
            var connString = Environment.GetEnvironmentVariable("APP_CONFIG_CONN_STRING");

            //TODO: Need to move these to Key Vault
            // RelayNamespace = Configuration["RelayNamespace"];
            // ConnectionName = Configuration["RelayConnectionName"];
            // KeyName = Configuration["RelayKeyName"];
            // Key = Configuration["RelayKey"];

            var variableList = new List<string>();
            if (string.IsNullOrWhiteSpace(cosmosDbConnection)) variableList.Add("CosmosDBConnection");
            if (string.IsNullOrWhiteSpace(IoTHubConnection)) variableList.Add("IoTHubConnection");
            if (string.IsNullOrWhiteSpace(connString)) variableList.Add("APP_CONFIG_CONN_STRING");

            if (variableList.Count > 0)
            {
                return new BadRequestObjectResult($"The service is missing one or more application settings: {string.Join(", ", variableList)}");
            }

            return new OkObjectResult($"The service contains expected application settings");
        }
    }
}
