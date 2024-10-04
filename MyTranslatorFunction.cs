using System.Net;
using System.Net.Http.Json;
using System.Text;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MyFunctionAppForLogging
{
    public class MyFunction
    {
        private readonly ILogger _logger;
        private readonly string key = Environment.GetEnvironmentVariable("TranslatorKey");
        private readonly string endpoint = "https://api.cognitive.microsofttranslator.com";

        // location, also known as region.
        // required if you're using a multi-service or regional (not global) resource. It can be found in the Azure portal on the Keys and Endpoint page.
        private readonly string location = "eastus";
        public MyFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<MyFunction>();
        }

        [Function("MyTranslatorFunction")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("MyTranslatorFunction function processed a request.");

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                dynamic data = JsonConvert.DeserializeObject(requestBody);
                string textToTranslate = data?.Text;
                // Calculate word count
                int wordCount = textToTranslate.Split(' ').Length;

                // Extract custom parameters
                var headers = req.Headers;
                string clientName = GetHeaderValue(headers, "X-Client-Name");
                string environment = GetHeaderValue(headers, "X-Environment");
                string clientTraceId = GetHeaderValue(headers, "X-ClientTraceId");

                string route = "/translate?api-version=3.0&from=en&to=hi&env=staging";

                using (var client = new HttpClient())
                using (var request = new HttpRequestMessage())
                {
                    // Build the request.
                    request.Method = HttpMethod.Post;
                    request.RequestUri = new Uri(endpoint + route);
                    request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                    request.Headers.Add("Ocp-Apim-Subscription-Key", key);
                    request.Headers.Add("Ocp-Apim-Subscription-Region", location);

                    //custom parameter 
                    request.Headers.Add("X-ClientTraceId", clientTraceId);
                    request.Headers.Add("X-Client-Name", clientName);
                    request.Headers.Add("X-Environment", environment);
                    request.Headers.Add("X-Word-Count", wordCount.ToString());

                    _logger.LogInformation("Translate API request body: " + requestBody);
                    // Send the request and get response.
                    HttpResponseMessage apiresponse = await client.SendAsync(request).ConfigureAwait(false);
                    // Read response as a string.
                    string result = await apiresponse.Content.ReadAsStringAsync();
                    Console.WriteLine(result);

                    _logger.LogInformation($"Translate API Response:{result}");

                    // Log custom parameters to Log Analytics
                    _logger.LogInformation($"ClientID: {clientName},ClientTraceId: {clientTraceId}, Environment: {environment}, WordCount: {wordCount}");


                    var response = req.CreateResponse(HttpStatusCode.OK);
                    response.Headers.Add("Content-Type", "application/json; charset=utf-8");

                    await response.WriteStringAsync(result);

                    return response;
                }
            }
            catch (Exception ex)
            {

                var response = req.CreateResponse(HttpStatusCode.BadRequest);

                response.WriteString(ex.Message);

                return response;
            }
        }

        private string GetHeaderValue(HttpHeadersCollection headers, string headerName)
        {
            try
            {
                string value = string.Empty;
                if (headers.TryGetValues(headerName, out var headerValues))
                {
                    if (headerValues.Count() > 0)
                        value = headerValues.FirstOrDefault();
                }

                return value;
            }
            catch (Exception ex)
            {

                throw;
            }
        }
    }
}
