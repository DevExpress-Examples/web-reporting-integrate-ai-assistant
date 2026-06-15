<!-- default badges list -->
![](https://img.shields.io/endpoint?url=https://codecentral.devexpress.com/api/v1/VersionRange/853003889/26.1.2%2B)
[![](https://img.shields.io/badge/Open_in_DevExpress_Support_Center-FF7200?style=flat-square&logo=DevExpress&logoColor=white)](https://supportcenter.devexpress.com/ticket/details/T1252182)
[![](https://img.shields.io/badge/📖_How_to_use_DevExpress_Examples-e9f6fc?style=flat-square)](https://docs.devexpress.com/GeneralInformation/403183)
[![](https://img.shields.io/badge/💬_Leave_Feedback-feecdd?style=flat-square)](#does-this-example-address-your-development-requirementsobjectives)
<!-- default badges end -->
# Reporting for ASP.NET Core - Integrate AI Assistant based on Azure OpenAI

This example is an ASP.NET Core application with integrated DevExpress Reports and an AI assistant. User requests and assistant responses are displayed on-screen using the DevExtreme [`dxChat`](https://js.devexpress.com/jQuery/Documentation/24_2/ApiReference/UI_Components/dxChat/) component.

The AI assistant's role depends on the associated DevExpress Reports component:

- **Data Analysis Assistant**: An assistant for the DevExpress *Web Document Viewer*. This assistant analyzes report content and answers questions related to information within the report.
- **UI Assistant**: An assistant for the DevExpress *Web Report Designer*. This assistant explains how to use the Designer UI to accomplish various tasks. Responses are based on information from [end-user documentation](https://github.com/DevExpress/dotnet-eud) for DevExpress Web Reporting components.

**Note: AI Assistant initialization takes time. The assistant tab becomes available once Azure OpenAI finishes uploading and indexing the source document on the server.**

To answer questions, the application uploads ODF documents to Azure OpenAI, and creates a chat agent using the [Azure OpenAI Responses API](https://learn.microsoft.com/en-us/azure/foundry/openai/how-to/responses?tabs=csharp). The agent uses File Search and Code Interpreter tools to read and analyze the document.

## Implementation Details

### Common Settings

#### Add Personal Keys

> [!NOTE]  
> DevExpress AI-powered extensions follow the "bring your own key" principle. DevExpress does not offer a REST API and does not ship any built-in LLMs/SLMs. You need an active Azure/Open AI subscription to obtain the REST API endpoint, key, and model deployment name. These variables must be specified at application startup to register AI clients and enable DevExpress AI-powered Extensions in your application.

Create an Azure OpenAI resource in the Azure portal. Refer to the following help topic for additional information: [Microsoft - Create and deploy an Azure OpenAI Service resource](https://learn.microsoft.com/en-us/azure/ai-services/openai/how-to/create-resource?pivots=web-portal).

Once you obtain a private endpoint and an API key, register them as `AZURE_OPENAI_ENDPOINT` and `AZURE_OPENAI_APIKEY` environment variables. The [EnvSettings.cs](./CS/ReportingApp/EnvSettings.cs) file reads these settings. `DeploymentName` is the name of your Azure model deployment. The model must support Responses API, File Search, and Code Interpreter tools (for example, `gpt-5.4`):

```cs
public static class EnvSettings {
    public static string AzureOpenAIEndpoint { get { return Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT"); } }
    public static string AzureOpenAIKey { get { return Environment.GetEnvironmentVariable("AZURE_OPENAI_APIKEY"); } }
    public static string DeploymentName { get { return "gpt-5.4"; } }
}
```

Files to Review: 
- [EnvSettings.cs](./CS/ReportingApp/EnvSettings.cs)

#### Register AI Services

Register AI services in your application. Add the following code to the _Program.cs_ file:

```cs
using Azure;
using Azure.AI.OpenAI;
using ReportingApp.Services;
using Microsoft.Extensions.Logging;
// ...
var azureOpenAIClient = new AzureOpenAIClient(
    new Uri(EnvSettings.AzureOpenAIEndpoint),
    new AzureKeyCredential(EnvSettings.AzureOpenAIKey));

// Create a Responses API agent (with File Search and Code Interpreter tools) for each chat.
builder.Services.AddSingleton<AgentFactory>(sp =>
    new(azureOpenAIClient, EnvSettings.DeploymentName, sp.GetRequiredService<ILogger<AgentFactory>>()));
builder.Services.AddSingleton<IAIReportingChatService, AIReportingChatService>();
// ...
```

Files to Review: 
- [Program.cs](./CS/ReportingApp/Program.cs)

#### AI Assistant Provider

On the server side, the `AIReportingChatService` service manages chat sessions:

```cs
public interface IAIReportingChatService {
    IChatResponseProvider GetChatProvider(string sessionId);
    Task<string> OpenDocumentChatAsync(Stream data);
    Task<string> OpenDesignerChatAsync();
    Task CloseChatAsync(string sessionId);
}
```

The `AgentFactory` class creates an agent that answers questions. When a chat opens, `AgentFactory.CreateAgentWithFileAsync` does the following:

1. Uploads the source PDF to Azure OpenAI and adds it to a short-lived vector store.
2. Creates a Responses API agent with File Search and Code Interpreter tools. The agent reads the document content and runs calculations to answer data-driven questions.
3. Starts a session that preserves the conversation history.
4. Returns an `IChatResponseProvider`.

`AIReportingChatService` stores each provider by session id, and deletes the uploaded file and vector store when the chat is closed.

For information on the OpenAI Responses API, refer to the following documents: 
- [Azure OpenAI Responses API](https://learn.microsoft.com/en-us/azure/foundry/openai/how-to/responses?tabs=csharp)
- [OpenAI Responses API](https://developers.openai.com/api/reference/responses/overview)
- [File Search tool](https://developers.openai.com/api/docs/guides/tools-file-search)
- [Code Interpreter tool](https://developers.openai.com/api/docs/guides/tools-code-interpreter)

You can review and tailor agent instructions in the following file: [AgentInstructions.cs](./CS/ReportingApp/Services/AgentInstructions.cs).

Files to Review: 
- [IAIReportingChatService.cs](./CS/ReportingApp/Services/IAIReportingChatService.cs)
- [AIReportingChatService.cs](./CS/ReportingApp/Services/AIReportingChatService.cs)
- [AgentFactory.cs](./CS/ReportingApp/Services/AgentFactory.cs)
- [AgentInstructions.cs](./CS/ReportingApp/Services/AgentInstructions.cs)


### Web Document Viewer (Data Analysis Assistant)

The following image displays Web Document Viewer UI implemented in this example. The AI Assistant tab uses a `dxChat` component to display requests and responses:

![Web Document Viewer](images/web-document-viewer.png)

#### Add a New Tab

On the `BeforeRender` event, add a new tab (a container for the assistant interface):

```cshtml
@model DevExpress.XtraReports.Web.WebDocumentViewer.WebDocumentViewerModel
@await Html.PartialAsync("_AILayout")
<script>
    let aiTab;
    async function BeforeRender(sender, args) {
        const previewModel = args;
        const reportPreview = previewModel.reportPreview;

        aiTab = createAssistantTab();
        const model = aiTab.model;
        previewModel.tabPanel.tabs.push(aiTab);
        // ...
    }
</script>

@{
    var viewerRender = Html.DevExpress().WebDocumentViewer("DocumentViewer")
        .Height("100%")
        .ClientSideEvents(configure => {
            configure.BeforeRender("BeforeRender");
        })
        .Bind(Model);
    @viewerRender.RenderHtml()
}
@* ... *@
```

#### Access the Assistant

Once the document is ready, the `DocumentReady` event handler sends a request to the server and obtains the chat (session) id:

```js
async function DocumentReady(sender, args) {
    const response = await sender.PerformCustomDocumentOperation(null, true);
    if (response.customData && aiTab?.model) {
        aiTab.model.chatId = response.customData;
        aiTab.visible = true;
    }
}
```

The [`PerformCustomDocumentOperation`](https://docs.devexpress.com/XtraReports/js-ASPxClientWebDocumentViewer?p=netframework#js_aspxclientwebdocumentviewer_performcustomdocumentoperation) method exports the report to PDF and opens a chat that uses the exported document to answer user questions: 

```cs
// ...
public override async Task<DocumentOperationResponse> PerformOperationAsync(DocumentOperationRequest request, PrintingSystemBase printingSystem, PrintingSystemBase printingSystemWithEditingFields) {
    using(var stream = new MemoryStream()) {
        printingSystem.ExportToPdf(stream, printingSystem.ExportOptions.Pdf);
        var chatId = await chatService.OpenDocumentChatAsync(stream);
        return new DocumentOperationResponse {
            DocumentId = request.DocumentId,
            CustomData = chatId,
            Succeeded = true
        };
    }
}
```

See the following files for implementation details:

- [AIDocumentOperationService.cs](./CS/ReportingApp/Services/AIDocumentOperationService.cs)
- [AIReportingChatService.cs](./CS/ReportingApp/Services/AIReportingChatService.cs)

#### Communicate with the Assistant

Each time a user sends a message, the [`onMessageEntered`](https://js.devexpress.com/jQuery/Documentation/24_2/ApiReference/UI_Components/dxChat/Configuration/#onMessageEntered) event handler passes the request to the assistant:

```js
//...
async function getAIResponse(instance, text, id) {
    const formData = new FormData();
    formData.append('text', text);
    formData.append('chatId', id);
    lastUserQuery = text;
    return _tryFetch(instance, async () => {
        const response = await fetch('/AI/GetAnswer', {
            method: 'POST',
            body: formData
        });

        if (!response.ok) {
            _handleError(instance, { code: `${response.status}`, message: `Internal server error` });
            return;
        }
        return await response.text();
    }, 'GetAnswer');
}
// ...
function RenderAssistantMessage(instance, message) {
    instance.option({ typingUsers: [] });
    instance.renderMessage({ timestamp: new Date(), text: message, author: assistant.name, id: assistant.id });
}
// ...
onMessageEntered: async (e) => {
    lastRefreshButton?.remove();
    const instance = e.component;
    instance.option('alerts', []);
    instance.renderMessage(e.message);
    instance.option({ typingUsers: [assistant] });
    const userInput = e.message.text;
    if (!assistant.id && model.chatId) {
        assistant.id = model.chatId;
    }
    const response = await getAIResponse(instance, userInput, assistant.id);
    RenderAssistantMessage(instance, response);
}
// ...
```

`AIController.GetAnswer` receives answers from the assistant.

#### Files to Review:

- [DocumentViewer.cshtml](./CS/ReportingApp/Views/Home/DocumentViewer.cshtml)
- [AIDocumentOperationService.cs](CS/ReportingApp/Services/AIDocumentOperationService.cs)
- [AIController.cs](./CS/ReportingApp/Controllers/AIController.cs)
- [aiIntegration.js](./CS/ReportingApp/wwwroot/js/aiIntegration.js)

### Web Report Designer (UI Assistant)

The following image displays Web Report Designer UI implemented in this example. The AI Assistant tab uses a `dxChat` component to display requests and responses:

![Web Report Designer](images/web-report-designer.png)

#### Add a New Tab

On the `BeforeRender` event, add a new tab (a container for the assistant interface):

```cshtml
@model DevExpress.XtraReports.Web.ReportDesigner.ReportDesignerModel
<script>
    async function BeforeRender(sender, args) {
        const result = await fetch(`/AI/CreateUserAssistant`);
        const chatId = await result.text();
        const tab = createAssistantTab(chatId);
        args.tabPanel.tabs.push(tab);
    }
</script>

@await Html.PartialAsync("_AILayout")
@{
    var designerRender = Html.DevExpress().ReportDesigner("reportDesigner")
        .Height("100%")
        .ClientSideEvents(configure => {
            configure.BeforeRender("BeforeRender");
        })
        .Bind(Model);
    @designerRender.RenderHtml()
}

@section Scripts {
    @* ... *@
    <script src="~/js/aiIntegration.js"></script>
    @designerRender.RenderScripts()
}
@* ... *@
```

#### Access the Assistant

On the `BeforeRender` event, send a request to `AIController` to create the assistant:

```js
async function BeforeRender(sender, args) {
    const result = await fetch(`/AI/CreateUserAssistant`);
    const chatId = await result.text();
    // ...
}
```

The `AIController.CreateUserAssistant` action calls `AIReportingChatService.OpenDesignerChatAsync` to read the *documentation.pdf* file ([end-user documentation for Web Reporting Controls](https://github.com/DevExpress/dotnet-eud) in PDF format) and creates a chat based on the specified prompt. See the [AIReportingChatService.cs](./CS/ReportingApp/Services/AIReportingChatService.cs) file to review implementation details.


#### Communicate with the Assistant

Each time a user sends a message, the [`onMessageEntered`](https://js.devexpress.com/jQuery/Documentation/24_2/ApiReference/UI_Components/dxChat/Configuration/#onMessageEntered) event handler passes the request to the assistant:

```js
//...
async function getAIResponse(instance, text, id) {
    const formData = new FormData();
    formData.append('text', text);
    formData.append('chatId', id);
    lastUserQuery = text;
    return _tryFetch(instance, async () => {
        const response = await fetch('/AI/GetAnswer', {
            method: 'POST',
            body: formData
        });

        if (!response.ok) {
            _handleError(instance, { code: `${response.status}`, message: `Internal server error` });
            return;
        }
        return await response.text();
    }, 'GetAnswer');
}
// ...
function RenderAssistantMessage(instance, message) {
    instance.option({ typingUsers: [] });
    instance.renderMessage({ timestamp: new Date(), text: message, author: assistant.name, id: assistant.id });
}
// ...
onMessageEntered: async (e) => {
    lastRefreshButton?.remove();
    const instance = e.component;
    instance.option('alerts', []);
    instance.renderMessage(e.message);
    instance.option({ typingUsers: [assistant] });
    const userInput = e.message.text;
    if (!assistant.id && model.chatId) {
        assistant.id = model.chatId;
    }
    const response = await getAIResponse(instance, userInput, assistant.id);
    RenderAssistantMessage(instance, response);
}
// ...
```

`AIController.GetAnswer` receives answers from the specified assistant.

#### Files to Review:

- [ReportDesigner.cshtml](./CS/ReportingApp/Views/Home/ReportDesigner.cshtml)
- [AIDocumentOperationService.cs](./CS/ReportingApp/Services/AIDocumentOperationService.cs)
- [AIController.cs](./CS/ReportingApp/Controllers/AIController.cs)
- [aiIntegration.js](./CS/ReportingApp/wwwroot/js/aiIntegration.js)

## Documentation 

- [AI-powered Extensions for DevExpress Reporting](https://docs.devexpress.com/XtraReports/405211/ai-powered-functionality/ai-for-devexpress-reporting?v=24.2)

## More Examples

- [DevExtreme Chat - Getting Started](https://github.com/DevExpress-Examples/devextreme-getting-started-with-chat)
- [Reporting for ASP.NET Core - Summarize and Translate DevExpress Reports Using Azure OpenAI](https://github.com/DevExpress-Examples/reporting-asp-net-core-ai-summarize-and-translate)
- [Reporting for Blazor - Integrate AI-powered Summarize and Translate Features based on Azure OpenAI](https://github.com/DevExpress-Examples/blazor-reporting-ai/)
- [AI Chat for Blazor - How to add DxAIChat component in Blazor, MAUI, WPF, and WinForms applications](https://github.com/DevExpress-Examples/devexpress-ai-chat-samples)
- [Rich Text Editor and HTML Editor for Blazor - How to integrate AI-powered extensions](https://github.com/DevExpress-Examples/blazor-ai-integration-to-text-editors)

<!-- feedback -->
## Does This Example Address Your Development Requirements/Objectives?

[<img src="https://www.devexpress.com/support/examples/i/yes-button.svg"/>](https://www.devexpress.com/support/examples/survey.xml?utm_source=github&utm_campaign=web-reporting-integrate-ai-assistant&~~~was_helpful=yes) [<img src="https://www.devexpress.com/support/examples/i/no-button.svg"/>](https://www.devexpress.com/support/examples/survey.xml?utm_source=github&utm_campaign=web-reporting-integrate-ai-assistant&~~~was_helpful=no)

(you will be redirected to DevExpress.com to submit your response)
<!-- feedback end -->
