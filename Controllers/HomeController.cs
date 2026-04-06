using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;
using Salamaty.API.Models.HomeModels;
using Salamaty.API.Services.HomeServices;
using SalamatyAPI.Data;

namespace Salamaty.API.Controllers
{
    [ApiController]
    [Route("api/home")]
    public class HomeController : ControllerBase
    {
        private readonly IHomeService _homeService;
        private readonly ApplicationDbContext _context;

        public HomeController(IHomeService homeService, ApplicationDbContext context)
        {
            _homeService = homeService;
            _context = context;
        }

        [HttpGet("specialties-providers")]
        public async Task<IActionResult> GetProviders(
            [FromQuery] string? governorate,
            [FromQuery] string? specialty,
            [FromQuery] string? search,
            [FromQuery] double? lat,
            [FromQuery] double? lng)
        {
            var result = await _homeService.FilterProvidersAsync(governorate, specialty, search, lat, lng);
            return Ok(result);
        }

        [HttpGet("Tips")]
        public async Task<IActionResult> GetBanners()
        {
            // 1. بنجيب عنوان السيرفر الحالي (مثلاً https://localhost:7140)
            var request = HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";

            // 2. بنسحب الـ 20 سطر من الداتابيز
            var banners = await _context.Banners.AsNoTracking().ToListAsync();

            // 3. بنعدل الـ ImageUrl عشان يروح للموبايل كـ لينك كامل
            var result = banners.Select(b => new
            {
                b.Id,
                b.Title,
                b.Summary,
                // بنشيل الـ backslash لو موجودة ونركب الـ URL
                ImageUrl = $"{baseUrl}/{b.ImageUrl.Replace("\\", "/")}",
                b.DetailsUrl
            });

            return Ok(new { success = true, data = result });
        }

        [HttpPost("upload-csv")]
        public async Task<IActionResult> UploadCsv(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("File is empty");

            var facilities = new List<Facility>();

            using (var parser = new TextFieldParser(file.OpenReadStream()))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(","); // الفاصلة هي الفاصل
                parser.HasFieldsEnclosedInQuotes = true; // دي اللي هتحل مشكلة الفاصلات جوه الكلام

                // تخطي الهيدر
                if (!parser.EndOfData) parser.ReadFields();

                while (!parser.EndOfData)
                {
                    var fields = parser.ReadFields();
                    if (fields == null || fields.Length < 8) continue;

                    facilities.Add(new Facility
                    {
                        // تأكدي من ترتيب الأعمدة في ملفك الـ CSV
                        Name = fields[1],
                        Type = fields[2],
                        Address = fields[3],
                        PhoneNumber = fields[4],
                        Governorate = fields[5],
                        Latitude = double.TryParse(fields[6], out var lat) ? lat : 0,
                        Longitude = double.TryParse(fields[7], out var lon) ? lon : 0,
                        OperatingHours = "Open 24 Hours"
                    });
                }
            }

            // تنظيف الجدول القديم قبل الرفع الجديد (اختياري عشان ميبقاش فيه تكرار)
            _context.Facilities.RemoveRange(_context.Facilities);

            await _context.Facilities.AddRangeAsync(facilities);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, count = facilities.Count });
        }
        [HttpPost("upload-governorate-specialties")]
        public async Task<IActionResult> UploadGovSpecialties(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("يرجى رفع ملف CSV.");

            _context.MedicalProviders.RemoveRange(_context.MedicalProviders);
            await _context.SaveChangesAsync();

            using var stream = new StreamReader(file.OpenReadStream());
            await stream.ReadLineAsync(); // Header

            while (!stream.EndOfStream)
            {
                var line = await stream.ReadLineAsync();
                if (string.IsNullOrEmpty(line)) continue;

                var v = line.Split(',');
                if (v.Length >= 7)
                {
                    _context.MedicalProviders.Add(new MedicalProvider
                    {
                        Governorate = v[0].Trim(),
                        ProviderName = v[1].Trim(),
                        Specialty = v[2].Trim(),
                        Phone = v[3].Trim(),
                        WorkingHours = v[4].Trim(),
                        Latitude = double.TryParse(v[5], out var la) ? la : 0,
                        Longitude = double.TryParse(v[6], out var lo) ? lo : 0
                    });
                }
            }
            await _context.SaveChangesAsync();
            return Ok("تم الرفع والترتيب الجغرافي مفعل!");
        }
    }
}

