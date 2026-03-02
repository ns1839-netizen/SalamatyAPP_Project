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
            var providers = await _context.MedicalProviders.ToListAsync();
            var query = providers.AsQueryable();

            if (!string.IsNullOrEmpty(governorate))
                query = query.Where(p => NormalizeArabic(p.Governorate).Contains(NormalizeArabic(governorate)));

            if (!string.IsNullOrEmpty(specialty))
                query = query.Where(p => NormalizeArabic(p.Specialty).Contains(NormalizeArabic(specialty)));

            if (!string.IsNullOrEmpty(searchTerm))
                query = query.Where(p => NormalizeArabic(p.ProviderName).Contains(NormalizeArabic(searchTerm)));

            // حساب المسافة والترتيب
            var sortedList = query.ToList().Select(p => new
            {
                p.Id,
                p.ProviderName,
                p.Specialty,
                p.Governorate,
                p.WorkingHours,
                Phone = (p.Phone ?? "").Replace(" ", "").Replace("-", ""),

                // اللينك الذكي اللي بيظهر الاسم والإحداثيات 👇
                // الصيغة دي بتدمج اسم المستشفى مع الإحداثيات لضمان ظهور النقطة الحمراء واسم المكان
                LocationUrl = $"https://www.google.com/maps/search/?api=1&query={Uri.EscapeDataString(p.ProviderName)}+{p.Latitude},{p.Longitude}",

                Distance = (userLat.HasValue && userLng.HasValue)
                    ? Math.Sqrt(Math.Pow(p.Latitude - userLat.Value, 2) + Math.Pow(p.Longitude - userLng.Value, 2))
                    : 0
            })
            .OrderBy(p => p.Distance)
            .ToList<object>();

            return sortedList;
        }

        private string NormalizeArabic(string? text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace("أ", "ا").Replace("إ", "ا").Replace("آ", "ا").Replace("ة", "ه").Replace("ى", "ي").Trim();
        }




    }
}