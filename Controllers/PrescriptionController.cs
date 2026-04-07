using Microsoft.AspNetCore.Mvc;
using Salamaty.API.Services.PrescriptionServices;

namespace Salamaty.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PrescriptionController : ControllerBase
    {
        private readonly IPrescriptionService _prescriptionService;

        public PrescriptionController(IPrescriptionService prescriptionService)
        {
            _prescriptionService = prescriptionService;
        }

        [HttpPost("scan")]
        [Consumes("multipart/form-data")] // ضروري جداً لتعريف Swagger بنوع البيانات
        public async Task<IActionResult> Scan(IFormFile image) // شيلنا [FromForm] عشان نحل الـ Exception
        {
            if (image == null || image.Length == 0)
                return BadRequest("Please upload a prescription image.");

            var result = await _prescriptionService.ScanPrescriptionAsync(image);

            return Ok(new { success = true, data = result });
        }
    }
}