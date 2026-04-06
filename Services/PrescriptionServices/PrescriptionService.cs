using Microsoft.EntityFrameworkCore;
using Salamaty.API.DTOs.PrescriptionDTOS;
using SalamatyAPI.Data;

namespace Salamaty.API.Services.PrescriptionServices
{
    public class PrescriptionService : IPrescriptionService
    {
        private readonly ApplicationDbContext _context;

        public PrescriptionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<DetectedMedicineDto>> ScanPrescriptionAsync(IFormFile prescriptionImage)
        {

            var aiDetectedNames = new List<string> { "Amikacin", "Bisoprolol", "Ibuprofen" };

            // 2. البحث الذكي (Fuzzy Matching) في جدولك الجديد MedicalProducts
            var matchedProducts = await _context.MedicalProducts
                .Where(p => aiDetectedNames.Any(name => p.Name.Contains(name)))
                .Select(p => new DetectedMedicineDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    ImageUrl = p.ImageUrl,
                    Category = p.Category
                })
                .ToListAsync();

            // 3. لو ملقيناش أي دواء مطابق، بنرجع لستة فاضية
            return matchedProducts;
        }
    }
}