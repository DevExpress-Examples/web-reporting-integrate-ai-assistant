using System;
using System.ClientModel;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenAI;
using OpenAI.Assistants;
using OpenAI.Files;

namespace ReportingApp.Services {
#pragma warning disable OPENAI001
    public class AssistantResources {
        public Assistant Assistant { get; set; }
        public AssistantThread Thread { get; set; }
        public OpenAIFile File { get; set; }
    }

    public class AIAssistantCreator : IAsyncDisposable {
        readonly AssistantClient assistantClient;
        readonly OpenAIFileClient fileClient;
        readonly string deployment;
        readonly ConcurrentDictionary<string, AssistantResources> assistantsResources = new();

        public AIAssistantCreator(OpenAIClient client, string deployment) {
            assistantClient = client.GetAssistantClient();
            fileClient = client.GetOpenAIFileClient();
            this.deployment = deployment;
        }

        public async Task<(string assistantId, string threadId)> CreateAssistantAndThreadAsync(Stream data, string fileName, string instructions, CancellationToken ct = default) {
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

            assistantsResources.TryAdd(assistant.Id, new() {
                Assistant = assistant,
                Thread = threadResponse.Value,
                File = fileResponse.Value
            });
            return (assistant.Id, thread.Id);
        }

        public async Task CleanUpAssistantAsync(string assistantId) {
            if(assistantsResources.TryRemove(assistantId, out var resources)) {
                try{
                    if(resources.Assistant != null){
                        await assistantClient.DeleteAssistantAsync(resources.Assistant.Id);
                    }

                    if(resources.Thread != null){
                        await assistantClient.DeleteThreadAsync(resources.Thread.Id);
                    }

                    if(resources.File != null){
                        await fileClient.DeleteFileAsync(resources.File.Id);
                    }
                }
                catch{}
            }
        }

        public async ValueTask DisposeAsync() {
            var assistantIds = assistantsResources.Keys.ToList();
            foreach (var assistantId in assistantIds){
                await CleanUpAssistantAsync(assistantId);
            }
            assistantsResources.Clear();
        }
    }
#pragma warning restore OPENAI001
}
