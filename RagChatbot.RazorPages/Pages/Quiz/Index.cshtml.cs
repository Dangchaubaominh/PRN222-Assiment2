using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagChatbot.BLL.DTOs;
using RagChatbot.BLL.Services.Interfaces;

namespace RagChatbot.RazorPages.Pages.Quiz
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IQuizService _quizService;
        private readonly IDocumentService _documentService;
        private readonly ISubjectService _subjectService;
        private readonly IUserSubjectService _userSubjectService;

        public IndexModel(
            IQuizService quizService,
            IDocumentService documentService,
            ISubjectService subjectService,
            IUserSubjectService userSubjectService)
        {
            _quizService = quizService;
            _documentService = documentService;
            _subjectService = subjectService;
            _userSubjectService = userSubjectService;
        }

        [BindProperty(SupportsGet = true)]
        public Guid SubjectId { get; set; }

        public SubjectDto? Subject { get; set; }
        public List<DocumentDto> Documents { get; set; } = new();
        public List<QuizDto> Quizzes { get; set; } = new();
        public Dictionary<int, QuizResultDto> LatestResults { get; set; } = new();
        public Dictionary<int, int> AttemptCounts { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (!await LoadPageDataAsync())
                return Forbid();

            return Page();
        }

        public async Task<IActionResult> OnPostGenerateAsync(Guid documentId, int questionCount = 5)
        {
            if (!User.IsInRole("Admin") && !User.IsInRole("Lecturer"))
                return Forbid();

            if (!await LoadPageDataAsync())
                return Forbid();

            if (documentId == Guid.Empty)
            {
                TempData["WarningMessage"] = "Vui lòng chọn tài liệu trước khi tạo quiz.";
                return RedirectToPage(new { SubjectId });
            }

            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var quiz = await _quizService.GenerateQuizAsync(documentId, Math.Clamp(questionCount, 3, 10), userId);

            TempData[quiz == null ? "WarningMessage" : "SuccessMessage"] = quiz == null
                ? "Chưa thể tạo quiz vì tài liệu chưa có nội dung phù hợp."
                : "Đã tạo quiz từ tài liệu.";

            return RedirectToPage(new { SubjectId });
        }

        private async Task<bool> LoadPageDataAsync()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            string role = User.IsInRole("Admin") ? "Admin" : User.IsInRole("Lecturer") ? "Lecturer" : "Student";

            if (!User.IsInRole("Admin") && !_userSubjectService.IsAssigned(userId, SubjectId))
                return false;

            Subject = _subjectService.GetSubjectById(SubjectId);
            Documents = _documentService
                .GetDocumentsBySubject(SubjectId, userId, role)
                .Where(d => d.Status == "Completed")
                .OrderByDescending(d => d.UploadedAt)
                .ToList();

            Quizzes = (await _quizService.GetQuizzesBySubjectAsync(SubjectId))
                .OrderByDescending(q => q.CreatedAt)
                .ToList();

            LatestResults = new Dictionary<int, QuizResultDto>();
            AttemptCounts = new Dictionary<int, int>();

            foreach (var quiz in Quizzes)
            {
                var latestResult = await _quizService.GetLatestResultAsync(userId, quiz.Id);
                if (latestResult != null)
                    LatestResults[quiz.Id] = latestResult;

                AttemptCounts[quiz.Id] = await _quizService.CountAttemptsAsync(userId, quiz.Id);
            }

            return true;
        }
    }
}
