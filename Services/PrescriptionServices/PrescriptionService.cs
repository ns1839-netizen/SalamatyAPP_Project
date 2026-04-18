using System.Text.Json.Serialization; // مهم جداً للـ Mapping
using Microsoft.EntityFrameworkCore;
using Salamaty.API.Models;
using SalamatyAPI.Data;

namespace Salamaty.API.Services.PrescriptionServices
{
    // 1. الكلاسات المساعدة بأسماء مطابقة تماماً لرد الـ AI
    public class AIScanResponse
    {
        [JsonPropertyName("medicines")]
        public List<AIMedicineResult> Medicines { get; set; } = new();

        [JsonPropertyName("total_found")]
        public int TotalFound { get; set; }
    }

    public class AIMedicineResult
    {
        [JsonPropertyName("ocr_text")]
        public string? OcrText { get; set; }

        [JsonPropertyName("matched_drug")]
        public string? MatchedDrug { get; set; }

        [JsonPropertyName("match_score")]
        public double MatchScore { get; set; }
    }

    // 2. الكلاس الخاص بالنتيجة النهائية اللي بترجع للـ Flutter
    public class ScanResultDto
    {
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

            // تأمين اسم الملف
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(prescriptionImage.FileName);

            try
            {
                // الخطوة 1: حفظ الصورة في wwwroot/Prescriptions
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Prescriptions");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await prescriptionImage.CopyToAsync(stream);
                }

                // الخطوة 2: إرسال الصورة لـ API الـ AI (مريم ناصر)
                using var requestContent = new MultipartFormDataContent();
                var imageStream = prescriptionImage.OpenReadStream();
                requestContent.Add(new StreamContent(imageStream), "file", prescriptionImage.FileName);

                var aiUrl = "https://mariamnasser02-slamaty-prescription-api.hf.space/api/scan";
                var response = await _httpClient.PostAsync(aiUrl, requestContent);

                // لو السيرفر الخارجي مش متاح أو هنج (حرف الـ I)
                if (!response.IsSuccessStatusCode)
                    return finalResult;

                // الخطوة 3: قراءة وتحويل الـ JSON (الـ Deserialization)
                var aiResult = await response.Content.ReadFromJsonAsync<AIScanResponse>();

                if (aiResult?.Medicines == null || !aiResult.Medicines.Any())
                    return finalResult;

                // الخطوة 4: فلترة الأسماء (أكبر من 50%) وتجهيزها للبحث
                var namesFromAi = aiResult.Medicines
                    .Where(m => m.MatchScore > 50 && !string.IsNullOrEmpty(m.MatchedDrug))
                    .Select(m => m.MatchedDrug!.ToLower().Trim())
                    .Distinct()
                    .ToList();

                // الخطوة 5: البحث "المرن" في جدول الـ Products
                // هنا بنجيب الأدوية اللي اسمها في الداتابيز يشبه اللي الـ AI لقاه
                var availableInDb = await _context.Products
                    .Where(p => namesFromAi.Any(aiName =>
                        p.Name.ToLower().Contains(aiName) || aiName.Contains(p.Name.ToLower())))
                    .Select(p => new DetectedMedicineDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Price = p.Price.GetValueOrDefault(),
                        ImageUrl = p.ImageUrl ?? string.Empty,
                        IsAvailable = true
                    }).ToListAsync();

                // الأدوية اللي الـ AI لقاها بس مش موجودة عندنا في الـ Products
                var notAvailable = namesFromAi
                    .Where(name => !availableInDb.Any(db => db.Name.ToLower().Contains(name)))
                    .Select(name => new DetectedMedicineDto
                    {
                        Name = name,
                        IsAvailable = false,
                        ImageUrl = string.Empty
                    })
                    .ToList();

                // الخطوة 6: تسجيل العملية في الهيستوري (جدول Prescriptions)
                if (availableInDb.Any() || notAvailable.Any())
                {
                    var prescriptionEntry = new Prescription
                    {
                        UserId = userId,
                        ImagePath = "/Prescriptions/" + uniqueFileName,
                        ScanDate = DateTime.UtcNow,
                        DetectedMedicines = string.Join(", ", availableInDb.Select(m => m.Name))
                    };
                    _context.Prescriptions.Add(prescriptionEntry);
                    await _context.SaveChangesAsync();
                }

                finalResult.AvailableMedicines = availableInDb;
                finalResult.NotAvailableMedicines = notAvailable;
            }
            catch (Exception ex)
            {
                // تسجيل الخطأ عشان تقدري تشوفي الـ Console لو فيه مشكلة
                Console.WriteLine($"[Prescription Error]: {ex.Message}");
            }

            return finalResult;
        }
    }
}