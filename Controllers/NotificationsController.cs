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

        // 1. الحصول على قائمة الإشعارات (الخاصة باليوزر + العامة المسموح بظهورها)
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetNotifications(string userId)
        {
            var now = DateTime.Now;

            // 1. جلب إشعارات اليوزر الخاصة (زي الـ Welcome اللي ظهرلك رقمه 51)
            var userNotifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .ToListAsync();

            // 2. جلب إشعار واحد "عشوائي" من الإشعارات العامة (UserId == "All")
            var dailyAwareness = await _context.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == "All" && n.CreatedAt.Hour <= 20) // شرط الساعة 10 صباحاً
                .OrderBy(r => Guid.NewGuid()) // <--- السطر ده بيعمل "لخبطة" عشوائية للبيانات
                .Take(2) // <--- السطر ده بياخد أول واحد بس طلع في اللخبطة دي (بطل اليوم)
                .FirstOrDefaultAsync();

            // 3. دمج إشعارات اليوزر مع الإشعار العشوائي المختار
            if (dailyAwareness != null)
            {
                userNotifications.Add(dailyAwareness);
            }

            // ترتيب النتيجة النهائية: الأحدث يظهر فوق
            var result = userNotifications.OrderByDescending(n => n.CreatedAt).ToList();

            return Ok(new { success = true, data = result });
        }

        // 2. تحديث حالة الإشعار لتصبح "مقروء"
        [HttpPatch("mark-as-read/{id}")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);

            if (notification == null)
                return NotFound(new { success = false, message = "Notification not found" });

            // لو الإشعار مقروء أصلاً ملوش لزمة نحدث قاعدة البيانات
            if (notification.IsRead) return Ok(new { success = true });

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Notification marked as read" });
        }
    }
}