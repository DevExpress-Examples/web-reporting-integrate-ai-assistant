using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using DevExpress.AIIntegration.OpenAI.Services;
using DevExpress.AIIntegration.Services.Assistant;
using Microsoft.AspNetCore.Hosting;

namespace ReportingApp.Services {
    public class AIAssistantProvider : IAIAssistantProvider {
        private readonly IAIAssistantFactory assistantFactory;
        private readonly IWebHostEnvironment environment;

        private ConcurrentDictionary<string, IAIAssistant> Assistants { get; set; } = new ();
        public AIAssistantProvider(IAIAssistantFactory assistantFactory, IWebHostEnvironment environment) {
            this.assistantFactory = assistantFactory;
            this.environment = environment;
        }
        async Task LoadDocumentation(IAIAssistant assistant, string prompt) {
            var dirPath = Path.Combine(environment.ContentRootPath, "Data");
            var filePath = Path.Combine(dirPath, "documentation.pdf");

            using(FileStream stream = File.OpenRead(filePath)) {
                await assistant.InitializeAsync(new OpenAIAssistantOptions("documentation.pdf", stream, prompt));
            }
        }
        string GetPrompt(AssistantType assistantType) {
            switch(assistantType) {
                case AssistantType.UserAssistant:
                    return "You are a user interface assistant (you help people use a software program). Your role is to read information from documentation files in PDF format. You assist users by providing accurate answers to their questions based on information from these files. \r\n\r\nTasks:\r\nExtract relevant information from PDF documentation to answer user questions.\r\nClearly explain your reasoning process and give step by step solutions to ensure users understand how you arrived at your answers.\r\nAlways provide precise and accurate information based on content from the documentation file.\r\nIf you cannot find an answer based on provided documentation, explicitly state: 'The requested information cannot be found in documentation provided.'\r\n Respond in plain text only, without markdown, sources, footnotes, or annotations.";
                case AssistantType.DocumentAssistant:
                    return "You are a data analysis assistant. Your task is to read information from PDF files and provide users with accurate data-driven answers based on the contents of these files. \n Key Responsibilities: \n - Perform data analysis, including data summaries, calculations, filtering, and trend identification.\n - Clearly explain your analysis process to ensure users understand how you reached your conclusions.\n - Provide precise and accurate responses strictly based on data in the file.\n - If the requested information is not available in the provided file's content, state: \"The requested information cannot be found in the data provided.\"\n - Avoid giving responses when data is insufficient for a reliable answer.\n - Ask clarifying questions when a user’s query is unclear or lacks detail.\n - Your primary goal is to deliver helpful insights that directly address user questions. Do not make assumptions or infer details not supported by data. Respond in plain text only, without sources, footnotes, or annotations.";
                default:
                    return "";
            }
        }
        public void DisposeAssistant(string assistantName) {
            if(Assistants.TryRemove(assistantName, out IAIAssistant assistant)) {
                assistant.Dispose();
            } else {
                throw new Exception("Assistant not found");
            }
        }
        public IAIAssistant GetAssistant(string assistantName) {
            if(!string.IsNullOrEmpty(assistantName) && Assistants.TryGetValue(assistantName, out var assistant)) {
                return assistant;
            } else {
                throw new Exception("Assistant not found");
            }
        }
        public async Task<string> CreateAssistant(AssistantType assistantType, Stream data) {
            var assistantName = Guid.NewGuid().ToString();
            var assistant = await assistantFactory.CreateAssistant(assistantName);
            Assistants.TryAdd(assistantName, assistant);

            var prompt = GetPrompt(assistantType);
            if(assistantType == AssistantType.UserAssistant) {
                await LoadDocumentation(assistant, prompt);
            } else {
                await assistant.InitializeAsync(new OpenAIAssistantOptions(Guid.NewGuid().ToString() + ".pdf", data, prompt));
            }
            return assistantName;
        }

        public Task<string> CreateAssistant(AssistantType assistantType) {
            return CreateAssistant(assistantType, null);
        }
    }
}
