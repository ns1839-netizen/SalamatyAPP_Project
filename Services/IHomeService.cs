using Salamaty.API.Models;

namespace Salamaty.API.Services
{
    public interface IHomeService
    {
        Task<List<Banner>> GetBannersAsync();
        Task<List<Product>> GetNewArrivalsAsync();
        Task<List<Product>> SearchProductsAsync(string query);
    }
}
