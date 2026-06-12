using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using DevExpress.AIIntegration.Agents;
using DevExpress.AIIntegration.Chat;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI.Files;
using OpenAI.Responses;
using OpenAI.VectorStores;

namespace ReportingApp.Services {
    // The OpenAI.Responses API is for evaluation purposes only and is subject to change or removal in a future update.
    // The following code suppresses the OPENAI001 diagnostic.
#pragma warning disable OPENAI001
    public class AgentFactory {
        readonly AzureOpenAIClient openAIClient;
        readonly string deployment;
        readonly ILogger<AgentFactory> logger;

        public AgentFactory(AzureOpenAIClient openAIClient, string deployment, ILogger<AgentFactory> logger) {
            this.openAIClient = openAIClient;
            this.deployment = deployment;
            this.logger = logger;
        }

        // Upload a PDF stream to OpenAI, create a short-lived vector store, and return
        // an IChatResponseProvider backed by a Responses API agent with File Search and
        // Code Interpreter tools. The cleanup delegate removes the uploaded resources.
        public async Task<(IChatResponseProvider Provider, Func<Task> Cleanup)> CreateAgentWithFileAsync(
            Stream data, string fileName, string instructions, CancellationToken ct = default) {

            var fileClient = openAIClient.GetOpenAIFileClient();
            var vectorStoreClient = openAIClient.GetVectorStoreClient();
            var responsesClient = openAIClient.GetResponsesClient();

            if(data.CanSeek)
                data.Position = 0;

            var file = (await fileClient.UploadFileAsync(data, fileName, FileUploadPurpose.Assistants, ct)).Value;

            var vectorStore = (await vectorStoreClient.CreateVectorStoreAsync(
                new VectorStoreCreationOptions {
                    ExpirationPolicy = new VectorStoreExpirationPolicy(VectorStoreExpirationAnchor.LastActiveAt, 1)
                }, ct)).Value;

            await vectorStoreClient.AddFileToVectorStoreAsync(vectorStore.Id, file.Id, ct);

            var tools = new List<AITool> {
                new HostedFileSearchTool { Inputs = [new HostedVectorStoreContent(vectorStore.Id)] },
                new HostedCodeInterpreterTool { Inputs = [new HostedFileContent(file.Id)] }
            };

            var aiAgent = responsesClient.AsAIAgent(
                instructions: instructions,
                tools: tools,
                name: $"Reporting Agent {Guid.NewGuid():N}",
                model: deployment);

            var session = await aiAgent.CreateSessionAsync(ct);
            var provider = aiAgent.AsIChatResponseProvider(session);

            async Task Cleanup() {
                try { await vectorStoreClient.DeleteVectorStoreAsync(vectorStore.Id); }
                catch(Exception ex) { logger.LogError(ex, "Error deleting vector store {Id}", vectorStore.Id); }

                try { await fileClient.DeleteFileAsync(file.Id); }
                catch(Exception ex) { logger.LogError(ex, "Error deleting file {Id}", file.Id); }
            }

            return (provider, Cleanup);
        }
    }
#pragma warning restore OPENAI001
}
