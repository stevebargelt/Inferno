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
            var Telemetry = new Telemetry(DeviceConnectionStrings, DeviceId, logger);
            await Telemetry.RunAsync();
            
            logger.LogInformation("Done.");
            return 0;
        }

    }
}
