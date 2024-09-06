using System.IO;
using System.Threading.Tasks;
using DevExpress.XtraPrinting;
using DevExpress.XtraReports.Web.WebDocumentViewer;
using DevExpress.XtraReports.Web.WebDocumentViewer.DataContracts;

namespace ReportingApp.Services {
    public class AIDocumentOperationService : DocumentOperationService {
        private IAIAssistantProvider AIAssistantProvider { get; set; }

        public AIDocumentOperationService(IAIAssistantProvider assistantProvider) {
            AIAssistantProvider = assistantProvider;
        }

        public override bool CanPerformOperation(DocumentOperationRequest request) {
            return true;
        }
        public override async Task<DocumentOperationResponse> PerformOperationAsync(DocumentOperationRequest request, PrintingSystemBase printingSystem, PrintingSystemBase printingSystemWithEditingFields) {
            using(var stream = new MemoryStream()) {
                printingSystem.ExportToPdf(stream, printingSystem.ExportOptions.Pdf);
                var assistantName = await AIAssistantProvider.CreateAssistant(AssistantType.DocumentAssistant, stream);
                return new DocumentOperationResponse {
                    DocumentId = request.DocumentId,
                    CustomData = assistantName,
                    Succeeded = true
                };
            }
        }
    }
}
