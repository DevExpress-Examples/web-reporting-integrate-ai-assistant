using DevExpress.AIIntegration.Services.Assistant;
using System.IO;
using System.Threading.Tasks;

namespace ReportingApp.Services {
    public enum AssistantType {
        DocumentAssistant,
        UserAssistant
    }
    public interface IAIAssistantProvider {
        IAIAssistant GetAssistant(string assistantName);
        Task<string> CreateAssistant(AssistantType assistantType, Stream data);
        Task<string> CreateAssistant(AssistantType assistantType);
        void DisposeAssistant(string assistantName);
    }
}
