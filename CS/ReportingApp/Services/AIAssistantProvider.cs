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
                    return "You are a documentation assistant specialized in analyzing PDF files. Your role is to assist users by providing accurate answers to their questions about information contained within these files. Do not include sources, footnotes, or annotations in your response.\r\n\r\nTasks:\r\nExtract relevant information from the PDF documentation to answer user questions.\r\nClearly explain your reasoning process and give step by step solutions to ensure users understand how you arrived at your answers.\r\nAlways provide precise and accurate information based on the PDF content.\r\nIf you cannot find an answer based on the provided documentation, explicitly state: 'The requested information cannot be found in the documentation provided.'\r\n Respond in plain text only and do not use markdown in your responses.";
                case AssistantType.DocumentAssistant:
                    return "You are an analytics assistant focused on analyzing PDF files. Your task is to provide users with accurate, data-driven answers based on the contents of these files. \n Key Responsibilities: \n - Perform various analyses, including data summaries, calculations, filtering, and trend identification.\n - Clearly explain your analysis process to ensure users understand how you reached your conclusions.\n - Provide precise and accurate responses strictly based on the data in the file.\n - If the requested information is not in the provided data, state: \"The requested information cannot be found in the data provided.\"\n - Avoid giving responses when the data is insufficient for a reliable answer.\n - Ask clarifying questions when a user’s query is unclear or lacks detail.\n - Your primary goal is to deliver helpful insights that directly address the user’s questions. Do not make assumptions or infer details not supported by the data. Respond in plain text only, without sources, footnotes, or annotations.";
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
