using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Relay;
using System.Net.Http;
using Inferno.Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Honeycomb;

namespace Inferno.Functions
{

    public static class GetTemps
    {
        private static IConfiguration Configuration { set; get; }
        private static string RelayNamespace;
        private static string ConnectionName;
        private static string KeyName;
        private static string Key;
        private static string HoneycombKey;
        private static string HoneycombDataset;        
        private static LibHoney _libHoney;

        static GetTemps()
        {
            var builder = new ConfigurationBuilder();
            var connString = Environment.GetEnvironmentVariable("APP_CONFIG_CONN_STRING", EnvironmentVariableTarget.Process);
            builder.AddAzureAppConfiguration(connString);
            Configuration = builder.Build();
            HoneycombKey = Configuration["HoneycombKey"];
            HoneycombDataset = Configuration["HoneycombDataset"];
            _libHoney = new LibHoney(HoneycombKey, HoneycombDataset);

        }

        [FunctionName("temps")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "temps/{traceid?}/{parentspanid?}")] HttpRequest req,
            ILogger log, string? traceid, string? parentspanid)
        {
            
            log.LogInformation("C# HTTP trigger function processed a request.");
            var spanId = Guid.NewGuid().ToString();
            RelayNamespace = Configuration["RelayNamespace"];
            ConnectionName = Configuration["RelayConnectionName"];
            KeyName = Configuration["RelayKeyName"];
            Key = Configuration["RelayKey"];
            var baseUri = new Uri(string.Format("https://{0}/{1}/", RelayNamespace, ConnectionName));

            HttpResponseMessage response;
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            response = await SendRelayRequest(baseUri, "temps", HttpMethod.Get, "");
            stopWatch.Stop();
            
            _libHoney.SendNow (new Dictionary<string, object> () {
                ["name"] = "temps",
                ["service_name"] = "GetTemps",
                ["trace_id"] = traceid,
                ["trace.span_id"] = spanId,
                ["trace.parent_id"] = parentspanid,
                ["duration_ms"] = stopWatch.ElapsedMilliseconds,
                ["method"] = "get",
                ["status_code"] = response.StatusCode,
                ["azFunction"] = "GetTemps",
                ["endpoint"] = baseUri + "temps",
            });

            if (response.IsSuccessStatusCode)
            {
                log.LogInformation("response.IsSuccessStatusCode PASSED");
                string payload = await response.Content.ReadAsStringAsync();
                return new OkObjectResult(payload);
            }
            else
            {
                log.LogInformation("response.IsSuccessStatusCode FAILED");
                return new BadRequestObjectResult(response.ReasonPhrase);
            }

        }

        // ************************************************************
        // The following should be broken out into a seperate class/obj
        // ************************************************************
        private static async Task<HttpResponseMessage> SendRelayRequest(Uri baseUri, string apiEndpoint, HttpMethod method, string payload = "")
        {
            HttpClient client = HttpClientFactory.Create();
            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri(baseUri, apiEndpoint),
                Method = method
            };

            if (method == HttpMethod.Post)
            {
                request.Content = new StringContent(payload);
                request.Content.Headers.ContentType.MediaType = "application/json";
                request.Content.Headers.ContentType.CharSet = null;
            }

            await AddAuthToken(request);

            var response = await client.SendAsync(request);

            return response;
        }

        private static async Task AddAuthToken(HttpRequestMessage request)
        {
            TokenProvider tokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(KeyName, Key);
            string token = (await tokenProvider.GetTokenAsync(request.RequestUri.AbsoluteUri, TimeSpan.FromHours(1))).TokenString;

            request.Headers.Add("ServiceBusAuthorization", token);
        }

    }
}


