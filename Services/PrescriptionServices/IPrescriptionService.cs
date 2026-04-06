
using global::Salamaty.API.DTOs.PrescriptionDTOS;

namespace Salamaty.API.Services.PrescriptionServices
{
    public interface IPrescriptionService
    {
        // ميثود لاستلام صورة الروشتة وإرجاع قائمة أدوية
        Task<List<DetectedMedicineDto>> ScanPrescriptionAsync(IFormFile prescriptionImage);
    }
}

