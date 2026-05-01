using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mscc.GenerativeAI.Types;
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
        [HttpGet("insurance-providers/{providerId}/nearby")]
        public async Task<ActionResult> GetNearbyServices(
            int providerId,
            [FromQuery] double? lat,
            [FromQuery] double? lng, [FromQuery] double radius = 10,
            [FromQuery] InsuranceServiceType type = InsuranceServiceType.All,
            [FromQuery] bool openNow = false)
        {
            try
            {
                var query = _db.InsuranceNetworkServices
                    .Where(s => s.InsuranceProviderId == providerId);

                // Filter by Lab, Pharmacy, or Hospital
                if (type != InsuranceServiceType.All)
                {
                    string selectedType = type.ToString().ToLower();
                    query = query.Where(s => s.Type != null &&
                                             s.Type.ToLower().Contains(selectedType));
                }

                var services = await query.ToListAsync();

                var nowTime = DateTime.Now.TimeOfDay;
                bool hasLocation = lat.HasValue && lng.HasValue;

                var resultList = services
                    .Select(s =>
                    {
                        double distance = 0;
                        string locUrl = "";

                        // Calculate Distance and Create Google Maps Location URL
                        if (s.Latitude.HasValue && s.Longitude.HasValue)
                        {
                            if (hasLocation)
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

                            // ✅ FIXED: Combine Name and Address for perfect Google Maps search
                            string facilityName = s.Name ?? "";
                            string facilityAddress = s.Address ?? "";
                            string searchText = $"{facilityName}, {facilityAddress}";

                            string encodedQuery = Uri.EscapeDataString(searchText);
                            locUrl = $"https://www.google.com/maps/search/?api=1&query={encodedQuery}";
                        }
                        else if (hasLocation)
                        {
                            return null;
                        }

                        // ✅ NEW LOGIC: Handle Open/Closed Status based on Facility Type
                        bool isOpen = false;
                        string openUntilStr = "";
                        string safeType = (s.Type ?? "").ToLower();

                        if (safeType.Contains("pharmacy") || safeType.Contains("hospital") || safeType.Contains("pharmacies"))
                        {
                            // 1. Hospitals and Pharmacies are open 24 Hours
                            isOpen = true;
                            openUntilStr = "Open 24 Hours";
                        }
                        else if (safeType.Contains("lab") || safeType.Contains("analysis"))
                        {
                            // 2. Labs (Assume they open at 8:00 AM and close at 11:00 PM)
                            TimeSpan labOpenTime = new TimeSpan(8, 0, 0);   // 8:00 AM
                            TimeSpan labCloseTime = new TimeSpan(23, 0, 0); // 11:00 PM

                            // It is only open if current time is BETWEEN 8 AM and 11 PM
                            isOpen = nowTime >= labOpenTime && nowTime <= labCloseTime;

                            // If it's open, show text. If it's closed, say "Closed"
                            openUntilStr = isOpen ? "Open until 11 PM" : "Closed";
                        }
                        else
                        {

                            // 2. Labs (Assume they open at 8:00 AM and close at 11:00 PM)
                            TimeSpan otherOpenTime = new TimeSpan(9, 0, 0);   // 8:00 AM
                            TimeSpan otherCloseTime = new TimeSpan(22, 0, 0);// 11:00 PM

                            // It is only open if current time is BETWEEN 8 AM and 11 PM
                            isOpen = nowTime >= otherOpenTime && nowTime <= otherCloseTime;

                            // If it's open, show text. If it's closed, say "Closed"
                            openUntilStr = isOpen ? "Open until 10 PM" : "Closed";
                         
                        }

                        // Filter out closed places if the user checked the "openNow" box
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
                            Status = isOpen ? "open" : "closed", // Dynamically set to open/closed
                            OpenUntil = openUntilStr,            // Assigns our custom strings
                            LocationUrl = locUrl // ✅ Pass the created Google Maps link!
                        };
                    })
                    .Where(x => x != null)
                    .ToList();

                // Sort Results
                var finalResult = hasLocation
                    ? resultList.OrderBy(x => x.DistanceKm).ToList()
                    : resultList.OrderBy(x => x.Name).ToList();

                return Ok(new
                {
                    success = true,
                    data = finalResult
                });
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