using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagChatbot.BLL.DTOs;
using RagChatbot.BLL.Services.Interfaces;
using RagChatbot.RazorPages.BackgroundTasks;

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
        private readonly IDocumentProcessingQueue _queue;

        public ViewDocModel(
            IDocumentService documentService,
            IDocumentSummaryService summaryService,
            ILearningProgressService progressService,
            IUserSubjectService userSubjectService,
            IWebHostEnvironment env,
            IDocumentProcessingQueue queue)
        {
            _documentService = documentService;
            _summaryService = summaryService;
            _progressService = progressService;
            _userSubjectService = userSubjectService;
            _env = env;
            _queue = queue;
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

        public async Task<IActionResult> OnPostRechunkAsync(Guid id, int newChunkSize)
        {
            var document = _documentService.GetDocumentById(id);
            if (document == null) return NotFound("Tài liệu không tồn tại.");
            if (!CanAccess(document.SubjectId)) return Forbid();
            if (!User.IsInRole("Admin") && !User.IsInRole("Lecturer")) return Forbid();

            if (newChunkSize < 100) newChunkSize = 100;
            if (newChunkSize > 2000) newChunkSize = 2000;

            await _documentService.UpdateChunkSizeAsync(id, newChunkSize);
            _queue.Enqueue(id);

            TempData["SuccessMessage"] = "Đang tiến hành cắt lại tài liệu theo kích thước mới. Vui lòng chờ trong giây lát.";
            return RedirectToPage(new { id });
        }

        public IActionResult OnGetChunksPartialAsync(Guid id)
        {
            var document = _documentService.GetDocumentById(id);
            if (document == null || !CanAccess(document.SubjectId)) return NotFound();

            var chunks = _documentService.GetChunksByDocumentId(id).ToList();
            var model = new ChunksViewModel
            {
                DocumentId = id,
                Status = document.Status,
                Chunks = chunks,
                TotalWords = chunks.Sum(c => c.WordCount)
            };

            return Partial("_ChunksListPartial", model);
        }
    }

    public class ChunksViewModel
    {
        public Guid DocumentId { get; set; }
        public string Status { get; set; }
        public List<DocumentChunkDto> Chunks { get; set; } = new();
        public int TotalWords { get; set; }
        public int Total => Chunks.Count;
        public int AvgWords => Total > 0 ? TotalWords / Total : 0;
    }
}
