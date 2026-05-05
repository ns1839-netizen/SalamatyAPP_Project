//using System.Globalization;
//using Microsoft.EntityFrameworkCore;
//using OfficeOpenXml;
//using Salamaty.API.Models;
//using SalamatyAPI.Models;

//namespace SalamatyAPI.Data
//{
//    public static class DbSeeder
//    {
//        public static void Seed(ApplicationDbContext db, IWebHostEnvironment env)
//        {
//            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

//            // 1. رفع الشبكة الطبية (المستشفيات والصيدليات)
//            if (!db.InsuranceNetworkServices.Any())
//            {
//                SeedInsuranceNetwork(db, env);
//            }

//            // 2. رفع الأدوية بكامل بياناتها
//            if (!db.Products.Any())
//            {
//                SeedMedicines(db, env);
//            }

//            SeedProviderLogos(db);
//        }

//        private static void SeedInsuranceNetwork(ApplicationDbContext db, IWebHostEnvironment env)
//        {
//            var filePath = Path.Combine(env.ContentRootPath, "Data", "SeedData", "insurance_network.csv");
//            if (!File.Exists(filePath)) return;

//            var servicesToInsert = new List<InsuranceNetworkService>();
//            using (var reader = new StreamReader(filePath))
//            {
//                reader.ReadLine(); // تخطي الهيدر
//                while (!reader.EndOfStream)
//                {
//                    var line = reader.ReadLine();
//                    if (string.IsNullOrWhiteSpace(line)) continue;

//                    // تقسيم بالفاصلة مع مراعاة النصوص التي تحتوي على فواصل
//                    var values = SplitCsvLine(line);
//                    if (values.Length < 10) continue;

//                    double.TryParse(values[3], NumberStyles.Any, CultureInfo.InvariantCulture, out var lat);
//                    double.TryParse(values[4], NumberStyles.Any, CultureInfo.InvariantCulture, out var lng);

//                    servicesToInsert.Add(new InsuranceNetworkService
//                    {
//                        Name = values[0].Trim('"').Trim(),
//                        Type = values[1].Trim('"').Trim(),
//                        Code = values[2].Trim('"').Trim(),
//                        Latitude = lat != 0 ? lat : null,
//                        Longitude = lng != 0 ? lng : null,
//                        Address = values[5].Trim('"').Trim(),
//                        Phone = values[6].Trim('"').Trim(),
//                        Governorate = values[7].Trim('"').Trim(),
//                        Area = values[8].Trim('"').Trim(),
//                        InsuranceProviderName = values[9].Trim('"').Trim(),
//                        OpenFrom = TimeSpan.Zero,
//                        OpenTo = new TimeSpan(23, 59, 59)
//                    });
//                }
//            }
//            db.InsuranceNetworkServices.AddRange(servicesToInsert);
//            db.SaveChanges();

//            // ربط الـ ProviderId تلقائياً
//            db.Database.ExecuteSqlRaw(@"
//                UPDATE InsuranceNetworkServices
//                SET InsuranceProviderId = P.Id
//                FROM InsuranceNetworkServices S
//                JOIN InsuranceProviders P ON S.InsuranceProviderName = P.Name");
//        }

//        private static void SeedMedicines(ApplicationDbContext db, IWebHostEnvironment env)
//        {
//            var filePath = Path.Combine(env.ContentRootPath, "Data", "SeedData", "medicines.xlsx");
//            if (!File.Exists(filePath)) return;

//            var productsToInsert = new List<Product>();
//            using (var package = new ExcelPackage(new FileInfo(filePath)))
//            {
//                var worksheet = package.Workbook.Worksheets[0];
//                int rowCount = worksheet.Dimension.End.Row;

//                for (int row = 2; row <= rowCount; row++)
//                {
//                    var name = worksheet.Cells[row, 1].Text.Trim();
//                    if (string.IsNullOrEmpty(name)) continue;

//                    decimal.TryParse(worksheet.Cells[row, 2].Text, out var price);

//                    productsToInsert.Add(new Product
//                    {
//                        Name = name,
//                        Price = price,
//                        SideEffects = worksheet.Cells[row, 3].Text.Trim(),
//                        Description = worksheet.Cells[row, 4].Text.Trim(),
//                        Uses = worksheet.Cells[row, 5].Text.Trim(),
//                        Alternatives = worksheet.Cells[row, 6].Text.Trim(),
//                        Category = worksheet.Cells[row, 7].Text.Trim(),
//                        ImageUrl = worksheet.Cells[row, 8].Text.Trim(),
//                        // تخزين أكواد الصيدليات لربطها لاحقاً
//                        PharmacyCodes = worksheet.Cells[row, 9].Text.Trim()
//                    });
//                }
//            }
//            db.Products.AddRange(productsToInsert);
//            db.SaveChanges();
//        }

//        // ميثود ذكية لتقسيم الـ CSV تتعامل مع الفواصل داخل العناوين
//        private static string[] SplitCsvLine(string line)
//        {
//            return line.Split(new[] { ',' }, StringSplitOptions.None);
//        }

//        private static void SeedProviderLogos(ApplicationDbContext db)
//        {
//            var logoMap = new Dictionary<string, string>
//            {
//                { "AXA Egypt", "/logos/insurance/AXAEgypt.png" },
//                { "MetLife Egypt", "/logos/insurance/MetLifeEgypt.jpg" },
//                { "Misr Life Insurance", "/logos/insurance/image.png" },
//                { "Suez Canal Insurance", "/logos/insurance/SuezCanalInsurance.png" },
//                { "Misr Insurance", "/logos/insurance/clubMisrInsurance.png" }
//            };

//            foreach (var provider in db.InsuranceProviders)
//            {
//                if (logoMap.TryGetValue(provider.Name.Trim(), out var logo))
//                    provider.LogoUrl = logo;
//            }
//            db.SaveChanges();
//        }
//    }
//}