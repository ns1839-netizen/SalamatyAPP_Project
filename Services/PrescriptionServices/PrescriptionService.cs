using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Salamaty.API.Models;
using SalamatyAPI.Data;

namespace Salamaty.API.Services.PrescriptionServices
{
    // 1. الكلاسات المساعدة للـ Mapping
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

    // 2. الـ DTO المحدث ليشمل كل النتائج المكتشفة
    public class ScanResultDto
    {
        public List<string> ExtractedMedicines { get; set; } = new(); // القائمة الجديدة
        public List<DetectedMedicineDto> AvailableMedicines { get; set; } = new();
        public List<DetectedMedicineDto> NotAvailableMedicines { get; set; } = new();
    }

    public class PrescriptionService : IPrescriptionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly HttpClient _httpClient;

        public PrescriptionService(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, HttpClient httpClient)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _httpClient = httpClient;
        }

        public async Task<ScanResultDto> ScanPrescriptionAsync(IFormFile prescriptionImage, string userId)
        {
            var finalResult = new ScanResultDto();
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(prescriptionImage.FileName);
            string aiUrl = "https://mariamnasser02-slamaty-prescription-api.hf.space/api/scan";

            try
            {
                // 1. حفظ الصورة محلياً
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Prescriptions");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create)) { await prescriptionImage.CopyToAsync(fileStream); }

                // 2. إرسال الطلب للـ AI بطريقة احترافية
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

                // 3. فلترة الأسماء المكتشفة (Match Score >= 50)
                var namesFromAi = aiResult.Medicines
                    .Where(m => m.MatchScore >= 50 && !string.IsNullOrWhiteSpace(m.MatchedDrug))
                    .Select(m => m.MatchedDrug!.ToLower().Trim())
                    .Distinct().ToList();

                if (!namesFromAi.Any()) return finalResult;

                // وضع كل الأسماء المكتشفة في النتيجة النهائية
                finalResult.ExtractedMedicines = namesFromAi;

                // 4. تحديد الأدوية المتاحة (التعديل هنا لإرجاع اللينك كامل)
                var availableInDb = await _context.Products
                    .Where(p => namesFromAi.Any(aiName => p.Name.ToLower().Contains(aiName)))
                    .Select(p => new DetectedMedicineDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Price = p.Price.GetValueOrDefault(),
                        // تعديل اللينك ليصبح Full URL
                        ImageUrl = string.IsNullOrEmpty(p.ImageUrl)
                                   ? ""
                                   : $"https://localhost:7140/{p.ImageUrl.Replace("\\", "/")}",
                        IsAvailable = true
                    }).ToListAsync();

                // 5. تحديد الأدوية غير المتاحة
                var notAvailable = namesFromAi
                    .Where(aiName => !availableInDb.Any(db => db.Name.ToLower().Contains(aiName)))
                    .Select(aiName => new DetectedMedicineDto
                    {
                        Name = aiName,
                        IsAvailable = false
                    }).ToList();

                // ملأ القوائم المصنفة
                finalResult.AvailableMedicines = availableInDb;
                finalResult.NotAvailableMedicines = notAvailable;

                // 6. محاولة حفظ العملية في الهيستوري (Safe Block)
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
                catch (Exception dbEx)
                {
                    Console.WriteLine($">>>> History Save Failed: {dbEx.Message}. But data is returned to user.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Critical Service Error]: {ex.Message}");
            }

            return finalResult;
        }
    }
}