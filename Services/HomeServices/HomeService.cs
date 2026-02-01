using Microsoft.EntityFrameworkCore;
using Salamaty.API.Models.HomeModels;
using SalamatyAPI.Data;

namespace Salamaty.API.Services.HomeServices
{
    public class HomeService : IHomeService
    {
        private readonly ApplicationDbContext _context;

        public HomeService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Banner>> GetBannersAsync()
        {
            return await _context.Banners.ToListAsync();
        }


    }
}
