using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalamatyAPI.Data;
using SalamatyAPI.Dtos.Insurance;
using SalamatyAPI.Models;
using SalamatyAPI.Models.Enums;

namespace SalamatyAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InsuranceController : ControllerBase
    {
        private readonly SalamatyDbContext _context;
        private readonly IWebHostEnvironment _env;

        public InsuranceController(SalamatyDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: api/insurance/providers
        [HttpGet("providers")]
        public async Task<ActionResult> GetProviders([FromQuery] string? search)
        {
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
                    LogoUrl = p.LogoUrl
                })
                .ToListAsync();

            return Ok(providers);
        }

        // GET: api/insurance/profile/details?userId=1
        [HttpGet("profile/details")]
        public async Task<ActionResult<InsuranceProfileDetailsDto>> GetProfileDetails(
            [FromQuery] int userId)
        {
            var profile = await _context.InsuranceProfiles
                .Include(p => p.User)
                .Include(p => p.InsuranceProvider)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
                return NotFound();

            // ---- User card (Mohamed Ali, Card holder ID...) ----
            var userDto = new UserSectionDto
            {
                FullName = profile.User.FullName,
                CardHolderId = profile.CardHolderId
            };

            // ---- Insurance Provider card (name, policy, valid until) ----
            var providerDto = new ProviderSectionDto
            {
                Id = profile.InsuranceProviderId,
                Name = profile.InsuranceProvider.Name,
                LogoUrl = profile.InsuranceProvider.LogoUrl,
                //ValidUntil = profile.ValidUntil
                // DEMO / GENERATED VALUES:
                PolicyNumber = $"P{profile.Id:D8}",          // e.g. P00000012
                ValidUntil = DateTime.UtcNow.AddYears(3)
            };

            int providerId = profile.InsuranceProviderId;

            // ---- Coverage: Hospitals (from InsuranceNetworkServices) ----
            var hospitalNames = await _context.InsuranceNetworkServices
                .Where(s => s.InsuranceProviderId == providerId &&
                            s.Type == InsuranceServiceType.Hospital)
                .OrderBy(s => s.Name)
                .Select(s => s.Name)
                .Take(3)   // show top 3 in UI; you can change this
                .ToListAsync();

            // ---- Coverage: Lab Tests ----
            var labNames = await _context.InsuranceNetworkServices
                .Where(s => s.InsuranceProviderId == providerId &&
                            s.Type == InsuranceServiceType.Lab)
                .OrderBy(s => s.Name)
                .Select(s => s.Name)
                .Take(3)
                .ToListAsync();

            // ---- Coverage: Medicines ----
            // For now: first 3 products as "covered" examples.
            // Later you can create a mapping table InsuranceProvider <-> Product.
            var medicineNames = await _context.Products
                .OrderBy(p => p.Id)
                .Select(p => p.Name)
                .Take(3)
                .ToListAsync();

            var coverage = new CoverageSectionDto
            {
                Medicines = new CoverageListDto
                {
                    IsCovered = medicineNames.Any(),
                    Items = medicineNames
                },
                LabTests = new CoverageListDto
                {
                    IsCovered = labNames.Any(),
                    Items = labNames
                },
                Hospitals = new CoverageListDto
                {
                    IsCovered = hospitalNames.Any(),
                    Items = hospitalNames
                }
            };

            var result = new InsuranceProfileDetailsDto
            {
                User = userDto,
                Provider = providerDto,
                Coverage = coverage
            };

            return Ok(result);
        }

        [HttpPost("information")]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> SubmitInsuranceInformation(
        [FromQuery] int userId,
        [FromForm] SubmitInsuranceInfoDto dto)
        {
            // Try to find existing insurance profile
            var profile = await _context.InsuranceProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
            {
                // Create new profile for this user + provider
                profile = new InsuranceProfile
                {
                    UserId = userId,
                    InsuranceProviderId = dto.ProviderId
                };
                _context.InsuranceProfiles.Add(profile);
            }
            else
            {
                // If you allow changing provider from this screen:
                profile.InsuranceProviderId = dto.ProviderId;
            }

            // Update card holder ID
            profile.CardHolderId = dto.CardHolderId;

            // Save front/back images if provided
            if (dto.FrontImage != null)
                profile.FrontImagePath = await SaveInsuranceImage(userId, "front", dto.FrontImage);

            if (dto.BackImage != null)
                profile.BackImagePath = await SaveInsuranceImage(userId, "back", dto.BackImage);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Insurance information saved successfully.",
                cardHolderId = profile.CardHolderId,
                providerId = profile.InsuranceProviderId,
                frontImagePath = profile.FrontImagePath,
                backImagePath = profile.BackImagePath
            });
        }

        private async Task<string> SaveInsuranceImage(int userId, string side, IFormFile file)
        {
            var uploadsRoot = Path.Combine(_env.ContentRootPath, "Uploads", "InsuranceCards", userId.ToString());
            Directory.CreateDirectory(uploadsRoot);

            var fileName = $"{side}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var fullPath = Path.Combine(uploadsRoot, fileName);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return Path.Combine("Uploads", "InsuranceCards", userId.ToString(), fileName)
                .Replace("\\", "/");
        }
    }
}
