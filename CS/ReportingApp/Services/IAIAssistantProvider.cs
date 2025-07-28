using DevExpress.AIIntegration.Services.Assistant;
using System.IO;
using System.Threading.Tasks;

namespace ReportingApp.Services {
    public interface IAIAssistantProvider {
        IAIAssistant GetAssistant(string assistantId);
        Task<string> CreateDocumentAssistant(Stream data);
        Task<string> CreateUserAssistant();
        Task DisposeAssistant(string assistantId);
    }
}
