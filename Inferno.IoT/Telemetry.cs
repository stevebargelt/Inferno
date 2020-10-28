using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Inferno.Common.Models;

namespace Inferno.IoT
{
    public class Telemetry
    {
        //s_sleepDuration = retry for errors
        private static readonly TimeSpan s_sleepDuration = TimeSpan.FromSeconds(5);
        //TelemetryInterval is how often to send the telemetry Event Messages
        private static TimeSpan TelemetryInterval = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan s_operationTimeout = TimeSpan.FromHours(1);

         private readonly object _initLock = new object();
        private readonly List<string> _deviceConnectionStrings;
        private readonly string _deviceId;
        private readonly ILogger _logger;
        private DeviceClient _deviceClient;
        private static ConnectionStatus s_connectionStatus;
        private static bool s_wasEverConnected;
         
        private static HttpClient _httpClient = new HttpClient();

        // This was mostly copied from https://github.com/Azure-Samples/azure-iot-samples-csharp/blob/master/iot-hub/Samples/device/DeviceReconnectionSample/DeviceReconnectionSample.cs

        public Telemetry(List<string> deviceConnectionStrings, string deviceId, ILogger logger)
        {
            _logger = logger;
            _deviceId = deviceId;
            //_httpClient= new HttpClient();
            //_deviceClient = deviceClient ?? throw new ArgumentNullException(nameof(deviceClient));
            if (deviceConnectionStrings == null
                || !deviceConnectionStrings.Any())
            {
                throw new ArgumentException("At least one connection string must be provided.", nameof(deviceConnectionStrings));
            }
            _deviceConnectionStrings = deviceConnectionStrings;
            _logger.LogInformation($"Supplied with {_deviceConnectionStrings.Count} connection string(s).");

            InitializeClient();
        }


        private void InitializeClient()
        {
            // If the client reports Connected status, it is already in operational state.
            if (s_connectionStatus != ConnectionStatus.Connected
                && _deviceConnectionStrings.Any())
            {
                lock (_initLock)
                {
                    _logger.LogDebug($"Attempting to initialize the client instance, current status={s_connectionStatus}");

                    // If the device client instance has been previously initialized, then dispose it.
                    // The s_wasEverConnected variable is required to store if the client ever reported Connected status.
                    if (s_wasEverConnected && s_connectionStatus == ConnectionStatus.Disconnected)
                    {
                        _deviceClient?.Dispose();
                        s_wasEverConnected = false;
                    }

                    _deviceClient = DeviceClient.CreateFromConnectionString(_deviceConnectionStrings.First(), TransportType.Mqtt);
                    _deviceClient.SetConnectionStatusChangesHandler(ConnectionStatusChangeHandler);
                    _deviceClient.OperationTimeoutInMilliseconds = (uint)s_operationTimeout.TotalMilliseconds;
                }

                try
                {
                    // Force connection now
                    _deviceClient.OpenAsync().GetAwaiter().GetResult();
                    _logger.LogDebug($"Initialized the client instance.");
                }
                catch (UnauthorizedException)
                {
                    // Handled by the ConnectionStatusChangeHandler
                }
            }
        }

