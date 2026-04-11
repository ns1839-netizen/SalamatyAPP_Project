using Microsoft.EntityFrameworkCore;
using Salamaty.API.Models.HomeModels;
using SalamatyAPI.Data;

namespace Salamaty.API.Services.HomeServices
{
    public class HomeService : IHomeService
    {
        private readonly ApplicationDbContext _context;

        public HomeService(ApplicationDbContext context) => _context = context;

        public async Task<List<Banner>> GetBannersAsync() => await _context.Banners.ToListAsync();
        public async Task<List<MedicalProvider>> GetAllProvidersAsync() => await _context.MedicalProviders.ToListAsync();

        public async Task<List<object>> FilterProvidersAsync(string? governorate, string? specialty, string? searchTerm, double? userLat, double? userLng)
        {
            // 1. يفضل الفلترة تبدأ من الـ Context مباشرة مش ToList الأول عشان السرعة
            var query = _context.MedicalProviders.AsQueryable();

            // 2. فلترة المحافظة (تجاهل حالة الحروف والتعامل مع All)
            if (!string.IsNullOrEmpty(governorate) && governorate != "All")
            {
                query = query.Where(p => p.Governorate.ToLower().Contains(governorate.ToLower()));
            }

            // 3. فلترة التخصص
            if (!string.IsNullOrEmpty(specialty) && specialty != "All")
            {
                query = query.Where(p => p.Specialty.ToLower().Contains(specialty.ToLower()));
            }

            // 4. السيرش الذكي (Case-Insensitive & Multi-Column)
            // بيبحث في الاسم والمحافظة والتخصص مع بعض
            if (!string.IsNullOrEmpty(searchTerm))
            {
                var term = searchTerm.ToLower();
                query = query.Where(p => p.ProviderName.ToLower().Contains(term) ||
                                         p.Governorate.ToLower().Contains(term) ||
                                         p.Specialty.ToLower().Contains(term));
            }

            var providers = await query.ToListAsync();

            // 5. حساب المسافة الجغرافية الحقيقية (Haversine) والترتيب
            var result = providers.Select(p => new
            {
                p.Id,
                p.ProviderName,
                p.Specialty,
                p.Governorate,
                WorkingHours = p.WorkingHours ?? "24 Hours",
                Phone = (p.Phone ?? "").Replace(" ", "").Replace("-", ""),

                // رابط جوجل ماب المعتمد
                LocationUrl = (p.Latitude != 0)
                              ? $"https://www.google.com/maps/search/?api=1&query={Uri.EscapeDataString(p.ProviderName)}+{p.Latitude},{p.Longitude}"
                              : "https://www.google.com/maps",

                // استخدام معادلة Haversine بدلاً من Math.Sqrt لضمان دقة الكيلومترات
                Distance = (userLat.HasValue && userLng.HasValue)
                           ? Math.Round(CalculateDistance(userLat.Value, userLng.Value, p.Latitude, p.Longitude), 1)
                           : 0
            })
            .OrderBy(p => p.Distance)
            .ToList<object>();

            return result;
        }

        // ضيفي الميثود دي تحتها في نفس الملف لحساب الكيلومترات بدقة
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371; // كيلومتر
            var dLat = (lat2 - lat1) * Math.PI / 180;
            var dLon = (lon2 - lon1) * Math.PI / 180;
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }





    }
}