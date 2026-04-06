using System.Globalization;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using Salamaty.API.Models;
using SalamatyAPI.Models.Enums;

namespace SalamatyAPI.Data
{
    public static class DbSeeder
    {
        private static void SeedProviderLogos(ApplicationDbContext db)
        {
            var logoMap = new Dictionary<string, string>
            {
                { "MetLife Egypt", "/logos/insurance/MetLifeEgypt.jpg" },
                { "Misr Life Insurance", "/logos/insurance/image.png" },
                { "Misr HealthCare", "/logos/insurance/healthCare.jpg" },
                { "Misr Insurance", "/logos/insurance/clubMisrInsurance.png" },
                { "AXA Egypt", "/logos/insurance/AXAEgypt.png" },
                { "Suez Canal Insurance", "/logos/insurance/SuezCanalInsurance.png" }
            };

            var providersToUpdate = db.InsuranceProviders
                .Where(p => string.IsNullOrEmpty(p.LogoUrl))
                .ToList();

            bool needsSave = false;
            foreach (var provider in providersToUpdate)
            {
                var cleanName = provider.Name?.Trim();
                if (cleanName != null && logoMap.TryGetValue(cleanName, out var logoUrl))
                {
                    provider.LogoUrl = logoUrl;
                    needsSave = true;
                }
            }

            if (needsSave) db.SaveChanges();
        }

        public static void Seed(ApplicationDbContext db, IWebHostEnvironment env)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // 1. تأمين الشبكة الطبية
            if (!db.InsuranceNetworkServices.Any())
            {
                SeedInsuranceNetworkFromExcel(db, env);
            }

            SeedProviderLogos(db);
            SeedPharmacyCodes(db);

            // 2. المنتجات والبدائل
            SeedProductsAndAlternativesFromExcel(db, env);
            UpdateProductPharmaciesFromExcel(db, env);

            // 3. المفضلات (تم التأكد من مطابقة اسم الجدول Favourites)
            if (!db.Favourites.Any())
            {
                SeedFavorites(db);
            }
        }

        private static void SeedInsuranceNetworkFromExcel(ApplicationDbContext db, IWebHostEnvironment env)
        {
            var filePath = Path.Combine(env.ContentRootPath, "Data", "SeedData", "insurance_network.xlsx");
            if (!File.Exists(filePath)) return;

            var providersByName = db.InsuranceProviders.ToDictionary(p => p.Name.Trim(), StringComparer.OrdinalIgnoreCase);
            var newProviders = new List<InsuranceProvider>();
            var servicesToInsert = new List<InsuranceNetworkService>();

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets[0];
                if (worksheet.Dimension == null) return;

                for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                {
                    var facilityName = worksheet.Cells[row, 1].Text?.Trim();
                    var providerName = worksheet.Cells[row, 10].Text?.Trim();

                    if (string.IsNullOrWhiteSpace(facilityName) || string.IsNullOrWhiteSpace(providerName)) continue;

                    // تحويل اسم "مصر هيلث كير" للإنجليزية إذا وجد في الإكسيل ليتوافق مع اللوجو
                    if (providerName == "مصر هيلث كير") providerName = "Misr HealthCare";

                    if (!providersByName.TryGetValue(providerName, out var provider))
                    {
                        provider = new InsuranceProvider { Name = providerName };
                        providersByName[providerName] = provider;
                        newProviders.Add(provider);
                    }

                    double.TryParse(worksheet.Cells[row, 4].Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double lat);
                    double.TryParse(worksheet.Cells[row, 5].Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double lng);

                    servicesToInsert.Add(new InsuranceNetworkService
                    {
                        Name = facilityName,
                        InsuranceProvider = provider,
                        Type = GuessServiceType(facilityName, worksheet.Cells[row, 2].Text),
                        Address = string.Join(" - ", new[] { worksheet.Cells[row, 8].Text, worksheet.Cells[row, 9].Text, worksheet.Cells[row, 6].Text }.Where(s => !string.IsNullOrWhiteSpace(s))),
                        Phone = worksheet.Cells[row, 7].Text?.Trim(),
                        Latitude = lat,
                        Longitude = lng,
                        Code = worksheet.Cells[row, 3].Text?.Trim(),
                        OpenFrom = TimeSpan.Zero,
                        OpenTo = new TimeSpan(23, 59, 0)
                    });
                }
            }

