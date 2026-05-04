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
                FullName = !string.IsNullOrEmpty(profile.CardHolderName) ? profile.CardHolderName : profile.User.FullName,
                CardHolderId = profile.CardHolderId
            };

            var providerDto = new ProviderSectionDto
            {
                Id = profile.InsuranceProviderId,
                Name = profile.InsuranceProvider.Name,
                LogoUrl = !string.IsNullOrEmpty(profile.InsuranceProvider.LogoUrl) ? baseUrl + profile.InsuranceProvider.LogoUrl : null,
                PolicyNumber = !string.IsNullOrEmpty(profile.PolicyNumber) ? profile.PolicyNumber : "Not Found",

                ValidUntil = !string.IsNullOrEmpty(profile.ValidUntil) ? profile.ValidUntil : "Not Found" ,
                Status = !string.IsNullOrEmpty(profile.Status) ? profile.Status : "Not Found"
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
            profile.CardHolderName = dto.FullName;
            profile.PolicyNumber = dto.PolicyNumber;
            profile.ValidUntil = dto.ValidUntil;
            profile.Status = dto.Status; // <--- هذا هو السطر الذي كان مفقوداً !!!

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
                cardHolderName = profile.CardHolderName,
                cardHolderId = profile.CardHolderId,
                policyNumber = profile.PolicyNumber,
                validUntil = profile.ValidUntil,
                status = profile.Status, // <-- Now it will return "Valid ✅" instead of null!
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
        public async Task<IActionResult> ScanInsuranceCard([FromForm] UploadInsuranceCardDto request)
        {
            // 1. Check if an image was actually uploaded
            if (request.FrontImage == null || request.FrontImage.Length == 0)
            {
                return BadRequest(new { success = false, message = "Front image is required to scan." });
            }

            var dbProvider = await _context.InsuranceProviders.FindAsync(request.ProviderId);
            if (dbProvider == null)
            {
                return BadRequest(new { success = false, message = "Invalid Insurance Provider selected." });
            }

            // 2. Prepare to call the External AI API
            string aiApiUrl = "https://ai-team-salamaty-card-scanner.hf.space/scan";

            using var httpClient = new HttpClient();
            using var requestContent = new MultipartFormDataContent();

            using var stream = request.FrontImage.OpenReadStream();
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(request.FrontImage.ContentType);
            requestContent.Add(fileContent, "file", request.FrontImage.FileName);

            try
            {
                var response = await httpClient.PostAsync(aiApiUrl, requestContent);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ScannerResponse>(jsonResponse);

                    // 👇 UPDATED HERE: Use ExpiryDate and grab the new Status field!
                    string? extractedName = result?.Data?.Name?.Trim();
                    string? extractedId = result?.Data?.Id?.Trim();
                    string? extractedValidDate = result?.Data?.ExpiryDate?.Trim();
                    string? extractedPolicy = result?.Data?.Policy?.Trim();
                    string? extractedStatus = result?.Data?.Status?.Trim();
                    string? extractedProvider = result?.Data?.InsuranceProvider?.Trim();

                    // ====================================================================
                    // CONSTRAINT: PREVENT RANDOM PHOTOS (CARPETS, SELFIES, ETC)
                    // ====================================================================
                    bool isNotCard = string.IsNullOrEmpty(extractedId) || extractedId.Contains("Not Found") ||
                                     string.IsNullOrEmpty(extractedName) || extractedName.Contains("Not Found");

                    if (isNotCard)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Invalid image. Please upload a clear picture of a valid insurance card."
                        });
                    }

                    // CONSTRAINT 2: CHECK IF THE PROVIDER MATCHES
                    // ====================================================================
                    if (!string.IsNullOrEmpty(extractedProvider) && !extractedProvider.Contains("Not Found"))
                    {
                        // Check if the AI text contains the DB name (e.g., "MedRight Insurance" contains "MedRight")
                        // OR if the DB name contains the AI text
                        bool isProviderMatch = extractedProvider.Contains(dbProvider.Name, StringComparison.OrdinalIgnoreCase) ||
                                               dbProvider.Name.Contains(extractedProvider, StringComparison.OrdinalIgnoreCase);

                        if (!isProviderMatch)
                        {
                            return BadRequest(new
                            {
                                success = false,
                                message = $"Mismatch Error: You selected '{dbProvider.Name}', but the uploaded card belongs to '{extractedProvider}'."
                            });
                        }
                    }


                    // Return all the data so the Mobile App can fill the text boxes!
                    return Ok(new
                    {
                        success = true,
                        message = "Card scanned successfully. Please review your details.",
                        data = new
                        {
                            ScannedId = extractedId,
                            ScannedName = extractedName,
                            ScannedValidDate = extractedValidDate,
                            ScannedPolicy = extractedPolicy,
                            ScannedStatus = extractedStatus ,
                            ScannedProvider = extractedProvider// <-- Now the mobile app knows if it's expired!
                        }
                    });
                }
                else
                {
                    return StatusCode((int)response.StatusCode, new { success = false, message = "Failed to scan card using AI API." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"An error occurred while connecting to the AI API: {ex.Message}" });
            }
        }



    }


}