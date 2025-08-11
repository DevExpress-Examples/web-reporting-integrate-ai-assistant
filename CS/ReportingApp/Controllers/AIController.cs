using System.Threading.Tasks;
using ReportingApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace ReportingApp.Controllers {
    public class AIController : ControllerBase {
        IAIAssistantProvider AIAssistantProvider { get; set; }

        public AIController(IAIAssistantProvider assistantProvider) {
            AIAssistantProvider = assistantProvider;
        }

        public async Task<string> CreateUserAssistant() {
            return await AIAssistantProvider.CreateUserAssistant();
        }

        public async Task<string> GetAnswer([FromForm] string chatId, [FromForm] string text) {
            var assistant = AIAssistantProvider.GetAssistant(chatId);
            return await assistant.GetAnswerAsync(text);
        }

        public async Task<ActionResult> CloseChat([FromForm] string chatId) {
            await AIAssistantProvider.DisposeAssistant(chatId);
            return Ok();
        }
    }
}
