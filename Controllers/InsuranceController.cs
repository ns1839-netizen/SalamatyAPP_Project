using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalamatyAPI.Data;
using SalamatyAPI.Dtos.Insurance;
using Salamaty.API.DTOs.Insurance;


namespace SalamatyAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InsuranceController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public InsuranceController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: api/insurance/providers
        [HttpGet("providers")]
        public async Task<ActionResult> GetProviders([FromQuery] string? search)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
            var query = _context.InsuranceProviders.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(term));
            }

            var providers = await query
                .Select(p => new InsuranceProviderDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    LogoUrl = !string.IsNullOrEmpty(p.LogoUrl) ? baseUrl + p.LogoUrl : null
                })
                .ToListAsync();

            return Ok(providers);
        }

        // GET: api/insurance/profile/details
        [HttpGet("profile/details")]
        public async Task<ActionResult<InsuranceProfileDetailsDto>> GetProfileDetails([FromQuery] string userId)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";

            var profile = await _context.InsuranceProfiles
                .Include(p => p.User)
                .Include(p => p.InsuranceProvider)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
                return NotFound(new { message = "Profile not found" });

            var userDto = new UserSectionDto
            {
                FullName = profile.User.FullName,
                CardHolderId = profile.CardHolderId
            };

            var providerDto = new ProviderSectionDto
            {
                Id = profile.InsuranceProviderId,
                Name = profile.InsuranceProvider.Name,
                LogoUrl = !string.IsNullOrEmpty(profile.InsuranceProvider.LogoUrl) ? baseUrl + profile.InsuranceProvider.LogoUrl : null,
                PolicyNumber = $"P{profile.Id:D8}",
                ValidUntil = DateTime.UtcNow.AddYears(3)
            };

            int providerId = profile.InsuranceProviderId;

            // استخدام المقارنة النصية لمنع الـ Invalid Cast Exception
            var hospitalNames = await _context.InsuranceNetworkServices
                .Where(s => s.InsuranceProviderId == providerId &&
                            (s.Type.ToLower().Contains("hospital"))) // بحث مرن عن كلمة مستشفى
                .OrderBy(s => s.Name)
                .Select(s => s.Name)
                .Take(3)
                .ToListAsync();

            var labNames = await _context.InsuranceNetworkServices
                .Where(s => s.InsuranceProviderId == providerId &&
                            (s.Type.ToLower().Contains("lab") || s.Type.ToLower().Contains("analysis"))) // بحث مرن عن المعامل
                .OrderBy(s => s.Name)
                .Select(s => s.Name)
                .Take(3)
                .ToListAsync();

            var medicineNames = await _context.Products
                .OrderBy(p => p.Id)
                .Select(p => p.Name)
                .Take(3)
                .ToListAsync();

            var coverage = new CoverageSectionDto
            {
                Medicines = new CoverageListDto { IsCovered = medicineNames.Any(), Items = medicineNames },
                LabTests = new CoverageListDto { IsCovered = labNames.Any(), Items = labNames },
                Hospitals = new CoverageListDto { IsCovered = hospitalNames.Any(), Items = hospitalNames }
            };

            return Ok(new InsuranceProfileDetailsDto
            {
                User = userDto,
                Provider = providerDto,
                Coverage = coverage
            });
        }

        [HttpPost("information")]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> SubmitInsuranceInformation([FromQuery] string userId, [FromForm] SubmitInsuranceInfoDto dto)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";

            var profile = await _context.InsuranceProfiles.FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
            {
                profile = new InsuranceProfile { UserId = userId, InsuranceProviderId = dto.ProviderId };
                _context.InsuranceProfiles.Add(profile);
            }
            else
            {
                profile.InsuranceProviderId = dto.ProviderId;
            }

            profile.CardHolderId = dto.CardHolderId;

            if (dto.FrontImage != null)
                profile.FrontImagePath = await SaveInsuranceImage(userId, "front", dto.FrontImage);

            if (dto.BackImage != null)
                profile.BackImagePath = await SaveInsuranceImage(userId, "back", dto.BackImage);

            await _context.SaveChangesAsync();

            string GetFullUrl(string path)
            {
                if (string.IsNullOrEmpty(path)) return null;
                return path.StartsWith("/") ? baseUrl + path : $"{baseUrl}/{path}";
            }

            return Ok(new
            {
                message = "Insurance information saved successfully.",
                cardHolderId = profile.CardHolderId,
                providerId = profile.InsuranceProviderId,
                frontImagePath = GetFullUrl(profile.FrontImagePath),
                backImagePath = GetFullUrl(profile.BackImagePath)
            });
        }

        private async Task<string> SaveInsuranceImage(string userId, string side, IFormFile file)
        {
            var uploadsRoot = Path.Combine(_env.ContentRootPath, "Uploads", "InsuranceCards", userId);
            Directory.CreateDirectory(uploadsRoot);

            var fileName = $"{side}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var fullPath = Path.Combine(uploadsRoot, fileName);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return Path.Combine("Uploads", "InsuranceCards", userId, fileName).Replace("\\", "/");
        }


        [HttpPost("scan")] 
        public async Task<IActionResult> CheckInsuranceCard(
         [FromQuery] string userId,
         [FromForm] SubmitInsuranceInfoDto request) // <-- FIXED: Grouped all FromForm fields into your existing DTO!
        {
            // 1. Check if an image was actually uploaded
            if (request.FrontImage == null || request.FrontImage.Length == 0)
            {
                return BadRequest("Front image is required.");
            }

            // 2. Prepare to call the External AI API
            string aiApiUrl = "https://mariamnasser02-card-scanner.hf.space/scan";

            using var httpClient = new HttpClient();
            using var requestContent = new MultipartFormDataContent();

            // 3. Read the uploaded file into a stream
            using var stream = request.FrontImage.OpenReadStream(); // Notice we use request.FrontImage now
            var fileContent = new StreamContent(stream);

            // Set the content type (e.g., image/jpeg or image/png)
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(request.FrontImage.ContentType);

            // IMPORTANT: The external AI API expects the parameter name to be "file"
            requestContent.Add(fileContent, "file", request.FrontImage.FileName);

            try
            {
                // 4. Send the POST request to the AI API
                var response = await httpClient.PostAsync(aiApiUrl, requestContent);

                if (response.IsSuccessStatusCode)
                {
                    // 5. Read the JSON response
                    var jsonResponse = await response.Content.ReadAsStringAsync();

                    // 6. Convert JSON to our C# Object
                    var result = JsonSerializer.Deserialize<ScannerResponse>(jsonResponse);

                    string extractedName = result?.Data?.Name;
                    string extractedId = result?.Data?.Id;

                    // TODO: Here you can check if the 'extractedId' matches your database
                    // or return it to the user.

                    return Ok(new
                    {
                        Message = "Card scanned successfully",
                        ScannedId = extractedId,
                        ScannedName = extractedName
                    });
                }
                else
                {
                    return StatusCode((int)response.StatusCode, "Failed to scan card using AI API.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while connecting to the AI API: {ex.Message}");
            }
        }







    }


}