using DevExpress.AIIntegration.Chat;
using System.IO;
using System.Threading.Tasks;

namespace ReportingApp.Services {
    public interface IAIReportingChatService {
        IChatResponseProvider GetChatProvider(string sessionId);
        Task<string> OpenDocumentChatAsync(Stream data);
        Task<string> OpenDesignerChatAsync();
        Task CloseChatAsync(string sessionId);
    }
}
