using DevExpress.AIIntegration.Chat;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

namespace ReportingApp.Services {
    public class AIReportingChatService : IAIReportingChatService, IAsyncDisposable {
        const string SESSION_NOT_FOUND_ERROR = "Chat session not found";
        const string DOCUMENTATION_FILE_NAME = "documentation.pdf";
        const string DOCUMENT_ASSISTANT_PROMPT = "You are a data analysis assistant. Your task is to read information from PDF files and provide users with accurate data-driven answers based on the contents of these files. \n Key Responsibilities: \n - Perform data analysis, including data summaries, calculations, filtering, and trend identification.\n - Clearly explain your analysis process to ensure users understand how you reached your conclusions.\n - Provide precise and accurate responses strictly based on data in the file.\n - If the requested information is not available in the provided file's content, state: \"The requested information cannot be found in the data provided.\"\n - Avoid giving responses when data is insufficient for a reliable answer.\n - Ask clarifying questions when a user’s query is unclear or lacks detail.\n - Your primary goal is to deliver helpful insights that directly address user questions. Do not make assumptions or infer details not supported by data. Respond in plain text only, without sources, footnotes, or annotations.\n Avoid giving information about provided file name, assistants' IDs and other internal data";
        const string USER_ASSISTANT_PROMPT = "You are a user interface assistant (you help people use a software program). Your role is to read information from documentation files in PDF format. You assist users by providing accurate answers to their questions based on information from these files. \r\n\r\nTasks:\r\nExtract relevant information from PDF documentation to answer user questions.\r\nClearly explain your reasoning process and give step by step solutions to ensure users understand how you arrived at your answers.\r\nAlways provide precise and accurate information based on content from the documentation file.\r\nIf you cannot find an answer based on provided documentation, explicitly state: 'The requested information cannot be found in documentation provided.'\r\n Respond in plain text only, without markdown, sources, footnotes, or annotations.";

        readonly AgentFactory agentFactory;
        readonly IWebHostEnvironment environment;

        // Each chat session holds an IChatResponseProvider and a cleanup delegate that
        // removes the uploaded OpenAI resources (file + vector store) when the session ends.
        ConcurrentDictionary<string, (IChatResponseProvider Provider, Func<Task> Cleanup)> sessions = new();

        public AIReportingChatService(AgentFactory agentFactory, IWebHostEnvironment environment) {
            this.agentFactory = agentFactory;
            this.environment = environment;
        }

        async Task<string> RegisterSession(IChatResponseProvider provider, Func<Task> cleanup) {
            string sessionId = Guid.NewGuid().ToString();
            sessions.TryAdd(sessionId, (provider, cleanup));
            return sessionId;
        }

        // Opens a Data Analysis chat for Web Document Viewer.
        // The agent analyzes the exported report PDF and answers data-driven questions.
        public async Task<string> OpenDocumentChatAsync(Stream data) {
            var (provider, cleanup) = await agentFactory.CreateAgentWithFileAsync(
                data, Guid.NewGuid().ToString() + ".pdf", DOCUMENT_ASSISTANT_PROMPT);
            return await RegisterSession(provider, cleanup);
        }

        // Opens a UI help chat for Web Report Designer.
        // The agent reads the documentation PDF and answers questions about using the Designer.
        public async Task<string> OpenDesignerChatAsync() {
            string filePath = Path.Combine(environment.ContentRootPath, "Data", DOCUMENTATION_FILE_NAME);
            using var stream = File.OpenRead(filePath);
            var (provider, cleanup) = await agentFactory.CreateAgentWithFileAsync(
                stream, DOCUMENTATION_FILE_NAME, USER_ASSISTANT_PROMPT);
            return await RegisterSession(provider, cleanup);
        }

        public IChatResponseProvider GetChatProvider(string sessionId) {
            if(!string.IsNullOrEmpty(sessionId) && sessions.TryGetValue(sessionId, out var tuple))
                return tuple.Provider;
            throw new Exception(SESSION_NOT_FOUND_ERROR);
        }

        public async Task CloseChatAsync(string sessionId) {
            if(sessions.TryRemove(sessionId, out var tuple))
                await tuple.Cleanup();
            else
                throw new Exception(SESSION_NOT_FOUND_ERROR);
        }

        public async ValueTask DisposeAsync() {
            foreach(var (_, (_, cleanup)) in sessions)
                await cleanup();
            sessions.Clear();
        }
    }
}
