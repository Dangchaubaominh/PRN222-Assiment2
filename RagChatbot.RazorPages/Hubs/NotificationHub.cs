using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RagChatbot.BLL.Services.Interfaces;

namespace RagChatbot.RazorPages.Hubs
{
    /// <summary>
    /// Hub thông báo chung. SignalR tự ánh xạ connection → user qua claim
    /// NameIdentifier (đặt lúc đăng nhập), nên server gọi Clients.User(userId)
    /// là tới đúng người dù họ đang ở trang nào.
    /// </summary>
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly INotificationService _store;

        public NotificationHub(INotificationService store)
        {
            _store = store;
        }

        // Client gọi khi mở chuông: đánh dấu tất cả thông báo của user là đã đọc
        public void MarkAllRead()
        {
            var idValue = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(idValue, out int userId))
                _store.MarkAllRead(userId);
        }
    }
}
