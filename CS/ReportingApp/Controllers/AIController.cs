using System.Text;
using System.Threading.Tasks;
using ReportingApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;

namespace ReportingApp.Controllers {
    public class AIController : ControllerBase {
        private readonly IAIReportingChatService chatService;

        public AIController(IAIReportingChatService chatService) {
            this.chatService = chatService;
        }

        public async Task<string> CreateUserAssistant() {
            return await chatService.OpenDesignerChatAsync();
        }

        public async Task<string> GetAnswer([FromForm] string chatId, [FromForm] string text) {
            var provider = chatService.GetChatProvider(chatId);

            var sb = new StringBuilder();
            await foreach(var update in provider.GetResponseAsync(
                [new ChatMessage(ChatRole.User, text)], useStreaming: false))
                sb.Append(update.Text);

            return sb.ToString();
        }

        public async Task<ActionResult> CloseChat([FromForm] string chatId) {
            await chatService.CloseChatAsync(chatId);
            return Ok();
        }
    }
}
