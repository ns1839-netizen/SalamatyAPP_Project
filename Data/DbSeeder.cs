using System.Globalization;
using OfficeOpenXml;
using Salamaty.API.Models;
using SalamatyAPI.Models.Enums;
namespace SalamatyAPI.Data
{
    public static class DbSeeder
    {

        private static void SeedProviderLogos(ApplicationDbContext db)
        {
            // A dictionary to map the provider name to its logo URL
            // We use RELATIVE paths here!
            var logoMap = new Dictionary<string, string>
    {
        { "MetLife Egypt", "/logos/insurance/MetLifeEgypt.jpg" },
        { "Misr Life Insurance", "/logos/insurance/image.png" },
        { "مصر هيلث كير", "/logos/insurance/healthCare.jpg" },
        { "Misr Insurance", "/logos/insurance/clubMisrInsurance.png" },
        { "AXA Egypt", "/logos/insurance/AXAEgypt.png" },
        { "Suez Canal Insurance", "/logos/insurance/SuezCanalInsurance.png" }
    };

            var providersToUpdate = db.InsuranceProviders
                .Where(p => string.IsNullOrEmpty(p.LogoUrl)) // Only get providers that don't have a logo yet
                .ToList();

            bool needsSave = false;
            foreach (var provider in providersToUpdate)
            {
                // هنا نقوم بمسح أي مسافات زائدة مخفية جاءت من ملف الإكسيل
                var cleanName = provider.Name?.Trim();

                // ثم نبحث بالاسم النظيف
                if (cleanName != null && logoMap.TryGetValue(cleanName, out var logoUrl))
                {
                    provider.LogoUrl = logoUrl;
                    needsSave = true;
                }
            }

            if (needsSave)
            {
                db.SaveChanges();
            }
        }
        // Called from Program.cs: DbSeeder.Seed(db, env)
        public static void Seed(ApplicationDbContext db, IWebHostEnvironment env)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // 1) Insurance providers + network services from Excel
            if (!db.InsuranceNetworkServices.Any())
            {
                SeedInsuranceNetworkFromExcel(db, env);
            }

            SeedProviderLogos(db); // This will add the logos after the providers are created

            // 1b) Ensure all pharmacies have Codes (runs even if data already exists)
            SeedPharmacyCodes(db);

            // 2) Products + alternatives from Excel (idempotent – safe to call every time)
            SeedProductsAndAlternativesFromExcel(db, env);

            // 2b) Ensure Pharmacies column is populated from Excel (idempotent)
            UpdateProductPharmaciesFromExcel(db, env);

            // 3) Favorites (only once)
            if (!db.Favourites.Any())
            {
                SeedFavorites(db);
            }
        }

        // -----------------------------------------------------------------
        //  INSURANCE NETWORK (providers + services)
        // -----------------------------------------------------------------

        private static void SeedInsuranceNetworkFromExcel(
          ApplicationDbContext db,
            IWebHostEnvironment env)
        {
            var filePath = Path.Combine(env.ContentRootPath, "Data", "SeedData", "insurance_network.xlsx");

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Insurance network Excel file not found at: {filePath}");

            var providersByName = db.InsuranceProviders
                .ToDictionary(p => p.Name.Trim(), StringComparer.OrdinalIgnoreCase);

            var newProviders = new List<InsuranceProvider>();
            var servicesToInsert = new List<InsuranceNetworkService>();

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets[0];
                if (worksheet.Dimension == null)
                    return;

                int rowCount = worksheet.Dimension.End.Row;

                // EXACTLY matches your Excel screenshot:
                // A: facility name
                // B: specialization
                // C: code
                // D: Latitude
                // E: Longitude
                // F: address
                // G: phone
                // H: governorate
                // I: area
                // J: provider (insurance company)
                const int colFacilityName = 1;   // A
                const int colSpecialization = 2; // B
                const int colCode = 3;           // C
                const int colLat = 4;            // D
                const int colLong = 5;           // E
                const int colAddress = 6;        // F
                const int colPhone = 7;          // G
                const int colGov = 8;            // H
                const int colArea = 9;           // I
                const int colProviderName = 10;  // J

                for (int row = 2; row <= rowCount; row++)
                {
                    var facilityName = worksheet.Cells[row, colFacilityName].Text?.Trim();
                    var specialization = worksheet.Cells[row, colSpecialization].Text?.Trim();
                    var code = worksheet.Cells[row, colCode].Text?.Trim();
                    var latText = worksheet.Cells[row, colLat].Text?.Trim();
                    var longText = worksheet.Cells[row, colLong].Text?.Trim();
                    var address = worksheet.Cells[row, colAddress].Text?.Trim();
                    var phone = worksheet.Cells[row, colPhone].Text?.Trim();
                    var governorate = worksheet.Cells[row, colGov].Text?.Trim();
                    var area = worksheet.Cells[row, colArea].Text?.Trim();
                    var providerName = worksheet.Cells[row, colProviderName].Text?.Trim();

                    if (string.IsNullOrWhiteSpace(facilityName) ||
                        string.IsNullOrWhiteSpace(providerName))
                        continue;

                    var fullAddress = string.Join(" - ",
                        new[] { governorate, area, address }
                            .Where(x => !string.IsNullOrWhiteSpace(x)));

                    // Parse latitude/longitude (if empty, stay 0)
                    double latitude = 0, longitude = 0;
                    double.TryParse(latText, NumberStyles.Any, CultureInfo.InvariantCulture, out latitude);
                    double.TryParse(longText, NumberStyles.Any, CultureInfo.InvariantCulture, out longitude);

                    // Provider
                    if (!providersByName.TryGetValue(providerName, out var provider))
                    {
                        provider = new InsuranceProvider
                        {
                            Name = providerName
                        };
                        providersByName[providerName] = provider;
                        newProviders.Add(provider);
                    }

                    // Type
                    var serviceType = GuessServiceType(facilityName, specialization);

                    var service = new InsuranceNetworkService
                    {
                        Name = facilityName,
                        InsuranceProvider = provider,
                        Type = serviceType,
                        Address = fullAddress,
                        Phone = phone,
                        Latitude = latitude,
                        Longitude = longitude,
                        Code = code,
                        OpenFrom = new TimeSpan(0, 0, 0),
                        OpenTo = new TimeSpan(23, 59, 0)
                    };

                    servicesToInsert.Add(service);
                }
            }

