using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using RagChatbot.BLL.DTOs;
using RagChatbot.BLL.Services.Interfaces;
using RagChatbot.RazorPages.Services;

namespace RagChatbot.RazorPages.Pages.Document
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IDocumentService _documentService;
        private readonly ISubjectService _subjectService;
        private readonly IUserSubjectService _userSubjectService;
        private readonly IWebHostEnvironment _env;
        private readonly IDashboardNotifier _dashboard;

        public IndexModel(
            IDocumentService documentService,
            ISubjectService subjectService,
            IUserSubjectService userSubjectService,
            IWebHostEnvironment env,
            IDashboardNotifier dashboard)
        {
            _documentService = documentService;
            _subjectService = subjectService;
            _userSubjectService = userSubjectService;
            _env = env;
            _dashboard = dashboard;
        }

        [BindProperty(SupportsGet = true)]
        public Guid SubjectId { get; set; }

        public SubjectDto? Subject { get; set; }
        public IEnumerable<DocumentDto> Documents { get; set; } = new List<DocumentDto>();

        // Admin xem mọi môn; còn lại chỉ môn mình là thành viên
        private bool CanAccess(Guid subjectId)
        {
            if (User.IsInRole("Admin")) return true;
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return _userSubjectService.IsAssigned(userId, subjectId);
        }

        public IActionResult OnGet()
        {
            Subject = _subjectService.GetSubjectById(SubjectId);
            if (Subject == null) return NotFound();
            if (!CanAccess(SubjectId)) return Forbid();

            Documents = _documentService.GetDocumentsBySubject(SubjectId);
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id, Guid subjectId)
        {
            if (!(User.IsInRole("Admin") || User.IsInRole("Lecturer"))) return Forbid();
            if (!CanAccess(subjectId)) return Forbid();

            var doc = _documentService.GetDocumentById(id);
            string fileName = doc?.FileName ?? "Tài liệu";
            _documentService.DeleteDocument(id, _env.WebRootPath);
            await _dashboard.StatsChangedAsync();
            TempData["SuccessMessage"] = $"Đã xóa tài liệu \"{fileName}\" thành công.";
            return RedirectToPage(new { subjectId });
        }
    }
}
