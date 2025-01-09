**Azure Translator V3 Integration with Logging**

This README provides step-by-step instructions to set up and use Azure Translator V3 with custom logging and diagnostics using Azure Functions and Log Analytics.

**1\. Create an Azure Translator V3 Service**

**Set up Azure Translator V3:**

1. Go to the [Azure Portal](https://portal.azure.com/).
2. Search for "Translator" in the search bar and select **Translator** under Azure AI services.
3. Create a new Translator resource.
4. Once the resource is created:
    - Navigate to the resource management page.
    - Note down the **API Key** and **Endpoint** (needed for API calls).

**Configure Translator in Postman or Azure Function:**

- **API URL**: {Text Translator URL}/translate?api-version=3.0&from=en&to=hi&env=staging Example: <https://api.cognitive.microsofttranslator.com/translate?api-version=3.0&from=en&to=hi&env=staging>
- **Headers:**

      Ocp-Apim-Subscription-Key: ************************
      Ocp-Apim-Subscription-Region: eastus
      Content-Type: application/json

- **Optional Custom Headers:**

      X-ClientTraceId: 0f4d2740-378b-46a7-*********
      X-Client-ID: ClientA
      X-Environment: UAT
      X-Word-Count: 50

Note: The Translator API might not natively accept custom headers like X-Client-ID or X-Environment. Custom parameters should be handled in your application code.

**2\. Test the API**

1. Use Postman or another HTTP client to test the API.
2. Ensure the API returns a successful translation response.

**3\. Set Up Azure Function for Logging**

**a. Create an Azure Function App**

1. Go to the [Azure Portal](https://portal.azure.com/).
2. Search for **Function App** in the search bar.
3. Create a new Function App:
    - Function App Name: MyFunctionAppForLogging

**b. Create an HTTP Trigger**

1. Add an HTTP Trigger in the Function App.
2. Implement the following steps in the function:
    - Accept API requests with custom parameters.
    - Log custom parameters (ClientID, Environment, etc.) to Azure Log Analytics.
    - Forward translation requests to the Azure Translator V3 API.
    - Log translation metrics (e.g., word count) to Log Analytics.

Function Code: [MyTranslatorFunction.cs](https://github.com/rajibmahata/MyFunctionApp-For-Translate-API-Logging-in-Log-Analytics/blob/master/MyTranslatorFunction.cs)

**c. Configure Diagnostic Logs**

1. Navigate to the Function App → **Diagnostic Settings** → Add Diagnostic Setting.
2. Select **AppServiceHTTPLogs** and **FunctionAppLogs**.
3. Send logs to Log Analytics.
4. Verify the Log Analytics Workspace is correctly linked.

**4\. Set Up Log Analytics Workspace**

1. Navigate to **Monitor** → **Log Analytics** → **Workspaces** → Add Workspace.
2. Create a new Log Analytics Workspace:
    - Workspace Name: CustomTranslatorLogAnalytics
3. Link the Log Analytics Workspace in the Translator resource diagnostic settings.

**5\. Test the Function and Generate Logs**

**Example Headers for API Requests**

1. Custom Parameters:
  X-ClientTraceId: 0f4d2740-378b-46a7-8c38-*********
  X-Client-ID: ClientA
  X-Environment: UAT
2. Dynamic Parameters:
  X-ClientTraceId: {{$guid}}
  X-Client-ID: ClientB
  X-Environment: UAT
3. Another Example:
  X-ClientTraceId: {{$guid}}
  X-Client-ID: ClientC
  X-Environment: UAT

**Function Behavior**

- Logs custom parameters (ClientID, Environment, X-ClientTraceId, X-Word-Count) to Azure Log Analytics.
- Forwards the request to the Translator API.
- Logs translation metrics and responses.

**6\. Query Logs in Log Analytics**

**Query Custom Parameters**

Use KQL to extract custom parameters:

    AppTraces
    | where Message contains "ClientID"
    | extend ClientID = extract(@"ClientID:\\s\*(\[A-Za-z0-9\]+)", 1, Message)
    | extend ClientTraceId = extract(@"ClientTraceId:\\s\*(\[A-Za-z0-9-\]+)", 1, Message)
    | extend Environment = extract(@"Environment:\\s\*(\[A-Za-z\]+)", 1, Message)
    | extend WordCount = toint(extract(@"WordCount:\\s\*(\\d+)", 1, Message))
    | project Message, ClientID, Environment, WordCount, ClientTraceId

**Query Translation Metrics by Client**

Summarize word count by ClientID and Environment:

    AppTraces
    | where Message contains "ClientID"
    | extend ClientID = extract(@"ClientID:\\s\*(\[A-Za-z0-9\]+)", 1, Message)
    | extend ClientTraceId = extract(@"ClientTraceId:\\s\*(\[A-Za-z0-9-\]+)", 1, Message)
    | extend Environment = extract(@"Environment:\\s\*(\[A-Za-z\]+)", 1, Message)
    | extend WordCount = toint(extract(@"WordCount:\\s\*(\\d+)", 1, Message))
    | project Message, ClientID, Environment, WordCount, ClientTraceId
    | summarize TotalWordsTranslated = sum(WordCount) by ClientID, Environment

**Additional Notes**

- Ensure all required diagnostic settings are enabled.
- Use Log Analytics to monitor and troubleshoot requests.

**License**

This project is licensed under the MIT License. See the LICENSE file for details.
