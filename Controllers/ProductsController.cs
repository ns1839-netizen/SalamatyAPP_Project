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

    // GET /api/products?category=GSL&search=ibuprofen
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductListDto>>> GetProducts(
        [FromQuery] string? category,
        [FromQuery] string? search)
    {
        var query = _db.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(p => p.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(search));
        }

        var products = await query
            .Select(p => new ProductListDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                ImageUrl = p.ImageUrl,
                Category = p.Category
            })
            .ToListAsync();

        return Ok(products);
    }

    // GET /api/products/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDetailsDto>> GetProductById(int id)
    {
        var p = await _db.Products.FindAsync(id);
        if (p == null) return NotFound();

        var dto = new ProductDetailsDto
        {
            Id = p.Id,
            Name = p.Name,
            Price = p.Price,
            ImageUrl = p.ImageUrl,
            Category = p.Category,
            Description = p.Description,
            SideEffects = p.SideEffects
        };

        return Ok(dto);
    }

    // GET /api/products/{id}/alternatives
    [HttpGet("{id:int}/alternatives")]
    public async Task<ActionResult<IEnumerable<ProductAlternativeDto>>> GetAlternatives(int id)
    {
        var exists = await _db.Products.AnyAsync(p => p.Id == id);
        if (!exists) return NotFound();

        var alternatives = await _db.ProductAlternatives
            .Where(pa => pa.ProductId == id)
            .Include(pa => pa.AlternativeProduct)
            .Select(pa => new ProductAlternativeDto
            {
                Id = pa.AlternativeProduct.Id,
                Name = pa.AlternativeProduct.Name,
                Description = pa.AlternativeProduct.Description,
                ImageUrl = pa.AlternativeProduct.ImageUrl
            })
            .ToListAsync();

        return Ok(alternatives);
    }

    [HttpGet("{id}/nearby-pharmacies")]
    public async Task<ActionResult<IEnumerable<NearbyPharmacyDto>>> GetNearbyPharmaciesForProduct(
     int id,
     [FromQuery] double? lat,
     [FromQuery] double? lng,
     [FromQuery] double maxDistanceKm = 10)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null)
            return NotFound("Product not found");

        if (string.IsNullOrWhiteSpace(product.Pharmacies))
            return Ok(new List<NearbyPharmacyDto>());

        var pharmacyCodes = product.Pharmacies
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(c => c.Trim())
            .ToList();

        var pharmacies = await _db.InsuranceNetworkServices
            .Where(s => s.Type == InsuranceServiceType.Pharmacy &&
                        pharmacyCodes.Contains(s.Code))
            .ToListAsync();

        var now = DateTime.Now.TimeOfDay;
        bool hasLocation = lat.HasValue && lng.HasValue;

        var resultQuery = pharmacies.Select(s =>
        {
            double distanceKm = 0;

            if (hasLocation)
                distanceKm = CalculateDistanceKm(lat.Value, lng.Value, s.Latitude, s.Longitude);

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
                DistanceKm = distanceKm,
                DistanceText = hasLocation ? $"{distanceKm:F1} KM away" : "",
                OpenStatusText = openStatusText,
                IsOpenNow = isOpenNow,
                Latitude = s.Latitude,
                Longitude = s.Longitude
            };
        });

        if (hasLocation)
            resultQuery = resultQuery.Where(p => p.DistanceKm <= maxDistanceKm)
                                     .OrderBy(p => p.DistanceKm);
        else
            resultQuery = resultQuery.OrderBy(p => p.Name);

        var result = resultQuery.ToList();
        return Ok(result);
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

