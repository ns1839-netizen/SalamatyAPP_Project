using Microsoft.EntityFrameworkCore;
using Salamaty.API.Models;
using SalamatyAPI.Data;

namespace Salamaty.API.Services.PrescriptionServices
{
    // كلاسات مساعدة لاستقبال رد الـ AI
    public class AIScanResponse
    {
        public List<AIMedicineResult> Medicines { get; set; } = new();
    }
    public class AIMedicineResult
    {
        public string? Matched_drug { get; set; }
        public double Match_score { get; set; }
    }

    // الكلاس الخاص بالنتيجة النهائية المقسمة
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
            // 1. سيف الصورة في wwwroot للهيستوري
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + prescriptionImage.FileName;
            string filePath = Path.Combine(_webHostEnvironment.WebRootPath, "Prescriptions", uniqueFileName);
            using (var stream = new FileStream(filePath, FileMode.Create)) { await prescriptionImage.CopyToAsync(stream); }

            // 2. كلمي API الـ AI (مريم ناصر)
            using var content = new MultipartFormDataContent();
            content.Add(new StreamContent(prescriptionImage.OpenReadStream()), "file", prescriptionImage.FileName);

            var response = await _httpClient.PostAsync("https://mariamnasser02-slamaty-prescription-api.hf.space/api/scan", content);
            var aiResult = await response.Content.ReadFromJsonAsync<AIScanResponse>();

            // 3. فلترة (أكبر من 50%)
            var namesFromAi = aiResult.Medicines
                .Where(m => m.Match_score > 50 && !string.IsNullOrEmpty(m.Matched_drug))
                .Select(m => m.Matched_drug.ToLower())
                .Distinct().ToList();

            // 4. البحث في جدول Products وتصنيف المتاح وغير المتاح
            var availableInDb = await _context.Products
                .Where(p => namesFromAi.Any(name => p.Name.ToLower().Contains(name)))
                .Select(p => new DetectedMedicineDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    ImageUrl = p.ImageUrl,
                    IsAvailable = true
                }).ToListAsync();

            var notAvailable = namesFromAi
                .Where(name => !availableInDb.Any(db => db.Name.ToLower().Contains(name)))
                .Select(name => new DetectedMedicineDto { Name = name, IsAvailable = false })
                .ToList();

            // 5. سجل العملية في جدول الـ Prescriptions (History)
            _context.Prescriptions.Add(new Prescription
            {
                UserId = userId,
                ImagePath = "/Prescriptions/" + uniqueFileName,
                DetectedMedicines = string.Join(", ", availableInDb.Select(m => m.Name))
            });
            await _context.SaveChangesAsync();

            return new ScanResultDto { AvailableMedicines = availableInDb, NotAvailableMedicines = notAvailable };
        }
    }
}