using System.Globalization;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using Salamaty.API.Models;

namespace SalamatyAPI.Data
{
    public static class DbSeeder
    {
        public static void Seed(ApplicationDbContext db, IWebHostEnvironment env)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // 1. تأمين الشبكة الطبية (المستشفيات، المعامل، الصيدليات)
            if (!db.InsuranceNetworkServices.Any())
            {
                SeedInsuranceNetworkFromExcel(db, env);
            }

            SeedProviderLogos(db);
            SeedPharmacyCodes(db); // استدعاء ميثود ترقيم الصيدليات

            // 2. المنتجات والبدائل
            SeedProductsAndAlternativesFromExcel(db, env);

            // 3. المفضلات
            if (!db.Favourites.Any())
            {
                SeedFavorites(db);
            }
        }

        private static void SeedInsuranceNetworkFromExcel(ApplicationDbContext db, IWebHostEnvironment env)
        {
            var filePath = Path.Combine(env.ContentRootPath, "Data", "SeedData", "insurance_network.csv");

            if (!File.Exists(filePath))
            {
                filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "SeedData", "insurance_network.csv");
            }

            if (!File.Exists(filePath)) return;

            var servicesToInsert = new List<InsuranceNetworkService>();

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets[0];
                if (worksheet.Dimension == null) return;

                for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                {
                    var name = worksheet.Cells[row, 1].Text?.Trim();
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    // تحويل الإحداثيات من نص إلى double? مع معالجة القيم الفارغة
                    double? lat = double.TryParse(worksheet.Cells[row, 4].Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var l) ? l : (double?)null;
                    double? lng = double.TryParse(worksheet.Cells[row, 5].Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var g) ? g : (double?)null;

                    servicesToInsert.Add(new InsuranceNetworkService
                    {
                        Name = name,                                        // Column 1
                        Code = worksheet.Cells[row, 2].Text?.Trim(),        // Column 2
                        Type = worksheet.Cells[row, 3].Text?.Trim(),        // Column 3 (Specialization)
                        Latitude = lat,                                     // Column 4 (Double)
                        Longitude = lng,                                    // Column 5 (Double)
                        Address = worksheet.Cells[row, 6].Text?.Trim(),     // Column 6
                        Phone = worksheet.Cells[row, 7].Text?.Trim(),       // Column 7
                        Governorate = worksheet.Cells[row, 8].Text?.Trim(), // Column 8
                        Area = worksheet.Cells[row, 9].Text?.Trim(),        // Column 9
                        InsuranceProviderName = worksheet.Cells[row, 10].Text?.Trim(), // Column 10
                        OpenFrom = TimeSpan.Zero,
                        OpenTo = new TimeSpan(23, 59, 59)
                    });
                }
            }

            if (servicesToInsert.Any())
            {
                db.InsuranceNetworkServices.AddRange(servicesToInsert);
                db.SaveChanges();

                // الربط التلقائي بالـ ID بعد إتمام الإدخال
                db.Database.ExecuteSqlRaw(@"
                    UPDATE InsuranceNetworkServices
                    SET InsuranceProviderId = P.Id
                    FROM InsuranceNetworkServices S
                    JOIN InsuranceProviders P ON S.InsuranceProviderName = P.Name
                    WHERE S.InsuranceProviderId IS NULL AND S.InsuranceProviderName IS NOT NULL AND S.InsuranceProviderName <> ''");
            }
        }

        private static void SeedProviderLogos(ApplicationDbContext db)
        {
            var logoMap = new Dictionary<string, string>
            {
                { "MetLife Egypt", "/logos/insurance/MetLifeEgypt.jpg" },
                { "Misr Life Insurance", "/logos/insurance/image.png" },
                { "AXA Egypt", "/logos/insurance/AXAEgypt.png" },
                { "Misr Insurance", "/logos/insurance/clubMisrInsurance.png" },
                { "Suez Canal Insurance", "/logos/insurance/SuezCanalInsurance.png" },
                { "Misr HealthCare", "/logos/insurance/healthCare.jpg" }
            };

            var providers = db.InsuranceProviders.ToList();
            foreach (var provider in providers)
            {
                if (logoMap.TryGetValue(provider.Name.Trim(), out var logoUrl))
                {
                    provider.LogoUrl = logoUrl;
                }
            }
            db.SaveChanges();
        }

        private static void SeedPharmacyCodes(ApplicationDbContext db)
        {
            // المقارنة بالنص لضمان التوافق مع قاعدة البيانات الجديدة
            var pharmacies = db.InsuranceNetworkServices
                .Where(s => s.Type.ToLower() == "pharmacy" || s.Type.ToLower() == "pharmacies")
                .Where(s => string.IsNullOrEmpty(s.Code))
                .ToList();

            int counter = 1;
            foreach (var pharmacy in pharmacies)
            {
                pharmacy.Code = $"PH{counter++:D3}";
            }
            db.SaveChanges();
        }

        private static void SeedProductsAndAlternativesFromExcel(ApplicationDbContext db, IWebHostEnvironment env)
        {
            // كود الأدوية الخاص بكِ هنا
        }

        private static void SeedFavorites(ApplicationDbContext db)
        {
            // كود المفضلات الخاص بكِ هنا
        }
    }
}