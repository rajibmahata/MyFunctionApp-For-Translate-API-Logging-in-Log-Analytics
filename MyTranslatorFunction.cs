using System.Net;
using System.Net.Http.Json;
using System.Text;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Data;
using System.Diagnostics.Metrics;

namespace MyFunctionAppForLogging
{
    public class MyFunction
    {
        private readonly ILogger _logger;
        private readonly MyConfiguration options;
        private readonly string key;
        private readonly string endpoint = "https://api.cognitive.microsofttranslator.com";

        // location, also known as region.
        // required if you're using a multi-service or regional (not global) resource. It can be found in the Azure portal on the Keys and Endpoint page.
        private readonly string location = "eastus";
        public MyFunction(ILoggerFactory loggerFactory, IOptions<MyConfiguration> _options)
        {
            options = _options.Value;
            _logger = loggerFactory.CreateLogger<MyFunction>();
            key = options.TranslatorKey;
        }

        [Function("MyTranslatorFunction")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("MyTranslatorFunction function processed a request.");

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                // Calculate word count
                List<MyTranslatorFunctionRequestModel> data = JsonConvert.DeserializeObject<List<MyTranslatorFunctionRequestModel>>(requestBody);
                string textToTranslate = data.FirstOrDefault()?.Text;
                int wordCount = textToTranslate.Split(' ').Length;

                // Extract custom parameters
                var requestHeaders = req.Headers;
                // string clientID = GetHeaderValue(headers, "X-Client-ID");
                string environment = GetHeaderValue(requestHeaders, "X-Environment")?.FirstOrDefault();
                string clientTraceId = GetHeaderValue(requestHeaders, "X-ClientTraceId")?.FirstOrDefault();
                string from = GetHeaderValue(requestHeaders, "from")?.FirstOrDefault();
                IEnumerable<string> tos = GetHeaderValue(requestHeaders, "to");
                string env = GetHeaderValue(requestHeaders, "env")?.FirstOrDefault();

                //binding route url 
                string route = $"/translate?api-version=3.0";

                if (!string.IsNullOrEmpty(from))
                    route += $"&from={from}";

                if (tos.Count() > 0)
                {
                    foreach (var to in tos)
                    {
                        route += $"&to={to}";
                    }
                }

                if (!string.IsNullOrEmpty(env))
                {
                    route += $"&env={env}";
                }

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
                    //request.Headers.Add("X-ClientTraceId", clientTraceId);
                    //request.Headers.Add("X-Client-ID", clientID);
                    //request.Headers.Add("X-Environment", environment);
                    //request.Headers.Add("X-Word-Count", wordCount.ToString());

                    _logger.LogInformation("Translate API request body: " + requestBody);
                    // Send the request and get response.
                    HttpResponseMessage apiresponse = await client.SendAsync(request).ConfigureAwait(false);
                    // Read response as a string.
                    string result = await apiresponse.Content.ReadAsStringAsync();
                    Console.WriteLine(result);

                    // Extract response headers parameters
                    var responseHeaders = apiresponse.Headers;
                    string meteredUsage = GetHeaderValue(requestHeaders, "X-Metered-Usage")?.FirstOrDefault();
                   

                    _logger.LogInformation($"Translate API Response:{result}");

                    // Log custom parameters to Log Analytics
                    _logger.LogInformation($"ClientTraceId: {clientTraceId}, Environment: {environment}, MeteredUsage: {meteredUsage}");


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

        private IEnumerable<string> GetHeaderValue(HttpHeadersCollection headers, string headerName)
        {
            try
            {
                IEnumerable<string> value = null;
                if (headers.TryGetValues(headerName, out var headerValues))
                {
                    if (headerValues.Count() > 0)
                        value = headerValues;
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
