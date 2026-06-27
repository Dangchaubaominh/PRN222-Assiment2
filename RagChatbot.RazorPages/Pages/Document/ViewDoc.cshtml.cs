using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagChatbot.BLL.DTOs;
using RagChatbot.BLL.Services.Interfaces;

namespace RagChatbot.RazorPages.Pages.Document
{
    [Authorize]
    public class ViewDocModel : PageModel
    {
        private readonly IDocumentService _documentService;
        private readonly IDocumentSummaryService _summaryService;
        private readonly ILearningProgressService _progressService;
        private readonly IUserSubjectService _userSubjectService;
        private readonly IWebHostEnvironment _env;

        public ViewDocModel(
            IDocumentService documentService,
            IDocumentSummaryService summaryService,
            ILearningProgressService progressService,
            IUserSubjectService userSubjectService,
            IWebHostEnvironment env)
        {
            _documentService = documentService;
            _summaryService = summaryService;
            _progressService = progressService;
            _userSubjectService = userSubjectService;
            _env = env;
        }

        public DocumentDto Document { get; set; } = default!;
        public string? FileContent { get; set; }
        public DocumentSummaryDto? Summary { get; set; }
        public List<DocumentChunkDto> Chunks { get; set; } = new();
        public int TotalWords { get; set; }

        private bool CanAccess(Guid subjectId)
        {
            if (User.IsInRole("Admin")) return true;
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return _userSubjectService.IsAssigned(userId, subjectId);
        }

        public IActionResult OnGet(Guid id)
        {
            var document = _documentService.GetDocumentById(id);
            if (document == null) return NotFound("Tài liệu không tồn tại.");
            if (!CanAccess(document.SubjectId)) return Forbid();

            Document = document;

            var fileExtension = Path.GetExtension(document.FileName).ToLower();
            if (fileExtension == ".txt")
            {
                string physicalPath = Path.Combine(_env.WebRootPath, document.FilePath.TrimStart('/'));
                if (System.IO.File.Exists(physicalPath))
                    FileContent = System.IO.File.ReadAllText(physicalPath);
            }

            Chunks = _documentService.GetChunksByDocumentId(id).ToList();
            TotalWords = Chunks.Sum(c => c.WordCount);
            Summary = _summaryService.GetByDocument(id);

            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            _progressService.RecordDocumentView(userId, document.SubjectId, id);

            return Page();
        }

        public async Task<IActionResult> OnPostGenerateSummaryAsync(Guid id)
        {
            var document = _documentService.GetDocumentById(id);
            if (document == null) return NotFound("Tài liệu không tồn tại.");
            if (!CanAccess(document.SubjectId)) return Forbid();
            if (!User.IsInRole("Admin") && !User.IsInRole("Lecturer")) return Forbid();

            var summary = await _summaryService.GenerateAsync(id);
            TempData[summary == null ? "WarningMessage" : "SuccessMessage"] = summary == null
                ? "Chưa thể tạo tóm tắt vì tài liệu chưa có chunk."
                : "Đã tạo tóm tắt tài liệu.";

            return RedirectToPage(new { id });
        }
    }
}
