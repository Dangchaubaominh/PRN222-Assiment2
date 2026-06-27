using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RagChatbot.BLL.Services.Interfaces;
using RagChatbot.DAL.Entities;

namespace RagChatbot.RazorPages.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IChatbotService _chatbotService;
        private readonly IChatMessageService _history;
        private readonly IChatSessionService _sessions;
        private readonly ILearningProgressService _progress;
        private readonly IUserSubjectService _userSubjectService;

        public ChatHub(
            IChatbotService chatbotService,
            IChatMessageService history,
            IChatSessionService sessions,
            ILearningProgressService progress,
            IUserSubjectService userSubjectService)
        {
            _chatbotService = chatbotService;
            _history = history;
            _sessions = sessions;
            _progress = progress;
            _userSubjectService = userSubjectService;
        }

        public async Task StreamMessage(Guid subjectId, int sessionId, string userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage)) return;
            if (!int.TryParse(Context.UserIdentifier, out int userId)) return;

            bool isAdmin = Context.User?.IsInRole("Admin") == true;
            if (!isAdmin && !_userSubjectService.IsAssigned(userId, subjectId))
            {
                await Clients.Caller.SendAsync("ReceiveError", "Bạn không có quyền truy cập môn học này.");
                return;
            }

            if (!_sessions.BelongsToUserSubject(userId, subjectId, sessionId))
            {
                await Clients.Caller.SendAsync("ReceiveError", "Phiên chat không hợp lệ.");
                return;
            }

            _history.Save(userId, subjectId, sessionId, "user", userMessage);
            _sessions.TouchSession(userId, subjectId, sessionId);
            _progress.RecordChatQuestion(userId, subjectId);

            await Clients.Caller.SendAsync("BotTyping");

            try
            {
                string role = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";
                var result = await _chatbotService.AskAsync(subjectId, userMessage, userId, role, Context.ConnectionAborted);

                var sb = new StringBuilder();
                await foreach (var piece in result.Answer)
                {
                    sb.Append(piece);
                    await Clients.Caller.SendAsync("ReceiveChunk", piece);
                }

                if (result.Sources.Count > 0)
                    await Clients.Caller.SendAsync("ReceiveSources", result.Sources);

                await Clients.Caller.SendAsync("ReceiveDone");

                int messageId = _history.Save(userId, subjectId, sessionId, "assistant", sb.ToString(), result.Sources);
                _sessions.TouchSession(userId, subjectId, sessionId);
                await Clients.Caller.SendAsync("ReceiveMessageId", messageId);
            }
            catch
            {
                await Clients.Caller.SendAsync("ReceiveError", "Có lỗi xảy ra khi xử lý câu hỏi. Vui lòng thử lại.");
            }
        }

        public async Task SubmitFeedback(int messageId, int? feedback)
        {
            if (!int.TryParse(Context.UserIdentifier, out int userId)) return;

            FeedbackType? type = feedback switch
            {
                1 => FeedbackType.Upvote,
                -1 => FeedbackType.Downvote,
                _ => null
            };

            _history.UpdateFeedback(messageId, userId, type);
            await Clients.Caller.SendAsync("FeedbackReceived", messageId, feedback);
        }
    }
}
