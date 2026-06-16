using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagChatbot.BLL.Services.Interfaces;
using RagChatbot.RazorPages.Services;

namespace RagChatbot.RazorPages.Pages.Home
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ISubjectService _subjectService;
        private readonly IUserService _userService;
        private readonly IDocumentService _documentService;
        private readonly IPresenceTracker _presence;

        public IndexModel(
            ISubjectService subjectService,
            IUserService userService,
            IDocumentService documentService,
            IPresenceTracker presence)
        {
            _subjectService = subjectService;
            _userService = userService;
            _documentService = documentService;
            _presence = presence;
        }

        public int SubjectCount { get; set; }
        public int UserCount { get; set; }
        public int DocumentCount { get; set; }
        public int OnlineCount { get; set; }

        private void LoadStats()
        {
            SubjectCount = _subjectService.GetAllSubjects().Count();
            UserCount = _userService.GetAllUsers().Count();
            DocumentCount = _documentService.CountAllDocuments();
            OnlineCount = _presence.OnlineCount;
        }

        public void OnGet() => LoadStats();

        // Trả số liệu mới (gọi qua AJAX khi nhận StatsChanged)
        public IActionResult OnGetStats()
        {
            LoadStats();
            return new JsonResult(new
            {
                subjects = SubjectCount,
                users = UserCount,
                documents = DocumentCount,
                online = OnlineCount
            });
        }
    }
}
