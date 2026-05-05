using System.Globalization;
using CsvHelper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using Salamaty.API.Models;
using SalamatyAPI.Data;
using SalamatyAPI.Models;

namespace Salamaty.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UploadController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- 1. ميثود رفع الأدوية (بدون ربط علاقات) ---
        [HttpPost("upload-medicines-simple")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadMedicines(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("الملف فارغ");

            try
            {
                // أ: تصفير الجداول والعدادات (لضمان البدء من ID 1)
                // ملحوظة: مسحنا جدول البدائل الأول عشان هو معتمد (Foreign Key) على جدول المنتجات
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM ProductAlternatives");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM Products");
                await _context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Products', RESEED, 0)");

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets[0];

                var products = new List<Product>();
                for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                {
                    var name = worksheet.Cells[row, 1].Text.Trim();
                    if (string.IsNullOrEmpty(name)) continue;

                    decimal.TryParse(worksheet.Cells[row, 2].Text, out var price);
                    products.Add(new Product
                    {
                        Name = name,
                        Price = price != 0 ? price : null,
                        SideEffects = worksheet.Cells[row, 3].Text.Trim(),
                        Description = worksheet.Cells[row, 4].Text.Trim(),
                        Uses = worksheet.Cells[row, 5].Text.Trim(),
                        Alternatives = worksheet.Cells[row, 6].Text.Trim(),
                        Category = worksheet.Cells[row, 7].Text.Trim(),
                        ImageUrl = worksheet.Cells[row, 8].Text.Trim(),
                        Pharmacies = worksheet.Cells[row, 9].Text.Trim()
                    });
                }

                // ب: حفظ المنتجات في الداتابيز
                _context.Products.AddRange(products);
                await _context.SaveChangesAsync();

                return Ok(new { message = $"تم رفع {products.Count} منتج بنجاح، والـ IDs بدأت من 1." });
            }
            catch (Exception ex) { return StatusCode(500, $"خطأ تقني: {ex.Message}"); }
        }
        // --- 2. ميثود رفع الشبكة الطبية وربط شركات التأمين بالـ ID ---
        [HttpPost("upload-insurance-network-smart-link")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadNetwork(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("الملف فارغ");

            try
            {
                // أ: تصفير الجدول والعداد
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM InsuranceNetworkServices");
                await _context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('InsuranceNetworkServices', RESEED, 0)");

                // ب: جلب شركات التأمين الموجودة مسبقاً للربط
                var existingProviders = await _context.InsuranceProviders.ToListAsync();

                using var reader = new StreamReader(file.OpenReadStream());
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                var services = new List<InsuranceNetworkService>();

                await csv.ReadAsync();
                csv.ReadHeader();

                while (await csv.ReadAsync())
                {
                    var providerNameInFile = csv.GetField(9)?.Trim();
                    // البحث عن الشركة بالاسم (تجاهل حالة الأحرف والمسافات)
                    var provider = existingProviders.FirstOrDefault(p =>
                        p.Name.Trim().Equals(providerNameInFile, StringComparison.OrdinalIgnoreCase));

                    services.Add(new InsuranceNetworkService
                    {
                        Name = csv.GetField(0)?.Trim(),
                        Type = csv.GetField(1)?.Trim(),
                        Code = csv.GetField(2)?.Trim(),
                        Latitude = double.TryParse(csv.GetField(3), out var lat) ? lat : null,
                        Longitude = double.TryParse(csv.GetField(4), out var lng) ? lng : null,
                        Address = csv.GetField(5)?.Trim(),
                        Phone = csv.GetField(6)?.Trim(),
                        Governorate = csv.GetField(7)?.Trim(),
                        Area = csv.GetField(8)?.Trim(),
                        InsuranceProviderName = providerNameInFile,
                        InsuranceProviderId = provider?.Id, // الربط بالـ ID أوتوماتيكياً
                        OpenFrom = new TimeSpan(8, 0, 0),
                        OpenTo = new TimeSpan(22, 0, 0)
                    });
                }

                _context.InsuranceNetworkServices.AddRange(services);
                await _context.SaveChangesAsync();

                return Ok(new { message = $"تم رفع {services.Count} خدمة طبية بنجاح، وتم ربط {services.Count(s => s.InsuranceProviderId != null)} منها بشركات التأمين المسجلة." });
            }
            catch (Exception ex) { return StatusCode(500, $"خطأ تقني: {ex.Message}"); }
        }
    }
}