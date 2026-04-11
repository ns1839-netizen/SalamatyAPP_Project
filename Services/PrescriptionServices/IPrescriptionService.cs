using global::Salamaty.API.DTOs.PrescriptionDTOS;

namespace Salamaty.API.Services.PrescriptionServices
{
    public interface IPrescriptionService
    {
        // ضفنا الـ userId هنا عشان يطابق الـ Implementation
        Task<List<DetectedMedicineDto>> ScanPrescriptionAsync(IFormFile prescriptionImage, string userId);
    }
}