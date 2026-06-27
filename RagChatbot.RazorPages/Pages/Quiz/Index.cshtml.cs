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
        private readonly ILearningProgressService _progressService;

        public IndexModel(
            IQuizService quizService,
            IDocumentService documentService,
            ISubjectService subjectService,
            IUserSubjectService userSubjectService,
            ILearningProgressService progressService)
        {
            _quizService = quizService;
            _documentService = documentService;
            _subjectService = subjectService;
            _userSubjectService = userSubjectService;
            _progressService = progressService;
        }

        [BindProperty(SupportsGet = true)]
        public Guid SubjectId { get; set; }

        public SubjectDto? Subject { get; set; }
        public IEnumerable<DocumentDto> Documents { get; set; } = new List<DocumentDto>();
        public IEnumerable<QuizDto> Quizzes { get; set; } = new List<QuizDto>();
        public Dictionary<int, QuizAttemptDto> LatestAttempts { get; set; } = new();

        public IActionResult OnGet()
        {
            if (!LoadPageData())
                return Forbid();

            return Page();
        }

        public async Task<IActionResult> OnPostGenerateAsync(Guid? documentId, int questionCount = 5)
        {
            if (!User.IsInRole("Admin") && !User.IsInRole("Lecturer")) return Forbid();
            if (!LoadPageData()) return Forbid();

            var quiz = await _quizService.GenerateAsync(SubjectId, documentId, Math.Clamp(questionCount, 3, 10));
            TempData[quiz == null ? "WarningMessage" : "SuccessMessage"] = quiz == null
                ? "Chưa thể tạo quiz vì tài liệu chưa có chunk."
                : "Đã tạo quiz từ tài liệu.";

            return RedirectToPage(new { SubjectId });
        }

        public IActionResult OnPostSubmit(int quizId)
        {
            if (!LoadPageData()) return Forbid();

            var answers = Request.Form
                .Where(kv => kv.Key.StartsWith("answers[", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(
                    kv => int.Parse(kv.Key.Replace("answers[", "").Replace("]", "")),
                    kv => kv.Value.ToString());

            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var attempt = _quizService.Submit(userId, quizId, answers);
            if (attempt != null)
            {
                _progressService.RecordQuizAttempt(userId, SubjectId, quizId);
                TempData["SuccessMessage"] = $"Đã lưu lần làm thứ {attempt.AttemptNumber}. Bạn đạt {attempt.Score}/{attempt.TotalQuestions} câu đúng.";
            }

            return RedirectToPage(new { SubjectId });
        }

        private bool LoadPageData()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (!User.IsInRole("Admin") && !_userSubjectService.IsAssigned(userId, SubjectId))
                return false;

            Subject = _subjectService.GetSubjectById(SubjectId);
            Documents = _documentService.GetDocumentsBySubject(SubjectId).Where(d => d.Status == "Completed").ToList();
            Quizzes = _quizService.GetBySubject(SubjectId).ToList();
            LatestAttempts = Quizzes
                .Select(q => _quizService.GetLatestAttempt(userId, q.Id))
                .Where(a => a != null)
                .ToDictionary(a => a!.QuizId, a => a!);

            return true;
        }
    }
}
