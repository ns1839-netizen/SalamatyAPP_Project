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

                // حساب المسافة بالكيلومتر الحقيقي
                Distance = (userLat.HasValue && userLng.HasValue)
    ? CalculateHaversine(userLat.Value, userLng.Value, p.Latitude, p.Longitude)
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


        private double CalculateHaversine(double lat1, double lon1, double lat2, double lon2)
        {
            double R = 6371; // نصف قطر الأرض بالكيلومتر
            double dLat = (lat2 - lat1) * (Math.PI / 180);
            double dLon = (lon2 - lon1) * (Math.PI / 180);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1 * (Math.PI / 180)) * Math.Cos(lat2 * (Math.PI / 180)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return Math.Round(R * c, 2); // الناتج بالكيلومتر ومقرب لرقمين عشريين
        }

    }
}