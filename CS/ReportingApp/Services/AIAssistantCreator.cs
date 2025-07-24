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
    public class AIAssistantCreator : IDisposable{
        readonly AssistantClient assistantClient;
        readonly OpenAIFileClient fileClient;
        readonly string deployment;
        AssistantThread thread;
        Assistant assistant;
        OpenAIFile file;

        public AIAssistantCreator(OpenAIClient client, string deployment) {
            assistantClient = client.GetAssistantClient();
            fileClient = client.GetOpenAIFileClient();
            this.deployment = deployment;
        }

        public async Task<(string assistantId, string threadId)> CreateAssistantAndThreadAsync(Stream data, string fileName, string instructions, CancellationToken ct = default) {
            data.Position = 0;

            ClientResult<OpenAIFile> fileResponse = await fileClient.UploadFileAsync(data, fileName, FileUploadPurpose.Assistants, ct);
            file = fileResponse.Value;

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
            assistant = assistantResponse.Value;
            ClientResult<AssistantThread> threadResponse = await assistantClient.CreateThreadAsync(cancellationToken: ct);
            thread = threadResponse.Value;

            return (assistantResponse.Value.Id, threadResponse.Value.Id);
        }
        
        public void Dispose() {
            try {
                if(assistant != null){
                    assistantClient?.DeleteAssistant(assistant.Id);
                    assistantClient?.DeleteThread(thread.Id);
                    fileClient?.DeleteFile(file.Id);
                    assistant = null;
                    thread = null;
                    file = null;
                }
            } catch {}
        }
    }
#pragma warning restore OPENAI001
}
