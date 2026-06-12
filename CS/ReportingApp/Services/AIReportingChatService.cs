using DevExpress.AIIntegration.Chat;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

namespace ReportingApp.Services {
    public class AIReportingChatService : IAIReportingChatService, IAsyncDisposable {
        const string SessionNotFoundError = "Chat session not found";
        const string DocumentationFileName = "documentation.pdf";

        readonly AgentFactory agentFactory;
        readonly IWebHostEnvironment environment;

        // Each chat session holds an IChatResponseProvider and a cleanup delegate that
        // removes the uploaded OpenAI resources (file and vector store) when the session ends.
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

        // Open a Data Analysis chat for the Web Document Viewer.
        // The agent analyzes the exported report PDF and answers data-driven questions.
        public async Task<string> OpenDocumentChatAsync(Stream data) {
            var (provider, cleanup) = await agentFactory.CreateAgentWithFileAsync(
                data, Guid.NewGuid().ToString() + ".pdf", AgentInstructions.DocumentAssistantPrompt);
            return await RegisterSession(provider, cleanup);
        }

        // Open a UI help chat for the Web Report Designer.
        // The agent reads the documentation PDF and answers questions about using the Designer.
        public async Task<string> OpenDesignerChatAsync() {
            string filePath = Path.Combine(environment.ContentRootPath, "Data", DocumentationFileName);
            using var stream = File.OpenRead(filePath);
            var (provider, cleanup) = await agentFactory.CreateAgentWithFileAsync(
                stream, DocumentationFileName, AgentInstructions.DesignerAssistantPrompt);
            return await RegisterSession(provider, cleanup);
        }

        public IChatResponseProvider GetChatProvider(string sessionId) {
            if(!string.IsNullOrEmpty(sessionId) && sessions.TryGetValue(sessionId, out var tuple))
                return tuple.Provider;
            throw new Exception(SessionNotFoundError);
        }

        public async Task CloseChatAsync(string sessionId) {
            if(sessions.TryRemove(sessionId, out var tuple))
                await tuple.Cleanup();
            else
                throw new Exception(SessionNotFoundError);
        }

        public async ValueTask DisposeAsync() {
            foreach(var (_, (_, cleanup)) in sessions)
                await cleanup();
            sessions.Clear();
        }
    }
}
