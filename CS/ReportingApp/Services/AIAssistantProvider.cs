using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using DevExpress.AIIntegration.Services.Assistant;

namespace ReportingApp.Services {
    public class AIAssistantProvider : IAIAssistantProvider {
        const string ASSISTANT_NOT_FOUND_ERROR = "Assistant not found";
        const string DOCUMENTATION_FILE_NAME = "documentation.pdf";
        const string DOCUMENT_ASSISTANT_PROMPT = "You are a data analysis assistant. Your task is to read information from PDF files and provide users with accurate data-driven answers based on the contents of these files. \n Key Responsibilities: \n - Perform data analysis, including data summaries, calculations, filtering, and trend identification.\n - Clearly explain your analysis process to ensure users understand how you reached your conclusions.\n - Provide precise and accurate responses strictly based on data in the file.\n - If the requested information is not available in the provided file's content, state: \"The requested information cannot be found in the data provided.\"\n - Avoid giving responses when data is insufficient for a reliable answer.\n - Ask clarifying questions when a user’s query is unclear or lacks detail.\n - Your primary goal is to deliver helpful insights that directly address user questions. Do not make assumptions or infer details not supported by data. Respond in plain text only, without sources, footnotes, or annotations.\n Avoid giving information about provided file name, assistants' IDs and other internal data";
        const string USER_ASSISTANT_PROMPT = "You are a user interface assistant (you help people use a software program). Your role is to read information from documentation files in PDF format. You assist users by providing accurate answers to their questions based on information from these files. \r\n\r\nTasks:\r\nExtract relevant information from PDF documentation to answer user questions.\r\nClearly explain your reasoning process and give step by step solutions to ensure users understand how you arrived at your answers.\r\nAlways provide precise and accurate information based on content from the documentation file.\r\nIf you cannot find an answer based on provided documentation, explicitly state: 'The requested information cannot be found in documentation provided.'\r\n Respond in plain text only, without markdown, sources, footnotes, or annotations.";

        private readonly IAIAssistantFactory assistantFactory;
        private readonly IWebHostEnvironment environment;
        private readonly AIAssistantCreator assistantCreator;

        private ConcurrentDictionary<string, IAIAssistant> Assistants { get; set; } = new ();

        private async Task<string> CreateAssistant(Stream data, string fileName, string prompt) {
            (string assistantId, string threadId) = await assistantCreator.CreateAssistantAsync(data, fileName, prompt);

            IAIAssistant assistant = await assistantFactory.GetAssistant(assistantId, threadId);
            await assistant.InitializeAsync();

            string assistantName = Guid.NewGuid().ToString();
            Assistants.TryAdd(assistantName, assistant);

            return assistantName;
        }

        public AIAssistantProvider(IAIAssistantFactory assistantFactory, IWebHostEnvironment environment, AIAssistantCreator assistantCreator) {
            this.assistantFactory = assistantFactory;
            this.environment = environment;
            this.assistantCreator = assistantCreator;
        }
        public async Task<string> CreateDocumentAssistant(Stream data) {
            return await CreateAssistant(data, Guid.NewGuid().ToString() + ".pdf", DOCUMENT_ASSISTANT_PROMPT);
        }
        public async Task<string> CreateUserAssistant() {
            string dirPath = Path.Combine(environment.ContentRootPath, "Data");
            string filePath = Path.Combine(dirPath, DOCUMENTATION_FILE_NAME);

            using (FileStream stream = File.OpenRead(filePath))
                return await CreateAssistant(stream, DOCUMENTATION_FILE_NAME, USER_ASSISTANT_PROMPT);
        }
        public void DisposeAssistant(string assistantName) {
            if(Assistants.TryRemove(assistantName, out IAIAssistant assistant)) {
                assistant.Dispose();
            } else {
                throw new Exception(ASSISTANT_NOT_FOUND_ERROR);
            }
        }
        public IAIAssistant GetAssistant(string assistantName) {
            if(!string.IsNullOrEmpty(assistantName) && Assistants.TryGetValue(assistantName, out var assistant)) {
                return assistant;
            } else {
                throw new Exception(ASSISTANT_NOT_FOUND_ERROR);
            }
        }
    }
}