            if (newProviders.Any()) db.InsuranceProviders.AddRange(newProviders);
            if (servicesToInsert.Any()) db.InsuranceNetworkServices.AddRange(servicesToInsert);
            db.SaveChanges();
        }

        private static void SeedProductsAndAlternativesFromExcel(ApplicationDbContext db, IWebHostEnvironment env)
        {
            var filePath = Path.Combine(env.ContentRootPath, "Data", "SeedData", "medicines.xlsx");
            if (!File.Exists(filePath)) return;

            var existingProductNames = db.Products.AsNoTracking().Select(p => p.Name.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var productsToInsert = new List<Product>();
            var alternativesMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets[0];
                if (worksheet.Dimension == null) return;

                for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                {
                    var name = worksheet.Cells[row, 1].Text?.Trim();
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    if (!existingProductNames.Contains(name))
                    {
                        decimal.TryParse(worksheet.Cells[row, 2].Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var price);

                        productsToInsert.Add(new Product
                        {
                            Name = name,
                            Price = price,
                            Category = worksheet.Cells[row, 7].Text?.Trim() ?? "General",
                            // تجنب خطأ الـ NULL بإضافة صورة افتراضية
                            ImageUrl = string.IsNullOrWhiteSpace(worksheet.Cells[row, 8].Text) ? "/images/products/default.png" : worksheet.Cells[row, 8].Text.Trim(),
                            Description = worksheet.Cells[row, 4].Text?.Trim(),
                            SideEffects = worksheet.Cells[row, 3].Text?.Trim(),
                            Pharmacies = worksheet.Cells[row, 9].Text?.Trim()
                        });
                        existingProductNames.Add(name);
                    }

                    var altText = worksheet.Cells[row, 6].Text?.Trim();
                    if (!string.IsNullOrWhiteSpace(altText))
                    {
                        alternativesMap[name] = altText.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(a => a.Trim()).ToList();
                    }
                }
            }

            if (productsToInsert.Any())
            {
                db.Products.AddRange(productsToInsert);
                db.SaveChanges();
            }

            ProcessAlternatives(db, alternativesMap);
        }

        private static void ProcessAlternatives(ApplicationDbContext db, Dictionary<string, List<string>> alternativesMap)
        {
            var allProducts = db.Products.ToList();
            var existingPairs = db.ProductAlternatives.Select(pa => new { pa.ProductId, pa.AlternativeProductId }).AsEnumerable().Select(x => (x.ProductId, x.AlternativeProductId)).ToHashSet();
            var productAlternatives = new List<ProductAlternative>();

            foreach (var kvp in alternativesMap)
            {
                var product = allProducts.FirstOrDefault(p => p.Name.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase));
                if (product == null) continue;

                foreach (var altName in kvp.Value)
                {
                    var altProduct = allProducts.FirstOrDefault(p => p.Name.Equals(altName, StringComparison.OrdinalIgnoreCase));

                    if (altProduct == null)
                    {
                        altProduct = new Product
                        {
                            Name = altName,
                            Category = "Alternative",
                            ImageUrl = "/images/products/default.png", // حماية من خطأ SQL
                            Description = "Alternative medicine",
                            Price = product.Price
                        };
                        db.Products.Add(altProduct);
                        db.SaveChanges();
                        allProducts.Add(altProduct);
                    }

                    if (altProduct.Id != product.Id && !existingPairs.Contains((product.Id, altProduct.Id)))
                    {
                        existingPairs.Add((product.Id, altProduct.Id));
                        productAlternatives.Add(new ProductAlternative { ProductId = product.Id, AlternativeProductId = altProduct.Id });
                    }
                }
            }

            if (productAlternatives.Any())
            {
                db.ProductAlternatives.AddRange(productAlternatives);
                db.SaveChanges();
            }
        }

        private static void SeedFavorites(ApplicationDbContext db)
        {
            // 1. جلب أول مستخدم موجود في قاعدة البيانات (مهما كان الـ ID بتاعه)
            var firstUser = db.Users.FirstOrDefault();

            // إذا لم يكن هناك مستخدمون بعد، لا تفعل شيئاً لتجنب الخطأ
            if (firstUser == null) return;

            var favoriteProducts = db.Products.Take(3).ToList();

            if (!favoriteProducts.Any()) return;

            var favorites = favoriteProducts.Select(p => new Favorite
            {
                UserId = firstUser.Id, // استخدام الـ ID الحقيقي للمستخدم الموجود
                ProductId = p.Id
            });

            db.Favourites.AddRange(favorites);
            db.SaveChanges();
        }

        private static void SeedPharmacyCodes(ApplicationDbContext db)
        {
            var pharmacies = db.InsuranceNetworkServices
                .Where(s => s.Type == InsuranceServiceType.Pharmacy && string.IsNullOrEmpty(s.Code))
                .OrderBy(s => s.Id).ToList();

            int counter = 1;
            foreach (var pharmacy in pharmacies)
            {
                pharmacy.Code = $"PH{counter++:D2}";
            }
            db.SaveChanges();
        }

        private static void UpdateProductPharmaciesFromExcel(ApplicationDbContext db, IWebHostEnvironment env)
        {
            var filePath = Path.Combine(env.ContentRootPath, "Data", "SeedData", "medicines.xlsx");
            if (!File.Exists(filePath)) return;

            using var package = new ExcelPackage(new FileInfo(filePath));
            var worksheet = package.Workbook.Worksheets[0];
            var productsByName = db.Products.ToDictionary(p => p.Name.Trim(), StringComparer.OrdinalIgnoreCase);

            for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                var name = worksheet.Cells[row, 1].Text?.Trim();
                if (name != null && productsByName.TryGetValue(name, out var product))
                {
                    product.Pharmacies = worksheet.Cells[row, 9].Text?.Trim();
                }
            }
            db.SaveChanges();
        }

        private static InsuranceServiceType GuessServiceType(string? name, string? specialization)
        {
            string text = $"{name} {specialization}".ToLowerInvariant();
            if (text.Contains("صيدلي") || text.Contains("pharmacy")) return InsuranceServiceType.Pharmacy;
            if (text.Contains("معمل") || text.Contains("lab")) return InsuranceServiceType.Lab;
            return InsuranceServiceType.Hospital;
        }
    }
}