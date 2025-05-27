using DevExpress.AIIntegration.Services.Assistant;
using System.IO;
using System.Threading.Tasks;

namespace ReportingApp.Services {
    public interface IAIAssistantProvider {
        IAIAssistant GetAssistant(string assistantName);
        Task<string> CreateDocumentAssistant(Stream data);
        Task<string> CreateUserAssistant();
        void DisposeAssistant(string assistantName);
    }
}
