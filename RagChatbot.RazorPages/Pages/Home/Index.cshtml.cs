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
        private readonly IUserService _userService;
        private readonly IDocumentService _documentService;
        private readonly IUserSubjectService _userSubjectService;
        private readonly IPresenceTracker _presence;

        public IndexModel(
            ISubjectService subjectService,
            IUserService userService,
            IDocumentService documentService,
            IUserSubjectService userSubjectService,
            IPresenceTracker presence)
        {
            _subjectService = subjectService;
            _userService = userService;
            _documentService = documentService;
            _userSubjectService = userSubjectService;
            _presence = presence;
        }

        public int SubjectCount { get; set; }
        public int UserCount { get; set; }
        public int DocumentCount { get; set; }
        public int OnlineCount { get; set; }

        private void LoadStats()
        {
            OnlineCount = _presence.OnlineCount;

            // Admin: số liệu toàn hệ thống
            if (User.IsInRole("Admin"))
            {
                SubjectCount = _subjectService.GetAllSubjects().Count();
                DocumentCount = _documentService.CountAllDocuments();
                UserCount = _userService.GetAllUsers().Count();
                return;
            }

            // Giảng viên / Sinh viên: chỉ tính theo các môn mình được gán
            // (để con số khớp với danh sách Môn học mà họ thực sự thấy)
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var assigned = _userSubjectService.GetAssignedSubjects(userId).ToList();
            SubjectCount = assigned.Count;
            DocumentCount = assigned.Sum(s => _documentService.GetDocumentsBySubject(s.Id).Count());
            UserCount = 0; // thẻ Tài khoản chỉ hiển thị cho Admin
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
