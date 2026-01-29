using Microsoft.EntityFrameworkCore;
using Salamaty.API.Models;
using SalamatyAPI.Data;

namespace Salamaty.API.Services
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

        public async Task<List<Product>> GetNewArrivalsAsync()
        {
            return await _context.Products
                .OrderByDescending(p => p.CreatedAt)
                .Take(10)
                .ToListAsync();
        }

        public async Task<List<Product>> SearchProductsAsync(string query)
        {
            return await _context.Products
                .Where(p => p.Name.Contains(query))
                .ToListAsync();
        }
    }
}
