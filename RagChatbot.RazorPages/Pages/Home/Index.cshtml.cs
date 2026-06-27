using System.Security.Claims;
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
        private readonly IUserSubjectService _userSubjectService;
        private readonly IUserService _userService;
        private readonly IDocumentService _documentService;
        private readonly ILearningProgressService _progressService;
        private readonly IPresenceTracker _presence;

        public IndexModel(
            ISubjectService subjectService,
            IUserSubjectService userSubjectService,
            IUserService userService,
            IDocumentService documentService,
            ILearningProgressService progressService,
            IPresenceTracker presence)
        {
            _subjectService = subjectService;
            _userSubjectService = userSubjectService;
            _userService = userService;
            _documentService = documentService;
            _progressService = progressService;
            _presence = presence;
        }

        public int SubjectCount { get; set; }
        public int UserCount { get; set; }
        public int DocumentCount { get; set; }
        public int OnlineCount { get; set; }
        public RagChatbot.BLL.DTOs.LearningDashboardDto? LearningDashboard { get; set; }

        private void LoadStats()
        {
            OnlineCount = _presence.OnlineCount;

            if (User.IsInRole("Admin"))
            {
                SubjectCount = _subjectService.GetAllSubjects().Count();
                DocumentCount = _documentService.CountAllDocuments();
                UserCount = _userService.GetAllUsers().Count();
                return;
            }

            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            string role = User.FindFirstValue(ClaimTypes.Role) ?? "";
            var assigned = _userSubjectService.GetAssignedSubjects(userId).ToList();
            SubjectCount = assigned.Count;
            DocumentCount = assigned.Sum(s => _documentService.GetDocumentsBySubject(s.Id, userId, role).Count());
            UserCount = 0;
            LearningDashboard = _progressService.GetDashboard(userId);
        }

        public void OnGet() => LoadStats();

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
