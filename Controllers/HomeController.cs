using Microsoft.AspNetCore.Mvc;
using Salamaty.API.Services;

namespace Salamaty.API.Controllers
{
    [ApiController]
    [Route("api/home")]
    public class HomeController : ControllerBase
    {
        private readonly IHomeService _homeService;

        public HomeController(IHomeService homeService)
        {
            _homeService = homeService;
        }

        [HttpGet("banners")]
        public async Task<IActionResult> GetBanners()
        {
            var banners = await _homeService.GetBannersAsync();
            return Ok(banners);
        }

        [HttpGet("products/new-arrivals")]
        public async Task<IActionResult> GetNewArrivals()
        {
            var products = await _homeService.GetNewArrivalsAsync();
            return Ok(products);
        }

        [HttpGet("products/search")]
        public async Task<IActionResult> SearchProducts([FromQuery] string query)
        {
            var products = await _homeService.SearchProductsAsync(query);
            return Ok(products);
        }
    }
}
