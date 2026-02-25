using Microsoft.AspNetCore.Mvc;
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