using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Relay;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using Inferno.Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;

namespace Infero.Function
{
    public static class GetStatus
    {
        private static IConfiguration Configuration { set; get; }
        private static string RelayNamespace;
        private static string ConnectionName;
        private static string KeyName;
        private static string Key;

        static GetStatus()
        {
            var builder = new ConfigurationBuilder();
            var connString = Environment.GetEnvironmentVariable("APP_CONFIG_CONN_STRING", EnvironmentVariableTarget.Process);
            builder.AddAzureAppConfiguration(connString);
            Configuration = builder.Build();
        }

        [FunctionName("GetStatus")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            RelayNamespace = Configuration["RelayNamespace"];
            ConnectionName = Configuration["RelayConnectionName"];
            KeyName = Configuration["RelayKeyName"];
            Key = Configuration["RelayKey"]; ;

            // Begin
            HttpClient client = HttpClientFactory.Create();
            var baseUri = new Uri(string.Format("https://{0}/{1}/", RelayNamespace, ConnectionName));
            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri(baseUri, "status"),
                Method = HttpMethod.Get
            };

            await AddAuthToken(request);

            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                string payload = await response.Content.ReadAsStringAsync();
                // I know deserializing and serializing is redundant. I think I might want to do
                // something with the object soon. Gold plating? Yes, indeed. 
                // SmokerStatus ss = JsonConvert.DeserializeObject<SmokerStatus>(payload);
                // return new OkObjectResult(JsonConvert.SerializeObject(ss));
                return new OkObjectResult(payload);
            }
            else
            {
                return new BadRequestObjectResult(response.ReasonPhrase);
            }

        }

        // private async Task<HttpResponseMessage> SendRelayRequest(string apiEndpoint, HttpMethod method, string payload = "")
        // {
        //     var request = new HttpRequestMessage()
        //     {
        //         RequestUri = new Uri(_baseUri, apiEndpoint),
        //         Method = method
        //     };

        //     if (method == HttpMethod.Post)
        //     {
        //         request.Content = new StringContent(payload);
        //         request.Content.Headers.ContentType.MediaType = "application/json";
        //         request.Content.Headers.ContentType.CharSet = null;
        //     }

        //     await AddAuthToken(request);

        //     var response = await _client.SendAsync(request);

        //     return response;
        // }

        private static async Task AddAuthToken(HttpRequestMessage request)
        {
            TokenProvider tokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(KeyName, Key);
            string token = (await tokenProvider.GetTokenAsync(request.RequestUri.AbsoluteUri, TimeSpan.FromHours(1))).TokenString;

            request.Headers.Add("ServiceBusAuthorization", token);
        }


    }
}