            if (newProviders.Any())
                db.InsuranceProviders.AddRange(newProviders);

            if (servicesToInsert.Any())
                db.InsuranceNetworkServices.AddRange(servicesToInsert);

            db.SaveChanges();
        }

        private static InsuranceServiceType GuessServiceType(string? name, string? specialization)
        {
            string text = $"{name} {specialization}".ToLowerInvariant();

            if (text.Contains("صيدلي") || text.Contains("صيدلية") || text.Contains("pharmacy"))
                return InsuranceServiceType.Pharmacy;

            if (text.Contains("معمل") || text.Contains("تحاليل") || text.Contains("تحليل") || text.Contains("lab"))
                return InsuranceServiceType.Lab;

            return InsuranceServiceType.Hospital;
        }

        // -----------------------------------------------------------------
        //  PRODUCTS & ALTERNATIVES
        // -----------------------------------------------------------------

        private static void SeedProductsAndAlternativesFromExcel(
           ApplicationDbContext db,
            IWebHostEnvironment env)
        {
            var filePath = Path.Combine(env.ContentRootPath, "Data", "SeedData", "medicines.xlsx");

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Seed Excel file not found at: {filePath}");

            var productsToInsert = new List<Product>();
            var alternativesMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets[0];
                if (worksheet.Dimension == null)
                    return;

                int rowCount = worksheet.Dimension.End.Row;

                for (int row = 2; row <= rowCount; row++)
                {
                    var name = worksheet.Cells[row, 1].Text?.Trim();
                    var priceText = worksheet.Cells[row, 2].Text?.Trim();
                    var sideEffect = worksheet.Cells[row, 3].Text?.Trim();
                    var desc = worksheet.Cells[row, 4].Text?.Trim();
                    var uses = worksheet.Cells[row, 5].Text?.Trim();
                    var altText = worksheet.Cells[row, 6].Text?.Trim();
                    var category = worksheet.Cells[row, 7].Text?.Trim();
                    var imageUrl = worksheet.Cells[row, 8].Text?.Trim();
                    var pharmacies = worksheet.Cells[row, 9].Text?.Trim();

                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    if (!decimal.TryParse(priceText, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
                        decimal.TryParse(priceText, NumberStyles.Any, CultureInfo.CurrentCulture, out price);

                    string? fullDescription;
                    if (!string.IsNullOrWhiteSpace(desc) && !string.IsNullOrWhiteSpace(uses))
                        fullDescription = $"{desc} Uses: {uses}";
                    else
                        fullDescription = desc ?? uses;

                    bool exists = db.Products.Any(p => p.Name == name);
                    if (!exists)
                    {
                        productsToInsert.Add(new Product
                        {
                            Name = name,
                            Price = price,
                            Category = category ?? string.Empty,
                            ImageUrl = imageUrl ?? string.Empty,
                            Description = fullDescription,
                            SideEffects = sideEffect,
                            Pharmacies = pharmacies
                        });
                    }

                    // Build in-memory map of alternatives from Excel
                    if (!string.IsNullOrWhiteSpace(altText))
                    {
                        var altNames = altText
                            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(a => a.Trim())
                            .Where(a => !string.IsNullOrWhiteSpace(a))
                            .ToList();

                        if (altNames.Any())
                            alternativesMap[name] = altNames;
                    }
                }
            }

            // Insert any new products
            if (productsToInsert.Any())
            {
                db.Products.AddRange(productsToInsert);
                db.SaveChanges();
            }

            var allProducts = db.Products.ToList();

            Product? FindProductByName(string name)
            {
                var exact = allProducts
                    .FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (exact != null) return exact;

                return allProducts.FirstOrDefault(p =>
                    p.Name.Contains(name, StringComparison.OrdinalIgnoreCase) ||
                    name.Contains(p.Name, StringComparison.OrdinalIgnoreCase));
            }

            var existingPairs = db.ProductAlternatives
                .Select(pa => new { pa.ProductId, pa.AlternativeProductId })
                .AsEnumerable()
                .Select(x => (x.ProductId, x.AlternativeProductId))
                .ToHashSet();

            var productAlternatives = new List<ProductAlternative>();

            // Build alternatives graph
            foreach (var kvp in alternativesMap)
            {
                var productName = kvp.Key;
                var altNames = kvp.Value;

                var product = FindProductByName(productName);
                if (product == null)
                    continue;

                foreach (var altName in altNames)
                {
                    var altProduct = FindProductByName(altName);

                    // If the alternative does not exist as a product, create a stub
                    if (altProduct == null)
                    {
                        altProduct = new Product
                        {
                            Name = altName,
                            Price = product.Price,
                            Category = product.Category,
                            ImageUrl = product.ImageUrl,
                            Description = product.Description,
                            SideEffects = product.SideEffects,
                            Pharmacies = product.Pharmacies
                        };

                        db.Products.Add(altProduct);
                        db.SaveChanges();
                        allProducts.Add(altProduct);
                    }

                    // Do not link product to itself
                    if (altProduct.Id == product.Id)
                        continue;

                    var key = (product.Id, altProduct.Id);
                    if (existingPairs.Contains(key))
                        continue;

                    existingPairs.Add(key);

                    productAlternatives.Add(new ProductAlternative
                    {
                        ProductId = product.Id,
                        AlternativeProductId = altProduct.Id
                    });
                }
            }

            if (productAlternatives.Any())
            {
                db.ProductAlternatives.AddRange(productAlternatives);
                db.SaveChanges();
            }
        }

        // -----------------------------------------------------------------
        //  FAVORITES
        // -----------------------------------------------------------------

        private static void SeedFavorites(ApplicationDbContext db)
        {
            if (!db.Products.Any() || db.Favourites.Any())
                return;

            // غيري الـ 1 ليكون نص بين علامات تنصيص "1" 
            // أو استخدمي GUID حقيقي من اللي عندك في جدول AspNetUsers
            const string userId = "1";

            var favoriteProducts = db.Products
                .OrderBy(p => p.Id)
                .Take(3)
                .ToList();

            var favorites = favoriteProducts.Select(p => new Favorite
            {
                UserId = userId, // كدة هتبقى string وموافقة للكلاس
                ProductId = p.Id
            });

            db.Favourites.AddRange(favorites);
            db.SaveChanges();
        }
        // -----------------------------------------------------------------
        //  PHARMACY CODES
        // -----------------------------------------------------------------

        /// <summary>
        /// Fills Code for all pharmacies that don't have one yet.
        /// Generates PH01, PH02, PH03 ... in Id order.
        /// Idempotent: if codes already exist, does nothing.
        /// </summary>
        private static void SeedPharmacyCodes(ApplicationDbContext db)
        {
            var pharmaciesWithoutCode = db.InsuranceNetworkServices
                .Where(s => s.Type == InsuranceServiceType.Pharmacy &&
                            (s.Code == null || s.Code == ""))
                .OrderBy(s => s.Id)
                .ToList();

            if (!pharmaciesWithoutCode.Any())
                return;

            int counter = 1;

            foreach (var pharmacy in pharmaciesWithoutCode)
            {
                pharmacy.Code = $"PH{counter:D2}"; // PH01, PH02, ...
                counter++;
            }

            db.SaveChanges();
        }

        // -----------------------------------------------------------------
        //  UPDATE PRODUCT.PHARMACIES FROM EXCEL
        // -----------------------------------------------------------------

        private static void UpdateProductPharmaciesFromExcel(
          ApplicationDbContext db,
            IWebHostEnvironment env)
        {
            var filePath = Path.Combine(env.ContentRootPath, "Data", "SeedData", "medicines.xlsx");

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Seed Excel file not found at: {filePath}");

            using var package = new ExcelPackage(new FileInfo(filePath));
            var worksheet = package.Workbook.Worksheets[0];
            if (worksheet.Dimension == null)
                return;

            int rowCount = worksheet.Dimension.End.Row;

            var productsByName = db.Products
                .ToDictionary(p => p.Name.Trim(), StringComparer.OrdinalIgnoreCase);

            for (int row = 2; row <= rowCount; row++)
            {
                var name = worksheet.Cells[row, 1].Text?.Trim();
                var pharmacies = worksheet.Cells[row, 9].Text?.Trim();

                if (string.IsNullOrWhiteSpace(name))
                    continue;

                if (!productsByName.TryGetValue(name, out var product))
                    continue;

                if (!string.IsNullOrWhiteSpace(pharmacies))
                {
                    product.Pharmacies = pharmacies;
                }
            }

            db.SaveChanges();
        }
    }
}