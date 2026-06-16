using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RagChatbot.BLL.Services.Interfaces;

namespace RagChatbot.RazorPages.Hubs
{
    /// <summary>
    /// Hub real-time cho Chat AI. Client gửi câu hỏi qua "StreamMessage";
    /// server kiểm tra quyền, lưu lịch sử, ủy thác cho BLL (RAG), rồi đẩy câu
    /// trả lời theo từng đoạn (streaming) kèm danh sách nguồn trích dẫn.
    /// </summary>
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IChatbotService _chatbotService;
        private readonly IChatMessageService _history;
        private readonly IUserSubjectService _userSubjectService;

        public ChatHub(
            IChatbotService chatbotService,
            IChatMessageService history,
            IUserSubjectService userSubjectService)
        {
            _chatbotService = chatbotService;
            _history = history;
            _userSubjectService = userSubjectService;
        }

        public async Task StreamMessage(Guid subjectId, string userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage)) return;
            if (!int.TryParse(Context.UserIdentifier, out int userId)) return;

            // Chỉ Admin hoặc thành viên của môn học mới được hỏi
            bool isAdmin = Context.User?.IsInRole("Admin") == true;
            if (!isAdmin && !_userSubjectService.IsAssigned(userId, subjectId))
            {
                await Clients.Caller.SendAsync("ReceiveError", "Bạn không có quyền truy cập môn học này.");
                return;
            }

            // Lưu câu hỏi của người dùng
            _history.Save(userId, subjectId, "user", userMessage);

            await Clients.Caller.SendAsync("BotTyping");

            try
            {
                var result = await _chatbotService.AskAsync(subjectId, userMessage, Context.ConnectionAborted);

                var sb = new StringBuilder();
                await foreach (var piece in result.Answer)
                {
                    sb.Append(piece);
                    await Clients.Caller.SendAsync("ReceiveChunk", piece);
                }

                if (result.Sources.Count > 0)
                    await Clients.Caller.SendAsync("ReceiveSources", result.Sources);

                await Clients.Caller.SendAsync("ReceiveDone");

                // Lưu câu trả lời của AI
                _history.Save(userId, subjectId, "assistant", sb.ToString());
            }
            catch
            {
                await Clients.Caller.SendAsync("ReceiveError",
                    "❌ Có lỗi xảy ra khi xử lý câu hỏi. Vui lòng thử lại.");
            }
        }
    }
}
