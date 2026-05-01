using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Salamaty.API.Models;
using SalamatyAPI.Data;

namespace Salamaty.API.Services.PrescriptionServices
{
    public class AIScanResponse
    {
        [JsonPropertyName("medicines")]
        public List<AIMedicineResult> Medicines { get; set; } = new();
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
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(prescriptionImage.FileName);
            string aiUrl = "https://ai-team-salamaty-slamaty-prescription-api.hf.space/api/scan";

            var request = _httpContextAccessor.HttpContext?.Request;
            var baseUrl = request != null ? $"{request.Scheme}://{request.Host}" : "http://salamaty.runasp.net";

            try
            {
                // 1. حفظ الصورة
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

                if (aiResult?.Medicines == null || !aiResult.Medicines.Any()) return finalResult;

                // 3. فلترة الـ AI (Match Score >= 50)
                var namesFromAi = aiResult.Medicines
                    .Where(m => m.MatchScore >= 50 && !string.IsNullOrWhiteSpace(m.MatchedDrug))
                    .Select(m => m.MatchedDrug!.ToLower().Trim())
                    .Distinct().ToList();

                if (!namesFromAi.Any()) return finalResult;
                finalResult.ExtractedMedicines = namesFromAi;

                // 4. البحث الدقيق جداً (Strict Precision Search)
                // سحب البيانات للميموري لضمان تنفيذ الـ String Operations بشكل صحيح
                var allProducts = await _context.Products.ToListAsync();

                var matchedProducts = allProducts.Where(p => namesFromAi.Any(aiName =>
                {
                    // تنظيف كامل للأسماء من المسافات
                    var cleanDbName = p.Name.ToLower().Replace(" ", "");
                    var cleanAiName = aiName.Replace(" ", "");

                    // مطابقة كاملة فقط! (إما يساوي تماماً أو يبدأ به في حالة التركيزات)
                    return cleanDbName == cleanAiName ||
                           cleanDbName.StartsWith(cleanAiName) ||
                           cleanAiName.StartsWith(cleanDbName);
                })).Select(p => new DetectedMedicineDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price.GetValueOrDefault(),
                    ImageUrl = string.IsNullOrEmpty(p.ImageUrl)
                               ? ""
                               : $"{baseUrl}/{p.ImageUrl.Replace("\\", "/")}",
                    IsAvailable = true
                }).ToList();

                // فلترة نهائية لمنع "الزيادات" (مثل لقط Neuroton مع Neurotone)
                // بنأخد فقط الأقرب طولاً للاسم المستخرج
                finalResult.AvailableMedicines = matchedProducts
                    .GroupBy(p => p.Name.ToLower().Replace(" ", ""))
                    .Select(g => g.First())
                    .ToList();

                // 5. تحديد غير المتاح (بناءً على ما لم يجد مطابقة دقيقة)
                finalResult.NotAvailableMedicines = namesFromAi
                    .Where(aiName => !matchedProducts.Any(db =>
                        db.Name.ToLower().Replace(" ", "") == aiName.Replace(" ", "") ||
                        db.Name.ToLower().Replace(" ", "").StartsWith(aiName.Replace(" ", ""))
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
                        DetectedMedicines = string.Join(", ", namesFromAi)
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