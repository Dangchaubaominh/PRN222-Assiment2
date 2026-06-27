using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagChatbot.BLL.DTOs;
using RagChatbot.BLL.Services.Interfaces;

namespace RagChatbot.RazorPages.Pages.Chat
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IChatMessageService _history;
        private readonly IChatSessionService _sessions;
        private readonly IUserSubjectService _userSubjectService;

        public IndexModel(
            IChatMessageService history,
            IChatSessionService sessions,
            IUserSubjectService userSubjectService)
        {
            _history = history;
            _sessions = sessions;
            _userSubjectService = userSubjectService;
        }

        [BindProperty(SupportsGet = true)]
        public Guid SubjectId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SessionId { get; set; }

        public ChatSessionDto? ActiveSession { get; set; }
        public IEnumerable<ChatSessionDto> Sessions { get; set; } = new List<ChatSessionDto>();
        public IEnumerable<ChatMessageDto> History { get; set; } = new List<ChatMessageDto>();

        public IActionResult OnGet()
        {
            int userId = GetUserId();

            if (!CanAccessSubject(userId))
                return Forbid();

            ActiveSession = _sessions.GetOrCreateActiveSession(userId, SubjectId, SessionId);
            SessionId = ActiveSession.Id;
            Sessions = _sessions.GetSessions(userId, SubjectId);
            History = _history.GetHistory(userId, SubjectId, ActiveSession.Id);

            return Page();
        }

        public IActionResult OnPostNewSession()
        {
            int userId = GetUserId();

            if (!CanAccessSubject(userId))
                return Forbid();

            var session = _sessions.CreateSession(userId, SubjectId);
            return RedirectToPage(new { SubjectId, SessionId = session.Id });
        }

        public IActionResult OnPostClearHistory()
        {
            int userId = GetUserId();

            if (!CanAccessSubject(userId))
                return Forbid();

            if (!SessionId.HasValue || !_sessions.BelongsToUserSubject(userId, SubjectId, SessionId.Value))
                return RedirectToPage(new { SubjectId });

            _history.ClearHistory(userId, SubjectId, SessionId.Value);
            _sessions.TouchSession(userId, SubjectId, SessionId.Value);

            return RedirectToPage(new { SubjectId, SessionId });
        }

        public IActionResult OnPostDeleteSession(int sessionId)
        {
            int userId = GetUserId();

            if (!CanAccessSubject(userId))
                return Forbid();

            _sessions.DeleteSession(userId, SubjectId, sessionId);
            return RedirectToPage(new { SubjectId });
        }

        private int GetUserId()
            => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private bool CanAccessSubject(int userId)
            => User.IsInRole("Admin") || _userSubjectService.IsAssigned(userId, SubjectId);
    }
}
