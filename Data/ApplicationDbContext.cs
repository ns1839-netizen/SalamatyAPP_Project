using System.Globalization;
using CsvHelper; // يفضل تثبيت باقة CsvHelper من Nuget
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Salamaty.API.Models.HomeModels;
using Salamaty.API.Models.ProfileModels;

namespace SalamatyAPI.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Banner> Banners { get; set; }
        public DbSet<MedicalProvider> MedicalProviders { get; set; }
        public DbSet<Notification> Notifications { get; set; } // إضافة جدول الإشعارات

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- كود الـ Seeding للإشعارات من ملف CSV ---

            var notifications = new List<Notification>();
            // تأكدي من وضع ملف الـ CSV في مسار معروف، مثلاً داخل فولدر DataResources
            var csvPath = Path.Combine(Directory.GetCurrentDirectory(), "DataResources", "Notifacations.csv");

            if (File.Exists(csvPath))
            {
                using (var reader = new StreamReader(csvPath))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    var records = csv.GetRecords<dynamic>();
                    foreach (var record in records)
                    {
                        notifications.Add(new Notification
                        {
                            Id = int.Parse(record.Id),
                            Type = record.Type,
                            Title = record.Title,
                            Message = record.Message,
                            TargetSpecialty = record.TargetSpecialty,
                            CreatedAt = DateTime.Now, // يتم التوليد لحظة الرفع
                            IsRead = false, // القيمة الافتراضية
                            UserId = "All" // "All" تعني أن الإشعار عام لكل المستخدمين
                        });
                    }
                }

                // إخبار EF Core بحقن هذه البيانات في الداتابيز
                modelBuilder.Entity<Notification>().HasData(notifications);
            }
        }
    }
}