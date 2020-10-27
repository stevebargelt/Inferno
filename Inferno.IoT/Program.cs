using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Devices.Logging;

using Newtonsoft.Json;

using Inferno.Common.Models;

namespace Inferno.IoT
{
    public class Program
    {

        //only using one connection string right now... but this allows multiple - probably primary and secondary 
        // in case you have to disable or revoke one or the other.
        private static List<string> DeviceConnectionStrings = new List<string>(2)
        {
            "HostName=inferno.azure-devices.net;DeviceId=inferno1;SharedAccessKey=0zAFYmxyKgckxNjwBD92S4sSlv1K1A0ibcgwofbP+GI="
        };

        // Replace with the device id you used when you created the device in Azure IoT Hub
        const string DeviceId = "inferno1";
        //static DeviceClient _deviceClient = DeviceClient.CreateFromConnectionString(DeviceConnectionString, TransportType.Mqtt);
        // static int _msgId = 0;

        static async Task<int> Main(string[] args)
        {
            // Set up logging
            ILoggerFactory loggerFactory = new LoggerFactory();
            loggerFactory.AddColorConsoleLogger(
                new ColorConsoleLoggerConfiguration
                {
                    MinLogLevel = LogLevel.Debug,
                });
            var logger = loggerFactory.CreateLogger<Program>();

            const string SdkEventProviderPrefix = "Microsoft-Azure-";
            // Instantiating this seems to do all we need for outputting SDK events to our console log
            _ = new ConsoleEventListener(SdkEventProviderPrefix, logger);

            // _deviceClient.SetMethodHandlerAsync("SetTelemetryInterval", SetTelemetryInterval, null).Wait();
            // _deviceClient.SetMethodHandlerAsync("SmokerSetPoint", SmokerSetPoint, null).Wait();
            // SendDeviceToCloudMessagesAsync();
            var Telemetry = new Telemetry(DeviceConnectionStrings, DeviceId, logger);
            await Telemetry.RunAsync();
            
            logger.LogInformation("Done.");
            return 0;
        }

        // // Handle the direct method call
        // private static Task<MethodResponse> SetTelemetryInterval(MethodRequest methodRequest, object userContext)
        // {
        //     var data = Encoding.UTF8.GetString(methodRequest.Data);

        //     // Check the payload is a single integer value
        //     if (Int32.TryParse(data, out TelemetryInterval))
        //     {
        //         Console.ForegroundColor = ConsoleColor.Green;
        //         Console.WriteLine("Telemetry interval set to {0} seconds", data);
        //         Console.ResetColor();

        //         // Acknowlege the direct method call with a 200 success message
        //         string result = "{\"result\":\"Executed direct method: " + methodRequest.Name + "\"}";
        //         return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
        //     }
        //     else
        //     {
        //         // Acknowlege the direct method call with a 400 error message
        //         string result = "{\"result\":\"Invalid parameter\"}";
        //         return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 400));
        //     }
        // }

        private static Task<MethodResponse> SmokerSetPoint(MethodRequest methodRequest, object userContext)
        {
            var data = Encoding.UTF8.GetString(methodRequest.Data);

            HttpClient client = HttpClientFactory.Create();
            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri("http://localhost:5000/api/setpoint"),
                Method = HttpMethod.Post
            };

            int setPoint;
            // Check the payload is a single integer value
            if (Int32.TryParse(data, out setPoint))
            {
                request.Content = new StringContent(setPoint.ToString());
                request.Content.Headers.ContentType.MediaType = "application/json";
                request.Content.Headers.ContentType.CharSet = null;

                var response = client.SendAsync(request);
                // await _client.GetStringAsync("http://localhost:5000/api/setpoint");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Smoker SetPoint {0} degrees", data);
                Console.ResetColor();
                
                // Acknowlege the direct method call with a 200 success message
                string result = "{\"result\":\"Executed direct method: " + methodRequest.Name + "\"}";
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
            }
            else
            {
                // Acknowlege the direct method call with a 400 error message
                string result = "{\"result\":\"Invalid parameter\"}";
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 400));
            }
        }
        // // Async method to send telemetry
        // private static async void SendDeviceToCloudMessagesAsync()
        // {
        //     Console.WriteLine("partitionKey,Timestamp,SmokerId,ttl,Setpoint,Grill,Probe1,Probe2,Probe3,Probe4");
        //     HttpClient _client = new HttpClient();

        //     while (true)
        //     {   
        //         SmokerStatus status = JsonConvert.DeserializeObject<SmokerStatus>(await _client.GetStringAsync("http://localhost:5000/api/status"));
        //         status.SmokerId = DeviceId;
        //         status.PartitionKey = $"{status.SmokerId}-{DateTime.UtcNow:yyyy-MM}";
        //         Console.WriteLine($"{status.PartitionKey},{status.CurrentTime},{status.SmokerId},{status.ttl},{status.SetPoint},{status.Temps.GrillTemp},{status.Temps.Probe1Temp},{status.Temps.Probe2Temp},{status.Temps.Probe3Temp},{status.Temps.Probe4Temp}");
        //         string json = JsonConvert.SerializeObject(status);
        //         Console.WriteLine($"Sending {json}");
        //         Message eventMessage = new Message(Encoding.UTF8.GetBytes(json));
        //         await _deviceClient.SendEventAsync(eventMessage).ConfigureAwait(false);                
        //         //await SendMsgIotHub(status);
        //         await Task.Delay(TimeSpan.FromSeconds(TelemetryInterval));
        //     }
        // }

        // private static async Task SendMsgIotHub(SmokerStatus status)
        // {
        //     //var telemetry = new Telemetry() { status, MessageId = _msgId++ };
        //     string json = JsonConvert.SerializeObject(status);

        //     Console.WriteLine($"Sending {json}");

        //     Message eventMessage = new Message(Encoding.UTF8.GetBytes(json));
        //     // eventMessage.Properties.Add("temperatureAlert", (temperature > TemperatureThreshold) ? "true" : "false");
        //     await _deviceClient.SendEventAsync(eventMessage).ConfigureAwait(false);
        // }

    }
}
