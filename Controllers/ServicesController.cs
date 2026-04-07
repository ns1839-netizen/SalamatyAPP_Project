using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalamatyAPI.Data;
using SalamatyAPI.Dtos.Services;

namespace SalamatyAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServicesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public ServicesController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet("insurance-providers/{providerId}/nearby")]
        public async Task<ActionResult<IEnumerable<NearbyServiceDto>>> GetNearbyServices(
            int providerId,
            [FromQuery] double? lat,
            [FromQuery] double? lng,
            [FromQuery] double radius = 10,
            [FromQuery] string type = "All",
            [FromQuery] bool openNow = false)
        {
            try
            {
                var query = _db.InsuranceNetworkServices
                    .Where(s => s.InsuranceProviderId == providerId);

                // ✅ حماية من null في Type
                if (!string.Equals(type, "All", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(s => s.Type != null &&
                                             s.Type.ToLower().Contains(type.ToLower()));
                }

                var services = await query.ToListAsync();

                var nowTime = DateTime.Now.TimeOfDay;
                bool hasLocation = lat.HasValue && lng.HasValue;

                var resultList = services
                    .Select(s =>
                    {
                        double distance = 0;

                        // ✅ حساب المسافة
                        if (hasLocation)
                        {
                            if (s.Latitude.HasValue && s.Longitude.HasValue)
                            {
                                distance = HaversineDistance(
                                    lat.Value,
                                    lng.Value,
                                    s.Latitude.Value,
                                    s.Longitude.Value
                                );

                                if (distance > radius)
                                    return null;
                            }
                            else
                            {
                                return null;
                            }
                        }

                        // ✅ حماية من null في OpenFrom / OpenTo
                        bool isOpen = false;

                        if (s.OpenFrom.HasValue && s.OpenTo.HasValue)
                        {
                            isOpen = nowTime >= s.OpenFrom.Value &&
                                     nowTime <= s.OpenTo.Value;
                        }

                        if (openNow && !isOpen)
                            return null;

                        return new NearbyServiceDto
                        {
                            Id = s.Id,
                            Name = s.Name ?? "",
                            Type = s.Type ?? "",
                            Address = s.Address ?? "",
                            Latitude = s.Latitude ?? 0,
                            Longitude = s.Longitude ?? 0,
                            DistanceKm = Math.Round(distance, 2),
                            Status = isOpen ? "open" : "closed",
                            OpenUntil = s.OpenTo.HasValue
                                ? s.OpenTo.Value.ToString(@"hh\:mm")
                                : ""
                        };
                    })
                    .Where(x => x != null)
                    .ToList();

                // ✅ ترتيب النتائج
                var finalResult = hasLocation
                    ? resultList.OrderBy(x => x.DistanceKm)
                    : resultList.OrderBy(x => x.Name);

                return Ok(finalResult);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error",
                    details = ex.Message
                });
            }
        }

        private static double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371;

            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }

        private static double ToRadians(double angle)
        {
            return Math.PI * angle / 180.0;
        }
    }
}