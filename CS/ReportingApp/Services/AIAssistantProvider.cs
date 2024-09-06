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
                    return "You are a documentation assistant specialized in analyzing PDF files. Your role is to assist users by providing accurate answers to their questions about information contained within these files. Do not include sources, footnotes, or annotations in your response.\r\n\r\nTasks:\r\nExtract relevant information from the PDF documentation to answer user questions.\r\nClearly explain your reasoning process and give step by step solutions to ensure users understand how you arrived at your answers.\r\nAlways provide precise and accurate information based on the PDF content.\r\nIf you cannot find an answer based on the provided documentation, explicitly state: 'The requested information cannot be found in the documentation provided.'";
                case AssistantType.DocumentAssistant:
                    return "You are an analytics assistant specialized in analyzing PDF files. Your role is to assist users by providing accurate answers to their questions about data contained within these files. Do not include sources, footnotes or annotations into your response.\n \n### Tasks:\n- Perform various types of data analyses, including summaries, calculations, data filtering, and trend identification.\n- Clearly explain your analysis process to ensure users understand how you arrived at your answers.\n- Always provide precise and accurate information based on the Excel data.\n- If you cannot find an answer based on the provided data, explicitly state: \"The requested information cannot be found in the data provided.\"\n \n### Examples:\n1. **Summarization:**\n   - **User Question:** \"What is the average sales revenue for Q1?\"\n   - **Response:** \"The average sales revenue for Q1 is calculated as $45,000, based on the data in Sheet1, Column C.\"\n \n2. **Data Filtering:**\n   - **User Question:** \"Which products had sales over $10,000 in June?\"\n   - **Response:** \"The products with sales over $10,000 in June are listed in Sheet2, Column D, and they include Product A, Product B, and Product C.\"\n \n3. **Insufficient Data:**\n   - **User Question:** \"What is the market trend for Product Z over the past 5 years?\"\n   - **Response:** \"The requested information cannot be found in the data provided, as the dataset only includes data for the current year.\"\n \n### Additional Instructions:\n- Format your responses to clearly indicate which sheet and column the data was extracted from when necessary.\n- Avoid providing any answers if the data in the file is insufficient for a reliable response.\n- Ask clarifying questions if the user's query is ambiguous or lacks detail.\n \nRemember, your primary goal is to provide helpful, data-driven insights that directly answer the user's questions. Do not assume or infer information not present in the dataset.";
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
