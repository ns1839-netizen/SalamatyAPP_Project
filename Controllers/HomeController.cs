using Microsoft.AspNetCore.Mvc;
using Salamaty.API.Services.HomeServices;

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


    }
}
