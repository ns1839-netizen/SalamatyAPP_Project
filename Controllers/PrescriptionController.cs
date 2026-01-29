using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Salamaty.API.DTOs; // <== هنا ضمّي الـ DTO الجديد
using Salamaty.API.Models;
using SalamatyAPI.Data;

namespace Salamaty.API.Controllers
{
    [ApiController]
    [Route("api/prescriptions")]
    [AllowAnonymous] // مؤقتًا لاختبار Swagger
    public class PrescriptionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public PrescriptionController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upload([FromForm] PrescriptionDto dto)
        {
            if (dto.File == null || dto.File.Length == 0)
                return BadRequest("No file uploaded");

            // مؤقتًا للاختبار
            var userId = "TestUserId";

            var uploads = Path.Combine(_env.ContentRootPath, "Uploads");
            if (!Directory.Exists(uploads))
                Directory.CreateDirectory(uploads);

            var fileName = $"{Guid.NewGuid()}_{dto.File.FileName}";
            var filePath = Path.Combine(uploads, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await dto.File.CopyToAsync(stream);
            }

            var prescription = new Prescription
            {
                FileName = dto.File.FileName,
                FilePath = filePath,
                UserId = userId
            };

            _context.Prescriptions.Add(prescription);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Prescription uploaded successfully", prescriptionId = prescription.Id });
        }
    }
}
