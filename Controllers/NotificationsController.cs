using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalamatyAPI.Data;

namespace Salamaty.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public NotificationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. الحصول على قائمة الإشعارات (الخاصة باليوزر + العامة لجميع المستخدمين)
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetNotifications(string userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId || n.UserId == "All") // جلب الخاص والعام
                .OrderByDescending(n => n.CreatedAt) // عرض الأحدث أولاً
                .ToListAsync();

            return Ok(new { success = true, data = notifications });
        }

        // 2. تحديث حالة الإشعار لتصبح "مقروء" (عند ضغط المستخدم عليه)
        [HttpPatch("mark-as-read/{id}")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);

            if (notification == null)
                return NotFound(new { success = false, message = "Notification not found" });

            notification.IsRead = true; // تغيير الحالة لمقروء
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Notification marked as read" });
        }
    }
}