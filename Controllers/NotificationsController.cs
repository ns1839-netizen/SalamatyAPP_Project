using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Salamaty.API.Models.HomeModels;
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

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetNotifications(string userId)
        {
            var now = DateTime.Now;

            // 1. جلب الإشعارات الخاصة باليوزر (التي تم إنشاؤها له خصيصاً)
            var userNotifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .ToListAsync();

            // 2. جلب "كل" رسائل الترحيب العامة الموجهة لجميع المستخدمين
            var welcomeNotifications = await _context.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == "All" && n.Type == "Welcome")
                .ToListAsync(); // غيرنا دي عشان تجيب القائمة كاملة

            // 3. اختيار "نصيحة واحدة فقط" عشوائية لليوم (Awareness)
            var dailyTip = await _context.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == "All" && n.Type == "Awareness" && n.CreatedAt.Hour >= 10)
                .OrderBy(r => Guid.NewGuid())
                .FirstOrDefaultAsync();

            // 4. تجميع القائمة النهائية
            var finalData = new List<Notification>();

            finalData.AddRange(userNotifications);    // إضافة الخاص
            finalData.AddRange(welcomeNotifications); // إضافة كل الترحيب العام

            if (dailyTip != null)
                finalData.Add(dailyTip);              // إضافة نصيحة واحدة عشوائية

            // ترتيب العرض (الأحدث فوق)
            var result = finalData.OrderByDescending(n => n.CreatedAt).ToList();

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
