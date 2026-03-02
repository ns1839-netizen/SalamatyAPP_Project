using Microsoft.EntityFrameworkCore;
using SalamatyAPI.Data;

namespace Salamaty.API.Services
{
    public class NotificationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        public NotificationBackgroundService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                // تحديد وقت التنفيذ (الساعة 10 صباحاً)
                var nextRunTime = new DateTime(now.Year, now.Month, now.Day, 10, 0, 0);

                // لو الوقت الحالي أعدى الساعة 10، يبقى المرة الجاية بكرة
                if (now > nextRunTime)
                    nextRunTime = nextRunTime.AddDays(1);

                var delay = nextRunTime - now;

                // لغرض التجربة حالاً (لو عايزة تشوفيها شغالة دلوقتي):
                // delay = TimeSpan.FromSeconds(10); 

                await Task.Delay(delay, stoppingToken);

                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    // 1. تصفير أي إشعار قديم كان واخد لقب "إشعار اليوم" 
                    // (اختياري: لو عايزة تضمنين إن مفيش غير واحد بس اللي تاريخه النهاردة)

                    // 2. اختيار إشعار عشوائي من الـ Templates اللي عندنا
                    var randomAwareness = await context.Notifications
                        .Where(n => n.Type == "Awareness")
                        .OrderBy(r => Guid.NewGuid())
                        .FirstOrDefaultAsync();

                    if (randomAwareness != null)
                    {
                        // 3. تحديث تاريخه للحظة الحالية (اللي هي 10 صباحاً) 
                        // وتأكيد إن الـ UserId بتاعه "All" عشان يظهر للكل
                        randomAwareness.CreatedAt = DateTime.Now;
                        randomAwareness.UserId = "All";

                        await context.SaveChangesAsync();
                    }
                }
            }
        }
    }
}