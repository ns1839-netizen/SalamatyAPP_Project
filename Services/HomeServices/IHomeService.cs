using Salamaty.API.Models.HomeModels;

namespace Salamaty.API.Services.HomeServices
{
    public interface IHomeService
    {
        Task<List<Banner>> GetBannersAsync();

        // تأكدي إن النوع هنا object عشان يقبل الـ LocationUrl والـ Phone
        Task<List<object>> FilterProvidersAsync(string? governorate, string? specialty, string? searchTerm, double? userLat, double? userLng);

        // الميثود اللي كانت عاملة الـ Error
        Task<List<MedicalProvider>> GetAllProvidersAsync();
    }
}