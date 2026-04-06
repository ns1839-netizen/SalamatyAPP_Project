using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalamatyAPI.Data;
using SalamatyAPI.Dtos.Products;
using SalamatyAPI.Dtos.Services;
using SalamatyAPI.Models.Enums;

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
        // أضفنا / في نهاية الـ baseUrl
        var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/";

        var query = _db.Products.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(p => p.Category == category);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search));

        var products = await query
            .Select(p => new ProductListDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                // هنا التعديل لضمان دمج الرابط صح
                ImageUrl = p.ImageUrl != null ? baseUrl + p.ImageUrl.TrimStart('/') : null,
                Category = p.Category
            })
            .ToListAsync();

        return Ok(products);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDetailsDto>> GetProductById(int id)
    {
        var p = await _db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();

        var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/";

        return Ok(new ProductDetailsDto
        {
            Id = p.Id,
            Name = p.Name,
            Price = p.Price,
            ImageUrl = p.ImageUrl != null ? baseUrl + p.ImageUrl.TrimStart('/') : null,
            Category = p.Category,
            Description = p.Description,
            SideEffects = p.SideEffects
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
            .Where(pa => pa.ProductId == id)
            .Include(pa => pa.AlternativeProduct)
            .Select(pa => new ProductAlternativeDto
            {
                Id = pa.AlternativeProduct.Id,
                Name = pa.AlternativeProduct.Name,
                Description = pa.AlternativeProduct.Description,
                ImageUrl = pa.AlternativeProduct.ImageUrl != null ? baseUrl + pa.AlternativeProduct.ImageUrl : null
            })
            .ToListAsync();

        return Ok(alternatives);
    }

    // --- الميثود التي تم تعديلها لحل مشكلة الأداء والـ Logs المتكررة ---
    [HttpGet("{id}/nearby-pharmacies")]
    public async Task<ActionResult<IEnumerable<NearbyPharmacyDto>>> GetNearbyPharmaciesForProduct(
     int id,
     [FromQuery] double? lat,
     [FromQuery] double? lng,
     [FromQuery] double maxDistanceKm = 10)
    {
        // 1. جلب بيانات المنتج الأساسية فقط (بدون Tracking للأداء)
        var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
            return NotFound("Product not found");

        if (string.IsNullOrWhiteSpace(product.Pharmacies))
            return Ok(new List<NearbyPharmacyDto>());

        var pharmacyCodes = product.Pharmacies
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(c => c.Trim())
            .ToList();

        // 2. جلب قائمة الصيدليات المطلوبة فقط في استعلام واحد (Single Query)
        var pharmaciesData = await _db.InsuranceNetworkServices
            .AsNoTracking()
            .Where(s => s.Type == InsuranceServiceType.Pharmacy &&
                        pharmacyCodes.Contains(s.Code))
            .ToListAsync();

        var now = DateTime.Now.TimeOfDay;
        bool hasLocation = lat.HasValue && lng.HasValue;

        // 3. معالجة البيانات في الذاكرة (Memory) لتجنب الـ N+1 Query
        var resultList = pharmaciesData.Select(s =>
        {
            double distanceKm = 0;
            if (hasLocation)
                distanceKm = CalculateDistanceKm(lat!.Value, lng!.Value, s.Latitude, s.Longitude);

            string openStatusText;
            bool isOpenNow;

            if (s.OpenFrom == TimeSpan.Zero && s.OpenTo == TimeSpan.Zero)
            {
                openStatusText = "Open 24 Hours";
                isOpenNow = true;
            }
            else if (s.OpenFrom <= now && now <= s.OpenTo)
            {
                openStatusText = $"Open until {TimeOnly.FromTimeSpan(s.OpenTo):h tt}";
                isOpenNow = true;
            }
            else
            {
                openStatusText = "Closed";
                isOpenNow = false;
            }

            return new NearbyPharmacyDto
            {
                Id = s.Id,
                Name = s.Name,
                Type = "Pharmacy",
                Address = s.Address,
                Phone = s.Phone,
                DistanceKm = Math.Round(distanceKm, 2), // تقريب المسافة لرقمين
                DistanceText = hasLocation ? $"{distanceKm:F1} KM away" : "",
                OpenStatusText = openStatusText,
                IsOpenNow = isOpenNow,
                Latitude = s.Latitude,
                Longitude = s.Longitude
            };
        }).ToList();

        // 4. الفلترة والترتيب النهائي
        if (lat.HasValue)
        {
            resultList = resultList
                .Where(p => p.DistanceKm <= maxDistanceKm)
                .OrderBy(p => p.DistanceKm)
                .ToList();
        }
        else
        {
            resultList = resultList.OrderBy(p => p.Name).ToList();
        }

        return Ok(resultList);
    }

    // ---- helpers ----

    private static double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371.0;
        double dLat = DegreesToRadians(lat2 - lat1);
        double dLon = DegreesToRadians(lon2 - lon1);

        double a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(DegreesToRadians(lat1)) *
            Math.Cos(DegreesToRadians(lat2)) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double DegreesToRadians(double deg) => deg * (Math.PI / 180.0);
}