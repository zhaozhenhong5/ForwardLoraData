using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.ServiceBus;

namespace ForwardLoraData
{
    public static class ForwardLoraData
    {
        public static async Task RunAsync([EventHubTrigger("%HubName%", Connection =
      "RouterConnection", ConsumerGroup = "%ConsumerGroupForwardLoraData%")]EventData
        eventData, ILogger log, ExecutionContext context)
        {
            var configuration = BuildConfiguration(context);
            var bodyBytes = eventData.Body.Array;
            var deviceId = GetDeviceId(eventData);
            var payload = GetPayload(bodyBytes);
            var requestUri = configuration[ConfigurationKeys.WebApiUrl];
            var httpClient = CreateHttpClient();

            var measurement = new LoraMeasurement
            {
                DeviceId = deviceId,
                Json = payload,
            };
            var message = await httpClient.PostAsJsonAsync(requestUri, measurement);

            log.LogInformation(message.IsSuccessStatusCode
               ? $"ForwardLoraData: request sent successfully"
               : $"ForwardLoraData: request not sent successfully - {message.ReasonPhrase}");
        }

        private static HttpClient CreateHttpClient()
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return httpClient;
        }
        private static string GetPayload(byte[] body)
        {
            var json = Encoding.UTF8.GetString(body);
            return json;
        }

        private static string GetDeviceId(EventData message)
        {
            return message.SystemProperties["iothub-connection-device-id"].ToString();
        }

        priate static IConfigurationRoot BuildConfiguration(ExecutionContext context)
        {
            return new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
        }
    }
}
