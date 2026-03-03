using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalamatyAPI.Data;

namespace Salamaty.API.Controllers
{
    public enum FacilityType { All, Pharmacy, Hospital, Lab }

    [Route("api/[controller]")]
    [ApiController]
    public class FacilitiesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public FacilitiesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ================== GET ALL FACILITIES (With Search & Filter) ==================
        [HttpGet("all-facilities")]
        public async Task<IActionResult> GetAllFacilities(
            [FromQuery] double userLat,
            [FromQuery] double userLon,
            [FromQuery] FacilityType type = FacilityType.All,
            [FromQuery] string? searchTerm = "")
        {
            var query = _context.Facilities.AsQueryable();

            // 1. الفلترة حسب النوع
            if (type != FacilityType.All)
            {
                string typeString = type.ToString();
                query = query.Where(f => f.Type == typeString);
            }

            // 2. السيرش الشامل (الاسم، العنوان، أو المحافظة)
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(f => f.Name.Contains(searchTerm) ||
                                         f.Address.Contains(searchTerm) ||
                                         f.Governorate.Contains(searchTerm));
            }

            var facilities = await query.ToListAsync();

            // 3. حساب المسافة وتوليد الرابط والترتيب
            var result = facilities.Select(f => new
            {
                f.Id,
                f.Name,
                f.Type,
                f.Address,
                f.PhoneNumber,
                f.OperatingHours,
                f.Governorate,
                Distance = Math.Round(CalculateDistance(userLat, userLon, f.Latitude, f.Longitude), 1),
                // رابط جوجل ماب الصحيح باستخدام الإحداثيات
                LocationUrl = $"https://www.google.com/maps/search/?api=1&query={f.Latitude},{f.Longitude}"
            })
            .OrderBy(f => f.Distance)
            .ToList();

            return Ok(new { success = true, data = result });
        }

        // ================== GET TOP 3 NEARBY (For Home Page) ==================
        [HttpGet("nearby-top3")]
        public async Task<IActionResult> GetTop3Nearby([FromQuery] double userLat, [FromQuery] double userLon)
        {
            // 1. سحب البيانات من الداتابيز
            var facilities = await _context.Facilities.ToListAsync();

            // 2. حساب المسافة والترتيب واختيار أول 3 فقط
            var top3 = facilities.Select(f => new
            {
                f.Id,
                f.Name,
                f.Type,
                f.Address,
                f.PhoneNumber,
                f.OperatingHours,
                f.Governorate,
                // حساب المسافة بالكيلومتر وتقريبها
                Distance = Math.Round(CalculateDistance(userLat, userLon, f.Latitude, f.Longitude), 1),
                // رابط الخريطة الذكي
                LocationUrl = $"https://www.google.com/maps/search/?api=1&query={f.Latitude},{f.Longitude}"
            })
            .OrderBy(f => f.Distance) // الأقرب أولاً
            .Take(3) // ليميت 3 صفوف بس
            .ToList();

            return Ok(new { success = true, data = top3 });
        }

        // ================== HELPER: CALCULATE DISTANCE (Haversine) ==================
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371; // نصف قطر الأرض بالكيلومتر
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