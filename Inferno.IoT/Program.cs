using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using Inferno.Common.Models;

namespace Inferno.IoT
{
    class Program
    {

        const string DeviceConnectionString = "HostName=inferno.azure-devices.net;DeviceId=inferno1;SharedAccessKey=0zAFYmxyKgckxNjwBD92S4sSlv1K1A0ibcgwofbP+GI=";

        // Replace with the device id you used when you created the device in Azure IoT Hub
        const string DeviceId = "inferno1";
        static DeviceClient _deviceClient = DeviceClient.CreateFromConnectionString(DeviceConnectionString, TransportType.Mqtt);
        // static int _msgId = 0;

        static async Task Main(string[] args)
        {
            Console.WriteLine("partitionKey,Timestamp,SmokerId,Setpoint,Grill,Probe1,Probe2,Probe3,Probe4");
            HttpClient _client = new HttpClient();

            while (true)
            {
                SmokerStatus status = JsonConvert.DeserializeObject<SmokerStatus>(await _client.GetStringAsync("http://localhost:5000/api/status"));
                status.SmokerId = DeviceId;
                status.PartitionKey = $"{status.SmokerId}-{DateTime.UtcNow:yyyy-MM}";
                Console.WriteLine($"{status.PartitionKey},{status.CurrentTime},{status.SmokerId},{status.SetPoint},{status.Temps.GrillTemp},{status.Temps.Probe1Temp},{status.Temps.Probe2Temp},{status.Temps.Probe3Temp},{status.Temps.Probe4Temp}");
                await SendMsgIotHub(status);
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }

        private static async Task SendMsgIotHub(SmokerStatus status)
        {
            //var telemetry = new Telemetry() { status, MessageId = _msgId++ };
            string json = JsonConvert.SerializeObject(status);

            Console.WriteLine($"Sending {json}");

            Message eventMessage = new Message(Encoding.UTF8.GetBytes(json));
            // eventMessage.Properties.Add("temperatureAlert", (temperature > TemperatureThreshold) ? "true" : "false");
            await _deviceClient.SendEventAsync(eventMessage).ConfigureAwait(false);
        }

    }
}
