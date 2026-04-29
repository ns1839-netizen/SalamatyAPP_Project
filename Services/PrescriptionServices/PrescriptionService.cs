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

            // 1. الرابط الجديد تماماً
            string aiUrl = "https://ai-team-salamaty-slamaty-prescription-api.hf.space/api/scan";

            try
            {
                // حفظ صورة الروشتة
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Prescriptions");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create)) { await prescriptionImage.CopyToAsync(fileStream); }

                // 2. إرسال الصورة للـ AI
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

                // 3. فلترة النتائج
                var namesFromAi = aiResult.Medicines
                    .Where(m => m.MatchScore >= 70 && !string.IsNullOrWhiteSpace(m.MatchedDrug))
                    .Select(m => m.MatchedDrug!.ToLower().Trim())
                    .Distinct().ToList();

                if (!namesFromAi.Any()) return finalResult;

                finalResult.ExtractedMedicines = namesFromAi;

                // 4. البحث في جدول الـ Products (تعديل احترافي للمسافات)
                var availableInDb = await _context.Products
                    .Where(p => namesFromAi.Any(aiName =>
                        // بنشيل المسافات من اسم الدواء في الداتابيز وبنحوله لـ lower
                        p.Name.Replace(" ", "").ToLower().Contains(aiName.Replace(" ", ""))
                    ))
                    .Select(p => new DetectedMedicineDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Price = p.Price.GetValueOrDefault(),
                        ImageUrl = string.IsNullOrEmpty(p.ImageUrl)
                                   ? ""
                                   : $"https://localhost:7140/{p.ImageUrl.Replace("\\", "/")}",
                        IsAvailable = true
                    }).ToListAsync();

                // 5. تحديد غير المتاح
                var notAvailable = namesFromAi
                    .Where(aiName => !availableInDb.Any(db => db.Name.ToLower().Contains(aiName)))
                    .Select(aiName => new DetectedMedicineDto { Name = aiName, IsAvailable = false })
                    .ToList();

                finalResult.AvailableMedicines = availableInDb;
                finalResult.NotAvailableMedicines = notAvailable;

                // 6. حفظ الهيستوري
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
                    Console.WriteLine($">>>> History Save Failed: {dbEx.Message}");
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