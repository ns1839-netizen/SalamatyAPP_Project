using Salamaty.API.Models.HomeModels;

namespace Salamaty.API.Services.HomeServices
{
    public interface IHomeService
    {
        Task<List<Banner>> GetBannersAsync();

    }
}
