using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagChatbot.BLL.DTOs;
using RagChatbot.BLL.Services.Interfaces;
using RagChatbot.RazorPages.Services;

namespace RagChatbot.RazorPages.Pages.User
{
    [Authorize(Roles = "Admin")]
    public class EditModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly IRealtimeNotifier _notifier;

        public EditModel(IUserService userService, IRealtimeNotifier notifier)
        {
            _userService = userService;
            _notifier = notifier;
        }

        [BindProperty]
        public UserEditDto Input { get; set; } = new();

        public IActionResult OnGet(int id)
        {
            var dto = _userService.GetEditById(id);
            if (dto == null) return NotFound();
            Input = dto;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            // Vai trò cũ (trước khi cập nhật) để biết có đổi quyền hay không
            var before = _userService.GetEditById(Input.Id);
            string? oldRole = before?.Role;

            bool success = _userService.UpdateUserInfo(Input);
            if (!success)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tài khoản cần cập nhật.";
                return RedirectToPage("Index");
            }

            // Nếu đổi vai trò → lưu thông báo (xem ở chuông khi đăng nhập lại) + buộc đăng xuất
            if (oldRole != null && oldRole != Input.Role)
            {
                await _notifier.NotifyUserAsync(
                    Input.Id,
                    $"Vai trò của bạn vừa được thay đổi thành \"{Input.Role}\". Bạn đã được đăng xuất, vui lòng đăng nhập lại để áp dụng quyền mới.",
                    "warning");

                await _notifier.ForceLogoutAsync(Input.Id, "role");

                TempData["SuccessMessage"] = $"Đã đổi vai trò tài khoản \"{Input.Username}\" và buộc đăng xuất phiên hiện tại.";
            }
            else
            {
                TempData["SuccessMessage"] = $"Đã cập nhật thông tin tài khoản \"{Input.Username}\" thành công.";
            }

            return RedirectToPage("Index");
        }
    }
}
