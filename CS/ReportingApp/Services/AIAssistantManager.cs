using System;
using System.ClientModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OpenAI;
using OpenAI.Assistants;
using OpenAI.Files;

namespace ReportingApp.Services {
#pragma warning disable OPENAI001
    public class AIAssistantData(string assistantId, string threadId, string fileId) {
        public string AssistantId { get; } = assistantId;
        public string ThreadId { get; } = threadId;
        public string FileId { get; } = fileId;
    }

    public class AIAssistantManager {
        readonly AssistantClient assistantClient;
        readonly OpenAIFileClient fileClient;
        readonly string deployment;

        public AIAssistantManager(OpenAIClient client, string deployment) {
            assistantClient = client.GetAssistantClient();
            fileClient = client.GetOpenAIFileClient();
            this.deployment = deployment;
        }

        public async Task<AIAssistantData> CreateAssistantAndThreadAsync(Stream data, string fileName, string instructions, CancellationToken ct = default) {
            data.Position = 0;

            ClientResult<OpenAIFile> fileResponse = await fileClient.UploadFileAsync(data, fileName, FileUploadPurpose.Assistants, ct);
            var file = fileResponse.Value;

            var resources = new ToolResources() {
                CodeInterpreter = new CodeInterpreterToolResources(),
                FileSearch =  new FileSearchToolResources()
            };
            resources.FileSearch?.NewVectorStores.Add(new VectorStoreCreationHelper([file.Id]));
            resources.CodeInterpreter.FileIds.Add(file.Id);

            AssistantCreationOptions assistantCreationOptions = new AssistantCreationOptions() {
                Name = Guid.NewGuid().ToString(),
                Instructions = instructions,
                ToolResources = resources,
                Tools = { new CodeInterpreterToolDefinition(),
                          new FileSearchToolDefinition() }
            };
            ClientResult<Assistant> assistantResponse = await assistantClient.CreateAssistantAsync(deployment, assistantCreationOptions, ct);
            var assistant = assistantResponse.Value;
            ClientResult<AssistantThread> threadResponse = await assistantClient.CreateThreadAsync(cancellationToken: ct);
            var thread = threadResponse.Value;

            return new(assistant.Id, thread.Id, file.Id);
        }

        public async Task CleanUpAssistantAsync(AIAssistantData assistantData) {
            try{
                if(!string.IsNullOrEmpty(assistantData.AssistantId)){
                    await assistantClient.DeleteAssistantAsync(assistantData.AssistantId);
                }

                if(!string.IsNullOrEmpty(assistantData.ThreadId)){
                    await assistantClient.DeleteThreadAsync(assistantData.ThreadId);
                }

                if(!string.IsNullOrEmpty(assistantData.FileId)){
                    await fileClient.DeleteFileAsync(assistantData.FileId);
                }
            }
            catch{}
        }
    }
#pragma warning restore OPENAI001
}
