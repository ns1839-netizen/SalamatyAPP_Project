using Microsoft.EntityFrameworkCore;
using Salamaty.API.DTOs.PrescriptionDTOS;
using Salamaty.API.Models;
using SalamatyAPI.Data;

namespace Salamaty.API.Services.PrescriptionServices
{
    public class PrescriptionService : IPrescriptionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public PrescriptionService(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<List<DetectedMedicineDto>> ScanPrescriptionAsync(IFormFile prescriptionImage, string userId)
        {
            // 1. حفظ الصورة في wwwroot/Prescriptions
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Prescriptions");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + prescriptionImage.FileName;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await prescriptionImage.CopyToAsync(fileStream);
            }

            // 2. محاكاة الـ AI (نفترض إنه قرأ أسامي فيها أخطاء بسيطة)
            var aiDetectedNames = new List<string> { "Augmentin", "Azithral", "Aciloc" };

            // 3. البحث الذكي (Fuzzy Match)
            // بنسحب الأدوية اللي بتبدأ بنفس الحروف أولاً لزيادة السرعة
            var allProducts = await _context.MedicalProducts.ToListAsync();

            var matchedProducts = allProducts
                .Where(p => aiDetectedNames.Any(aiName =>
                    p.Name.Contains(aiName, StringComparison.OrdinalIgnoreCase) ||
                    CalculateSimilarity(p.Name, aiName) > 0.7)) // نسبة تشابه 70%
                .Select(p => new DetectedMedicineDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price ?? 0,
                    ImageUrl = p.ImageUrl ?? string.Empty,
                    Uses = p.Uses ?? string.Empty,
                    Composition = p.Composition ?? string.Empty
                }).ToList();

            // 4. حفظ العملية في جدول الـ Prescriptions (الـ History)
            var prescriptionHistory = new Prescription
            {
                UserId = userId,
                ImagePath = "/Prescriptions/" + uniqueFileName,
                ScanDate = DateTime.UtcNow,
                DetectedMedicines = string.Join(", ", matchedProducts.Select(m => m.Name))
            };

            _context.Prescriptions.Add(prescriptionHistory);
            await _context.SaveChangesAsync();

            return matchedProducts;
        }

        // خوارزمية بسيطة لحساب تشابه الكلمات (Fuzzy Search Logic)
        private double CalculateSimilarity(string source, string target)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target)) return 0;
            if (source == target) return 1.0;

            int stepsToSame = LevenshteinDistance(source, target);
            return 1.0 - ((double)stepsToSame / Math.Max(source.Length, target.Length));
        }

        private int LevenshteinDistance(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];
            if (n == 0) return m;
            if (m == 0) return n;
            for (int i = 0; i <= n; d[i, 0] = i++) ;
            for (int j = 0; j <= m; d[0, j] = j++) ;
            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                }
            }
            return d[n, m];
        }
    }
}