        private void ConnectionStatusChangeHandler(ConnectionStatus status, ConnectionStatusChangeReason reason)
        {
            _logger.LogDebug($"Connection status changed: status={status}, reason={reason}");

            s_connectionStatus = status;
            switch (s_connectionStatus)
            {
                case ConnectionStatus.Connected:
                    _logger.LogDebug("### The DeviceClient is CONNECTED; all operations will be carried out as normal.");

                    s_wasEverConnected = true;
                    break;

                case ConnectionStatus.Disconnected_Retrying:
                    _logger.LogDebug("### The DeviceClient is retrying based on the retry policy. Do NOT close or open the DeviceClient instance");
                    break;

                case ConnectionStatus.Disabled:
                    _logger.LogDebug("### The DeviceClient has been closed gracefully." +
                        "\nIf you want to perform more operations on the device client, you should dispose (DisposeAsync()) and then open (OpenAsync()) the client.");
                    break;

                case ConnectionStatus.Disconnected:
                    switch (reason)
                    {
                        case ConnectionStatusChangeReason.Bad_Credential:
                            // When getting this reason, the current connection string being used is not valid.
                            // If we had a backup, we can try using that.
                            string badCs = _deviceConnectionStrings[0];
                            _deviceConnectionStrings.RemoveAt(0);
                            if (_deviceConnectionStrings.Any())
                            {
                                // Not great to print out a connection string, but this is done for sample/demo purposes.
                                _logger.LogWarning($"The current connection string {badCs} is invalid. Trying another.");
                                InitializeClient();
                                break;
                            }

                            _logger.LogWarning("### The supplied credentials are invalid. Update the parameters and run again.");
                            break;

                        case ConnectionStatusChangeReason.Device_Disabled:
                            _logger.LogWarning("### The device has been deleted or marked as disabled (on your hub instance)." +
                                "\nFix the device status in Azure and then create a new device client instance.");
                            break;

                        case ConnectionStatusChangeReason.Retry_Expired:
                            _logger.LogWarning("### The DeviceClient has been disconnected because the retry policy expired." +
                                "\nIf you want to perform more operations on the device client, you should dispose (DisposeAsync()) and then open (OpenAsync()) the client.");

                            InitializeClient();
                            break;

                        case ConnectionStatusChangeReason.Communication_Error:
                            _logger.LogWarning("### The DeviceClient has been disconnected due to a non-retry-able exception. Inspect the exception for details." +
                                "\nIf you want to perform more operations on the device client, you should dispose (DisposeAsync()) and then open (OpenAsync()) the client.");

                            InitializeClient();
                            break;

                        default:
                            _logger.LogError("### This combination of ConnectionStatus and ConnectionStatusChangeReason is not expected, contact the client library team with logs.");
                            break;

                    }

                    break;

                default:
                    _logger.LogError("### This combination of ConnectionStatus and ConnectionStatusChangeReason is not expected, contact the client library team with logs.");
                    break;
            }
        }

        public async Task RunAsync()
        {

           try
            {
                _deviceClient.SetMethodHandlerAsync("SetTelemetryInterval", SetTelemetryInterval, null).Wait();
                _deviceClient.SetMethodHandlerAsync("SmokerSetSetPoint", SmokerSetSetPoint, null).Wait();
                _deviceClient.SetMethodHandlerAsync("SmokerGetSetPoint", SmokerGetSetPoint, null).Wait();
                _deviceClient.SetMethodHandlerAsync("SmokerGetTemps", SmokerGetTemps, null).Wait();
                _deviceClient.SetMethodHandlerAsync("SmokerGetStatus", SmokerGetStatus, null).Wait();
                await Task.WhenAll(SendEventAsync(), ReceiveMessagesAsync());
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unrecoverable exception caught, user action is required, so exiting...: \n{ex}");
            }
        }

        private async Task SendEventAsync()
        {
            while (true) //TODO: use cancellation context
            {   
                if (s_connectionStatus == ConnectionStatus.Connected)
                {
                    _logger.LogInformation($"Device sending Event/Telemetry to IoT Hub...");
                    SmokerStatus status = JsonConvert.DeserializeObject<SmokerStatus>(await _httpClient.GetStringAsync("http://localhost:5000/api/status"));
                    status.SmokerId = _deviceId;
                    status.PartitionKey = $"{status.SmokerId}-{DateTime.UtcNow:yyyy-MM}";
                    _logger.LogInformation($"{status.PartitionKey},{status.CurrentTime},{status.SmokerId},{status.ttl},{status.SetPoint},{status.Temps.GrillTemp},{status.Temps.Probe1Temp},{status.Temps.Probe2Temp},{status.Temps.Probe3Temp},{status.Temps.Probe4Temp}");
                    string json = JsonConvert.SerializeObject(status);
                    _logger.LogInformation($"Sending {json}");
                    Message eventMessage = new Message(Encoding.UTF8.GetBytes(json));

                    while (true) 
                    {
                        try
                        {
                            await _deviceClient.SendEventAsync(eventMessage).ConfigureAwait(false);
                            _logger.LogInformation($"Sent eventMessage");
                            eventMessage.Dispose();
                            break;
                        }
                        catch (IotHubException ex) when (ex.IsTransient)
                        {
                            // Inspect the exception to figure out if operation should be retried, or if user-input is required.
                            _logger.LogError($"An IotHubException was caught, but will try to recover and retry: {ex}");
                        }
                        catch (Exception ex) when (ExceptionHelper.IsNetworkExceptionChain(ex))
                        {
                            _logger.LogError($"A network related exception was caught, but will try to recover and retry: {ex}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Unexpected error {ex}");
                        }

                        // wait and retry
                        await Task.Delay(s_sleepDuration);
                    }
                    //wait between event message / telemetry message send
                    await Task.Delay(TelemetryInterval);
                }
            }
        }

