using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalamatyAPI.Data;
using SalamatyAPI.Dtos.Services;
using SalamatyAPI.Models.Enums;

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

        // GET: api/services/insurance-providers/1/nearby?lat=..&lng=..&radius=10&type=Hospital&openNow=true
        [HttpGet("insurance-providers/{providerId}/nearby")]
        public async Task<ActionResult<IEnumerable<NearbyServiceDto>>> GetNearbyServices(
       int providerId,
       [FromQuery] double? lat,
       [FromQuery] double? lng,
       [FromQuery] double radius = 10,
       [FromQuery] InsuranceServiceType type = InsuranceServiceType.All,
       [FromQuery] bool openNow = false)
        {
            var query = _db.InsuranceNetworkServices
                .Where(s => s.InsuranceProviderId == providerId);

            if (type != InsuranceServiceType.All)
                query = query.Where(s => s.Type == type);

            var services = await query.ToListAsync();
            var nowTime = DateTime.Now.TimeOfDay;
            bool hasLocation = lat.HasValue && lng.HasValue;

            var result = services
                .Select(s =>
                {
                    double distance = 0;

                    // Only calculate distance if user sent lat/lng
                    if (lat.HasValue && lng.HasValue)
                    {
                        // NO WARNINGS HERE! The compiler now knows lat and lng are safe to use.
                        distance = HaversineDistance(lat.Value, lng.Value, s.Latitude, s.Longitude);
                        if (distance > radius)
                            return null; // outside radius
                    }

                    bool isOpen = nowTime >= s.OpenFrom && nowTime <= s.OpenTo;
                    if (openNow && !isOpen)
                        return null;

                    return new NearbyServiceDto
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Type = s.Type.ToString(),
                        Address = s.Address,
                        Latitude = s.Latitude,
                        Longitude = s.Longitude,
                        DistanceKm = distance, // Math.Round(distance, 2)
                        Status = isOpen ? "open" : "closed",
                        OpenUntil = s.OpenTo.ToString(@"hh\:mm")
                    };
                })
                .Where(x => x != null)!;

            // If we have location, order by distance; otherwise any reasonable order
            result = hasLocation
                ? result.OrderBy(x => x!.DistanceKm)
                : result.OrderBy(x => x!.Name);

            return Ok(result.ToList());
        }

        private static double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // km
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private static double ToRadians(double angle) => Math.PI * angle / 180.0;
    }
}
