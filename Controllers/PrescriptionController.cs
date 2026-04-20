using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Mvc;
using Salamaty.API.Models;
using Salamaty.API.Services.PrescriptionServices;
using SalamatyAPI.Data; // لازم السطر ده عشان يشوف الـ ApplicationDbContext

namespace Salamaty.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PrescriptionController : ControllerBase
    {
        private readonly IPrescriptionService _prescriptionService;
        private readonly ApplicationDbContext _context; // رجعنا الـ context هنا

        public PrescriptionController(IPrescriptionService prescriptionService, ApplicationDbContext context)
        {
            _prescriptionService = prescriptionService;
            _context = context; // حقن الـ context
        }

        [HttpPost("scan")]
        public async Task<IActionResult> Scan(IFormFile image, [FromForm] string userId)
        {
            var result = await _prescriptionService.ScanPrescriptionAsync(image, userId);
            return Ok(new { success = true, data = result });
        }

        [HttpPost("upload-csv")]
        public async Task<IActionResult> UploadCsv(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Please upload a valid CSV file.");

            try
            {
                using var reader = new StreamReader(file.OpenReadStream());
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    HeaderValidated = null,
                    MissingFieldFound = null
                };

                using var csv = new CsvReader(reader, config);

                var records = new List<MedicalProduct>();
                csv.Read();
                csv.ReadHeader();
                while (csv.Read())
                {
                    var record = new MedicalProduct
                    {
                        Name = csv.GetField("Medicine Name") ?? "",
                        Composition = csv.GetField("Composition"),
                        Uses = csv.GetField("Uses"),
                        SideEffects = csv.GetField("Side_effects"),
                        ImageUrl = csv.GetField("Image URL"),
                        Manufacturer = csv.GetField("Manufacturer"),
                        Price = 0,
                        // لو حابة تسحبي الـ Reviews اللي صلحناها قبل الـ Reset:
                        ExcellentReviewPercent = csv.GetField<int>("Excellent Review %"),
                        AverageReviewPercent = csv.GetField<int>("Average Review %")
                    };
                    records.Add(record);
                }

                // إضافة البيانات للداتابيز
                _context.MedicalProducts.AddRange(records);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = $"{records.Count} medicines uploaded successfully!" });
            }
            catch (Exception ex)
            {
                // بنعرض الـ InnerException عشان لو فيه Error في الداتا نعرفه
                var innerMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return StatusCode(500, $"Internal server error: {innerMsg}");
            }
        }
    }
}