        private async Task ReceiveMessagesAsync()
        {

            using Message receivedMessage = await _deviceClient.ReceiveAsync(TimeSpan.FromSeconds(30));
            if (receivedMessage == null)
            {
                Console.WriteLine($"\t{DateTime.Now}> Timed out");
                return;
            }

            string messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
            Console.WriteLine($"\t{DateTime.Now}> Received message: {messageData}");

            int propCount = 0;
            foreach (var prop in receivedMessage.Properties)
            {
                Console.WriteLine($"\t\tProperty[{propCount++}> Key={prop.Key} : Value={prop.Value}");
            }

            await _deviceClient.CompleteAsync(receivedMessage);
        }

        // Handle the direct method call
        private Task<MethodResponse> SetTelemetryInterval(MethodRequest methodRequest, object userContext)
        {
            var data = Encoding.UTF8.GetString(methodRequest.Data);

            int newTelemetryInterval;
            // Check the payload is a single integer value
            if (Int32.TryParse(data, out newTelemetryInterval))
            {
                TelemetryInterval = TimeSpan.FromSeconds(newTelemetryInterval);
                 _logger.LogInformation($"Telemetry interval set to {0} seconds", data);
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

        private Task<MethodResponse> SmokerSetSetPoint(MethodRequest methodRequest, object userContext)
        {
            _logger.LogInformation($"SmokerSetSetPoint Called...");
            var data = Encoding.UTF8.GetString(methodRequest.Data);

            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri("http://localhost:5000/api/setpoint"),
                Method = HttpMethod.Post
            };

            int setPoint;
            // Check the payload is a single integer value
            if (Int32.TryParse(data, out setPoint))
            {
                _logger.LogInformation($"Attempting to set Smoker SetPoint to {data} degrees F");
                request.Content = new StringContent(data);
                request.Content.Headers.ContentType.MediaType = "application/json";
                request.Content.Headers.ContentType.CharSet = null;

                var response = _httpClient.SendAsync(request);
                _httpClient.GetStringAsync("http://localhost:5000/api/setpoint");
                
                _logger.LogInformation($"Smoker SetPoint {data} degrees");
                
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
        private Task<MethodResponse> SmokerGetSetPoint(MethodRequest methodRequest, object userContext)
        {
            _logger.LogInformation($"SmokerGetSetPoint Called...");
            var data = Encoding.UTF8.GetString(methodRequest.Data);

            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri("http://localhost:5000/api/setpoint"),
                Method = HttpMethod.Get
            };
            _logger.LogInformation($"Attempting to GET Smoker SetPoint.");
            string url = "http://localhost:5000/api/setpoint";
            string json;
            using (HttpResponseMessage response = _httpClient.GetAsync(url).Result)
            {
                using (HttpContent content = response.Content)
                {
                    json = content.ReadAsStringAsync().Result;
                }
            }
            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(json), 200));
        } 

        private Task<MethodResponse> SmokerGetTemps(MethodRequest methodRequest, object userContext)
        {
            _logger.LogInformation($"SmokerGetTemps Called...");

            string url = "http://localhost:5000/api/temps";
            string json;
            using (HttpResponseMessage response = _httpClient.GetAsync(url).Result)
            {
                using (HttpContent content = response.Content)
                {
                    json = content.ReadAsStringAsync().Result;
                }
            }

            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(json), 200));

        }  

        private Task<MethodResponse> SmokerGetStatus(MethodRequest methodRequest, object userContext)
        {
            _logger.LogInformation($"SmokerGetStatus Called...");

            string url = "http://localhost:5000/api/status";
            string json;
            using (HttpResponseMessage response = _httpClient.GetAsync(url).Result)
            {
                using (HttpContent content = response.Content)
                {
                    json = content.ReadAsStringAsync().Result;
                }
            }

            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(json), 200));

        }         
    }   
    
}