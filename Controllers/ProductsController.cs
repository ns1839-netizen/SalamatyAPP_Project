using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalamatyAPI.Data;
using SalamatyAPI.Dtos.Products;
using SalamatyAPI.Dtos.Services;

namespace SalamatyAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ProductsController(ApplicationDbContext db)
    {
        _db = db;
    }



    // GET /api/products
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductListDto>>> GetProducts(
        [FromQuery] string? category,
        [FromQuery] string? search)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
        var query = _db.Products.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(p => p.Category != null && p.Category == category);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name != null && p.Name.Contains(search));

        var products = await query
            .Select(p => new ProductListDto
            {
                Id = p.Id,
                Name = p.Name ?? "",
                Price = p.Price ?? 0m, // حماية من null
                ImageUrl = !string.IsNullOrEmpty(p.ImageUrl)
                     ? baseUrl + "/" + p.ImageUrl.TrimStart('/')
                    : null,
                Category = p.Category ?? ""
            })
            .ToListAsync();

        return Ok(products);
    }

    // GET /api/products/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDetailsDto>> GetProductById(int id)
    {
        var p = await _db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();

        var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";

        return Ok(new ProductDetailsDto
        {
            Id = p.Id,
            Name = p.Name ?? "",
            Price = p.Price ?? 0m,
            ImageUrl = !string.IsNullOrEmpty(p.ImageUrl)
                ? baseUrl + "/" + p.ImageUrl.TrimStart('/')
                : null,
            Category = p.Category ?? "",
            Description = p.Description ?? "",
            SideEffects = p.SideEffects ?? ""
        });
    }

    // GET /api/products/{id}/alternatives
    [HttpGet("{id:int}/alternatives")]
    public async Task<ActionResult<IEnumerable<ProductAlternativeDto>>> GetAlternatives(int id)
    {
        var exists = await _db.Products.AnyAsync(p => p.Id == id);
        if (!exists) return NotFound();

        var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";

        var alternatives = await _db.ProductAlternatives
            .AsNoTracking()
            .Include(pa => pa.AlternativeProduct)
            .Where(pa => pa.ProductId == id && pa.AlternativeProduct != null) // 🔥 fix
            .Select(pa => new ProductAlternativeDto
            {
                Id = pa.AlternativeProduct!.Id,
                Name = pa.AlternativeProduct.Name ?? "",
                Description = pa.AlternativeProduct.Description ?? "",
                SideEffect = pa.AlternativeProduct.SideEffects ?? "",
                ImageUrl = !string.IsNullOrEmpty(pa.AlternativeProduct.ImageUrl)
                    ? baseUrl + "/" + pa.AlternativeProduct.ImageUrl.TrimStart('/')
                    : null
            })
            .ToListAsync();

        return Ok(alternatives);
    }

    // GET /api/products/{id}/nearby-pharmacies[HttpGet("{id}/nearby-pharmacies")]
    // GET /api/products/{id}/nearby-pharmacies
    [HttpGet("{id:int}/nearby-pharmacies")]
    public async Task<ActionResult<IEnumerable<NearbyPharmacyDto>>> GetNearbyPharmaciesForProduct(
        int id,
        [FromQuery] double? lat, [FromQuery] double? lng,
        [FromQuery] double maxDistanceKm = 10)
    {
        var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound("Product not found");

        if (string.IsNullOrWhiteSpace(product.Pharmacies))
            return Ok(new List<NearbyPharmacyDto>());

        // Safely split the pharmacy codes (rewritten to avoid the code-breaking glitch!)
        var pharmacyCodes = product.Pharmacies
            .Split(',', ';')
            .Select(c => c.Trim())
            .Where(c => !string.IsNullOrEmpty(c))
            .ToList();

        var pharmaciesData = await _db.InsuranceNetworkServices
            .AsNoTracking()
            .Where(s =>
                !string.IsNullOrEmpty(s.Code) &&
                !string.IsNullOrEmpty(s.Type) &&
                (s.Type == "pharmacy" || s.Type == "pharmacies") &&
                pharmacyCodes.Contains(s.Code))
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Type,
                s.Address,
                s.Phone,
                s.Latitude,
                s.Longitude,
                s.OpenFrom,
                s.OpenTo
            })
            .ToListAsync();

        var now = DateTime.Now.TimeOfDay;
        bool hasUserLocation = lat.HasValue && lng.HasValue;

        var resultList = pharmaciesData.Select(s =>
        {
            double distanceKm = 9999;
            string locUrl = "";

            // ✅ 1. Calculate Distance & Generate Google Maps URL
            if (s.Latitude.HasValue && s.Longitude.HasValue)
            {
                if (hasUserLocation)
                {
                    distanceKm = CalculateDistanceKm(lat!.Value, lng!.Value, s.Latitude.Value, s.Longitude.Value);
                }

                // FIXED: Combine Name and Address/Area to search Google Maps properly!
                // Example result: "Al Bustan Pharmacy, 16 Al Bustan Street, Abdeen"
                string pharmacyName = s.Name ?? "";
                string pharmacyAddress = s.Address ?? "";

                // Combine them into one search text
                string searchText = $"{pharmacyName}, {pharmacyAddress}";

                // Encode it for a URL (changes spaces to %20, etc.)
                string encodedQuery = Uri.EscapeDataString(searchText);

                locUrl = $"https://www.google.com/maps/search/?api=1&query={encodedQuery}";
            }

            // ✅ 2. Handle the Open/Closed Status (Now ALWAYS Open 24 Hours)
            string openStatusText = "Open 24 Hours";
            bool isOpenNow = true;

            // ✅ 3. Return the mapped DTO
            return new NearbyPharmacyDto
            {
                Id = s.Id,
                Name = s.Name ?? "",
                Type = s.Type ?? "Pharmacy",
                Address = s.Address ?? "",
                Phone = s.Phone ?? "",
                DistanceKm = Math.Round(distanceKm, 2),
                DistanceText = (hasUserLocation && distanceKm < 999)
                    ? $"{distanceKm:F1} KM away"
                    : "",
                OpenStatusText = openStatusText, // Will always be "Open 24 Hours"
                IsOpenNow = isOpenNow,           // Will always be true
                Latitude = s.Latitude ?? 0,
                Longitude = s.Longitude ?? 0,
                LocationUrl = locUrl             // Will drop an exact pin!
            };
        })
        .Where(p => !hasUserLocation || p.DistanceKm <= maxDistanceKm)
        .OrderBy(p => hasUserLocation ? p.DistanceKm : 0)
        .ThenBy(p => p.Name)
        .ToList();

        return Ok(resultList);
    }

    // --- Distance Helper Methods ---

    private static double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371.0;
        double dLat = DegreesToRadians(lat2 - lat1);
        double dLon = DegreesToRadians(lon2 - lon1);

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(DegreesToRadians(lat1)) *
                   Math.Cos(DegreesToRadians(lat2)) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double DegreesToRadians(double deg) => deg * (Math.PI / 180.0);
}