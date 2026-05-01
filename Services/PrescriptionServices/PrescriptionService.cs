using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Salamaty.API.Models;
using SalamatyAPI.Data;

namespace Salamaty.API.Services.PrescriptionServices
{
    // 1. الكلاسات المساعدة (خارج كلاس السيرفيس لسهولة الوصول)
    public class AIScanResponse
    {
        [JsonPropertyName("medicines")]
        public List<AIMedicineResult>? Medicines { get; set; } = new();
    }

    public class AIMedicineResult
    {
        [JsonPropertyName("matched_drug")]
        public string? MatchedDrug { get; set; }

        [JsonPropertyName("match_score")]
        public double MatchScore { get; set; }
    }

    public class ScanResultDto
    {
        public List<string> ExtractedMedicines { get; set; } = new();
        public List<DetectedMedicineDto> AvailableMedicines { get; set; } = new();
        public List<DetectedMedicineDto> NotAvailableMedicines { get; set; } = new();
    }

    public class PrescriptionService : IPrescriptionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PrescriptionService(
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment,
            HttpClient httpClient,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ScanResultDto> ScanPrescriptionAsync(IFormFile prescriptionImage, string userId)
        {
            var finalResult = new ScanResultDto();

            if (prescriptionImage == null || string.IsNullOrEmpty(userId)) return finalResult;

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(prescriptionImage.FileName);
            string aiUrl = "https://ai-team-salamaty-slamaty-prescription-api.hf.space/api/scan";

            // بناء الرابط الديناميكي للسيرفر المرفوع
            var request = _httpContextAccessor.HttpContext?.Request;
            var baseUrl = request != null ? $"{request.Scheme}://{request.Host}" : "http://salamaty.runasp.net";

            try
            {
                // 1. حفظ الصورة محلياً
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Prescriptions");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create)) { await prescriptionImage.CopyToAsync(fileStream); }

                // 2. طلب الـ AI
                using var requestContent = new MultipartFormDataContent();
                var imageStream = prescriptionImage.OpenReadStream();
                var streamContent = new StreamContent(imageStream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(prescriptionImage.ContentType);
                requestContent.Add(streamContent, "file", prescriptionImage.FileName);

                var response = await _httpClient.PostAsync(aiUrl, requestContent);
                if (!response.IsSuccessStatusCode) return finalResult;

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var aiResult = await response.Content.ReadFromJsonAsync<AIScanResponse>(options);

                if (aiResult?.Medicines == null) return finalResult;

                // 3. المطابقة المثالية (Exact Match Only)
                // تجهيز أسماء الـ AI بدون مسافات وبحروف صغيرة
                var cleanAiNames = aiResult.Medicines
                    .Where(m => m.MatchScore >= 50 && !string.IsNullOrWhiteSpace(m.MatchedDrug))
                    .Select(m => m.MatchedDrug!.ToLower().Replace(" ", "").Trim())
                    .Distinct().ToList();

                if (!cleanAiNames.Any()) return finalResult;

                // وضع الأسماء الأصلية للعرض
                finalResult.ExtractedMedicines = aiResult.Medicines
                    .Where(m => m.MatchScore >= 50 && !string.IsNullOrWhiteSpace(m.MatchedDrug))
                    .Select(m => m.MatchedDrug ?? "")
                    .Distinct().ToList();

                // سحب الأدوية للمطابقة (بأمان ضد الـ Null)
                var allProducts = await _context.Products.ToListAsync();

                var matchedProducts = allProducts.Where(p =>
                {
                    var cleanDbName = (p.Name ?? "").ToLower().Replace(" ", "");
                    return cleanAiNames.Contains(cleanDbName);
                }).Select(p => new DetectedMedicineDto
                {
                    Id = p.Id,
                    Name = p.Name ?? "Unknown",
                    Price = p.Price.GetValueOrDefault(),
                    ImageUrl = string.IsNullOrEmpty(p.ImageUrl)
                               ? ""
                               : $"{baseUrl}/{p.ImageUrl.Replace("\\", "/")}",
                    IsAvailable = true
                }).ToList();

                finalResult.AvailableMedicines = matchedProducts;

                // 5. تحديد غير المتاح بالمطابقة الدقيقة
                finalResult.NotAvailableMedicines = finalResult.ExtractedMedicines
                    .Where(aiOriginal => !matchedProducts.Any(db =>
                        (db.Name ?? "").ToLower().Replace(" ", "") == aiOriginal.ToLower().Replace(" ", "")
                    ))
                    .Select(aiName => new DetectedMedicineDto { Name = aiName, IsAvailable = false })
                    .ToList();

                // 6. الهيستوري
                try
                {
                    var history = new Prescription
                    {
                        UserId = userId,
                        ImagePath = "/Prescriptions/" + uniqueFileName,
                        ScanDate = DateTime.UtcNow,
                        DetectedMedicines = string.Join(", ", finalResult.ExtractedMedicines)
                    };
                    _context.Prescriptions.Add(history);
                    await _context.SaveChangesAsync();
                }
                catch (Exception dbEx) { Console.WriteLine($">>>> History Save Failed: {dbEx.Message}"); }
            }
            catch (Exception ex) { Console.WriteLine($"[Critical Service Error]: {ex.Message}"); }

            return finalResult;
        }
    }
}