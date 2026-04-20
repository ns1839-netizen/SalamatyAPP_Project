namespace Salamaty.API.Services.PrescriptionServices
{
    public interface IPrescriptionService
    {
        // غيرنا الـ List لـ ScanResultDto
        Task<ScanResultDto> ScanPrescriptionAsync(IFormFile prescriptionImage, string userId);
    }
}