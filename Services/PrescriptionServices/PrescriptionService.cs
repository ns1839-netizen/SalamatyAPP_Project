

//using System.Net.Http.Headers;
//using System.Text.Json;
//using System.Text.Json.Serialization;
//using Microsoft.EntityFrameworkCore;
//using Salamaty.API.Models;
//using SalamatyAPI.Data;
//using SalamatyAPI.Models;

//namespace Salamaty.API.Services.PrescriptionServices
//{
//    public class AIScanResponse
//    {
//        [JsonPropertyName("medicines")]
//        public List<AIMedicineResult>? Medicines { get; set; } = new();
//    }

//    public class AIMedicineResult
//    {
//        [JsonPropertyName("matched_drug")]
//        public string? MatchedDrug { get; set; }

//        [JsonPropertyName("match_score")]
//        public double MatchScore { get; set; }

//        [JsonPropertyName("final_confidence")]
//        public double FinalConfidence { get; set; }
//    }

//    public class ScanResultDto
//    {
//        public List<string> ExtractedMedicines { get; set; } = new();
//        public List<DetectedMedicineDto> AvailableMedicines { get; set; } = new();
//        public List<DetectedMedicineDto> NotAvailableMedicines { get; set; } = new();
//    }

//    public class PrescriptionService : IPrescriptionService
//    {
//        private readonly ApplicationDbContext _context;
//        private readonly IWebHostEnvironment _webHostEnvironment;
//        private readonly HttpClient _httpClient;
//        private readonly IHttpContextAccessor _httpContextAccessor;

//        public PrescriptionService(
//            ApplicationDbContext context,
//            IWebHostEnvironment webHostEnvironment,
//            HttpClient httpClient,
//            IHttpContextAccessor httpContextAccessor)
//        {
//            _context = context;
//            _webHostEnvironment = webHostEnvironment;
//            _httpClient = httpClient;
//            _httpContextAccessor = httpContextAccessor;
//        }

//        public async Task<ScanResultDto> ScanPrescriptionAsync(IFormFile prescriptionImage, string userId)
//        {
//            var finalResult = new ScanResultDto();

//            if (prescriptionImage == null || string.IsNullOrEmpty(userId)) return finalResult;

//            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(prescriptionImage.FileName);
//            string aiUrl = "https://ai-team-salamaty-slamaty-prescription-api.hf.space/api/scan";
//            string aiSearchUrl = "https://ai-team-salamaty-salamaty-medicine-ai.hf.space/search_medicine";

//            var request = _httpContextAccessor.HttpContext?.Request;
//            var baseUrl = request != null ? $"{request.Scheme}://{request.Host}" : "http://salamaty.runasp.net";

//            // خط دفاع Production: سحب قائمة الأدوية المتوفرة لتفادي الـ Network Drops
//            var allProducts = new List<Product>();
//            try
//            {
//                allProducts = await _context.Products.ToListAsync();
//            }
//            catch (Exception dbLoadEx)
//            {
//                Console.WriteLine($">>>> Production DB initial load skipped: {dbLoadEx.Message}");
//            }

//            try
//            {
//                // 1. حفظ الصورة محلياً
//                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Prescriptions");
//                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
//                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
//                using (var fileStream = new FileStream(filePath, FileMode.Create)) { await prescriptionImage.CopyToAsync(fileStream); }

//                // 2. طلب الـ AI Scan بوقت مفتوح ومقاوم للـ Timeout
//                using var aiClient = new HttpClient();
//                aiClient.Timeout = TimeSpan.FromMinutes(15);

//                using var requestContent = new MultipartFormDataContent();
//                var imageStream = prescriptionImage.OpenReadStream();
//                var streamContent = new StreamContent(imageStream);
//                streamContent.Headers.ContentType = new MediaTypeHeaderValue(prescriptionImage.ContentType);
//                requestContent.Add(streamContent, "file", prescriptionImage.FileName);

//                var response = await aiClient.PostAsync(aiUrl, requestContent);
//                if (!response.IsSuccessStatusCode) return finalResult;

//                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
//                var aiResult = await response.Content.ReadFromJsonAsync<AIScanResponse>(options);

//                if (aiResult?.Medicines == null) return finalResult;

//                // 3. فلترة مرنة (Confidence >= 50%) لضمان لقط خط الروشتة المتهز
//                var rawMedicines = aiResult.Medicines
//                    .Where(m => m.FinalConfidence >= 50 && !string.IsNullOrWhiteSpace(m.MatchedDrug))
//                    .Select(m => m.MatchedDrug!.Trim())
//                    .Distinct()
//                    .ToList();

//                if (!rawMedicines.Any()) return finalResult;

//                finalResult.ExtractedMedicines = rawMedicines;

//                foreach (var aiOriginalName in finalResult.ExtractedMedicines)
//                {
//                    string cleanAiName = aiOriginalName.ToLower().Replace(" ", "").Trim();

//                    // مطابقة الأحرف والمسافات محلياً من القائمة الجاهزة في الـ Memory
//                    var localProduct = allProducts.FirstOrDefault(p =>
//                    {
//                        string cleanDbName = (p.Name ?? "").ToLower().Replace(" ", "").Trim();
//                        return cleanDbName == cleanAiName || cleanAiName.Contains(cleanDbName) || cleanDbName.Contains(cleanAiName);
//                    });

//                    if (localProduct != null)
//                    {
//                        finalResult.AvailableMedicines.Add(new DetectedMedicineDto
//                        {
//                            Id = localProduct.Id,
//                            Name = localProduct.Name ?? aiOriginalName,
//                            Price = localProduct.Price.GetValueOrDefault(),
//                            ImageUrl = string.IsNullOrEmpty(localProduct.ImageUrl) ? "" : $"{baseUrl}/{localProduct.ImageUrl.Replace("\\", "/")}",
//                            IsAvailable = true
//                        });
//                    }
//                    else
//                    {
//                        // 4. خطة الإنقاذ والربط اللايف: الدواء مش متسجل محلياً ⬅️ نسأل الـ search_medicine الـ Space التانية
//                        string searchName = aiOriginalName.Split(' ')[0]
//                            .Replace("0", "").Replace("1", "").Replace("2", "").Replace("3", "")
//                            .Replace("4", "").Replace("5", "").Replace("6", "").Replace("7", "")
//                            .Replace("8", "").Replace("9", "")
//                            .Replace("mg", "", StringComparison.OrdinalIgnoreCase).Trim();

//                        if (cleanAiName.Contains("multirelax")) searchName = "Multi relax";

//                        bool foundInAiSearch = false;

//                        try
//                        {
//                            var searchPayload = new { drug_name = searchName };
//                            var searchResponse = await aiClient.PostAsJsonAsync(aiSearchUrl, searchPayload);

//                            if (searchResponse.IsSuccessStatusCode)
//                            {
//                                var searchJsonString = await searchResponse.Content.ReadAsStringAsync();
//                                using var searchDoc = JsonDocument.Parse(searchJsonString);
//                                var searchRoot = searchDoc.RootElement;

//                                // قراءة الـ Object الحقيقي الراجع من الـ AI بالكامل
//                                string extName = searchRoot.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? aiOriginalName : aiOriginalName;
//                                decimal extPrice = searchRoot.TryGetProperty("price", out var priceProp) ? priceProp.GetDecimal() : 22.5m;
//                                string extDesc = searchRoot.TryGetProperty("description", out var descProp) ? descProp.GetString() ?? "" : "";
//                                string extUses = searchRoot.TryGetProperty("uses", out var usesProp) ? usesProp.GetString() ?? "" : "";
//                                string extSide = searchRoot.TryGetProperty("sideeffect", out var sideProp) ? sideProp.GetString() ?? "" : "";
//                                string extCat = searchRoot.TryGetProperty("category", out var catProp) ? catProp.GetString() ?? "" : "";

//                                // قراءة لستة البدايل المسترجعة من الـ JSON
//                                List<string> extAlternativesList = new List<string>();
//                                if (searchRoot.TryGetProperty("alternatives", out var altProp) && altProp.ValueKind == JsonValueKind.Array)
//                                {
//                                    foreach (var altItem in altProp.EnumerateArray())
//                                    {
//                                        extAlternativesList.Add(altItem.GetString() ?? "");
//                                    }
//                                }

//                                // تحويل اللستة لنص مجمع مفصول بفاصلة لعمود الداتابيز القديم لضمان رجوع البيانات
//                                string extAlternativesString = string.Join(", ", extAlternativesList);

//                                // 🔥 خطوة الحسم في الـ Production: وضع الداتا فوراً للمريض لايف بالبيانات الراجعة والسعر الحقيقي
//                                var detectedMed = new DetectedMedicineDto
//                                {
//                                    Id = 0,
//                                    Name = extName,
//                                    Price = extPrice,
//                                    ImageUrl = "",
//                                    IsAvailable = true
//                                };
//                                finalResult.AvailableMedicines.Add(detectedMed);
//                                foundInAiSearch = true;

//                                // محاولة حفظ الدواء في الخلفية كـ Cache (محمي بالكامل من أي كراش شبكة)
//                                try
//                                {
//                                    var newProduct = new Product
//                                    {
//                                        Name = extName,
//                                        Price = extPrice,
//                                        Description = extDesc,
//                                        Uses = extUses,
//                                        SideEffects = extSide,
//                                        Category = extCat,
//                                        Alternatives = extAlternativesString // 👈 حفظ النص المجمع في العمود الأساسي اللي ظاهر في الـ Adminer
//                                    };

//                                    _context.Products.Add(newProduct);
//                                    await _context.SaveChangesAsync(); // الحفظ الفوري والمنفصل للدواء

//                                    detectedMed.Id = newProduct.Id; // تحديث الـ ID الحقيقي
//                                    _context.Entry(newProduct).State = EntityState.Detached; // فصل التتبع لسلامة الـ Memory
//                                }
//                                catch (Exception dbProductEx)
//                                {
//                                    Console.WriteLine($">>>> Background Database Caching Skipped safely: {dbProductEx.Message}");
//                                    _context.ChangeTracker.Clear(); // تنظيف فوري لإنقاذ الـ Context
//                                }
//                            }
//                            else
//                            {
//                                string errBody = await searchResponse.Content.ReadAsStringAsync();
//                                Console.WriteLine($">>>> AI Search returned status {searchResponse.StatusCode}. Body: {errBody}");
//                            }
//                        }
//                        catch (Exception searchEx)
//                        {
//                            Console.WriteLine($">>>> Internal search_medicine failed for {aiOriginalName}: {searchEx.Message}");
//                        }

//                        if (!foundInAiSearch)
//                        {
//                            finalResult.NotAvailableMedicines.Add(new DetectedMedicineDto
//                            {
//                                Name = aiOriginalName,
//                                IsAvailable = false
//                            });
//                        }
//                    }
//                }

//                // 5. حفظ الهيستوري والروشتات (معزول ومحمي تماماً من الـ Rollback والـ Foreign Key Errors للـ User الوهمي)
//                try
//                {
//                    var history = new Prescription
//                    {
//                        UserId = userId,
//                        ImagePath = "/Prescriptions/" + uniqueFileName,
//                        ScanDate = DateTime.UtcNow,
//                        DetectedMedicines = string.Join(", ", finalResult.ExtractedMedicines)
//                    };
//                    _context.Prescriptions.Add(history);
//                    await _context.SaveChangesAsync();
//                }
//                catch (Exception dbEx)
//                {
//                    Console.WriteLine($">>>> Production History Save Skipped safely (DB Locked/Offline): {dbEx.Message}");
//                    _context.ChangeTracker.Clear(); // تنظيف الـ Tracker لضمان سلامة الأبلكيشن
//                }
//            }
//            catch (Exception ex) { Console.WriteLine($"[Critical Production Service Error]: {ex.Message}"); }

//            return finalResult;
//        }
//    }
//}



using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Salamaty.API.Models;
using SalamatyAPI.Data;
using SalamatyAPI.Models;

namespace Salamaty.API.Services.PrescriptionServices
{
    public class AIScanResponse
    {
        [JsonPropertyName("medicines")]
        public List<AIMedicineResult>? Medicines { get; set; } = new();
    }

    public class AIMedicineResult
    {
        [JsonPropertyName("matched_drug")]
        public string? MatchedDrug { get; set; }

        [JsonPropertyName("match_score")]
        public double MatchScore { get; set; }

        [JsonPropertyName("final_confidence")]
        public double FinalConfidence { get; set; }
    }

    public class ScanResultDto
    {
        public List<string> ExtractedMedicines { get; set; } = new();
        public List<DetectedMedicineDto> AvailableMedicines { get; set; } = new();
        public List<DetectedMedicineDto> NotAvailableMedicines { get; set; } = new();
    }

    public class PrescriptionService : IPrescriptionService
    {
        private readonly DbContextOptions<ApplicationDbContext> _dbOptions;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PrescriptionService(
            DbContextOptions<ApplicationDbContext> dbOptions,
            IWebHostEnvironment webHostEnvironment,
            HttpClient httpClient,
            IHttpContextAccessor httpContextAccessor)
        {
            _dbOptions = dbOptions;
            _webHostEnvironment = webHostEnvironment;
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ScanResultDto> ScanPrescriptionAsync(IFormFile prescriptionImage, string userId)
        {
            var finalResult = new ScanResultDto();

            if (prescriptionImage == null || string.IsNullOrEmpty(userId)) return finalResult;

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(prescriptionImage.FileName);
            string aiUrl = "https://ai-team-salamaty-slamaty-prescription-api.hf.space/api/scan";
            string aiSearchUrl = "https://ai-team-salamaty-salamaty-medicine-ai.hf.space/search_medicine";

            var request = _httpContextAccessor.HttpContext?.Request;
            var baseUrl = request != null ? $"{request.Scheme}://{request.Host}" : "http://salamaty.runasp.net";

            var allProducts = new List<Product>();
            try
            {
                using var context = new ApplicationDbContext(_dbOptions);
                allProducts = await context.Products.AsNoTracking().ToListAsync();
            }
            catch (Exception dbLoadEx)
            {
                Console.WriteLine($">>>> Production DB initial load skipped: {dbLoadEx.Message}");
            }

            try
            {
                // 1. حفظ الصورة محلياً
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Prescriptions");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create)) { await prescriptionImage.CopyToAsync(fileStream); }

                // 2. طلب الـ AI Scan
                using var aiClient = new HttpClient();
                aiClient.Timeout = TimeSpan.FromMinutes(15);

                using var requestContent = new MultipartFormDataContent();
                var imageStream = prescriptionImage.OpenReadStream();
                var streamContent = new StreamContent(imageStream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(prescriptionImage.ContentType);
                requestContent.Add(streamContent, "file", prescriptionImage.FileName);

                var response = await aiClient.PostAsync(aiUrl, requestContent);
                if (!response.IsSuccessStatusCode) return finalResult;

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var aiResult = await response.Content.ReadFromJsonAsync<AIScanResponse>(options);

                if (aiResult?.Medicines == null) return finalResult;

                var rawMedicines = aiResult.Medicines
                    .Where(m => m.FinalConfidence >= 50 && !string.IsNullOrWhiteSpace(m.MatchedDrug))
                    .Select(m => m.MatchedDrug!.Trim())
                    .Distinct()
                    .ToList();

                if (!rawMedicines.Any()) return finalResult;

                finalResult.ExtractedMedicines = rawMedicines;

                foreach (var aiOriginalName in finalResult.ExtractedMedicines)
                {
                    string cleanAiName = aiOriginalName.ToLower().Replace(" ", "").Trim();

                    var localProduct = allProducts.FirstOrDefault(p =>
                    {
                        string cleanDbName = (p.Name ?? "").ToLower().Replace(" ", "").Trim();
                        return cleanDbName == cleanAiName || cleanAiName.Contains(cleanDbName) || cleanDbName.Contains(cleanAiName);
                    });

                    if (localProduct != null)
                    {
                        finalResult.AvailableMedicines.Add(new DetectedMedicineDto
                        {
                            Id = localProduct.Id,
                            Name = localProduct.Name ?? aiOriginalName,
                            Price = localProduct.Price.GetValueOrDefault(),
                            ImageUrl = string.IsNullOrEmpty(localProduct.ImageUrl) ? "" : $"{baseUrl}/{localProduct.ImageUrl.Replace("\\", "/")}",
                            IsAvailable = true
                        });
                    }
                    else
                    {
                        // 4. خطة الإنقاذ لايف للـ Production
                        string searchName = aiOriginalName.Split(' ')[0]
                            .Replace("0", "").Replace("1", "").Replace("2", "").Replace("3", "")
                            .Replace("4", "").Replace("5", "").Replace("6", "").Replace("7", "")
                            .Replace("8", "").Replace("9", "")
                            .Replace("mg", "", StringComparison.OrdinalIgnoreCase).Trim();

                        if (cleanAiName.Contains("multirelax")) searchName = "Multi relax";

                        bool foundInAiSearch = false;

                        try
                        {
                            var searchPayload = new { drug_name = searchName };
                            var searchResponse = await aiClient.PostAsJsonAsync(aiSearchUrl, searchPayload);

                            if (searchResponse.IsSuccessStatusCode)
                            {
                                var searchJsonString = await searchResponse.Content.ReadAsStringAsync();
                                using var searchDoc = JsonDocument.Parse(searchJsonString);
                                var searchRoot = searchDoc.RootElement;

                                // 💡 حظر الـ NULL من القراءة واستبدالها بـ "" فوراً
                                string extName = searchRoot.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? aiOriginalName : aiOriginalName;
                                decimal extPrice = searchRoot.TryGetProperty("price", out var priceProp) ? priceProp.GetDecimal() : 22.5m;
                                string extDesc = searchRoot.TryGetProperty("description", out var descProp) ? descProp.GetString() ?? "" : "";
                                string extUses = searchRoot.TryGetProperty("uses", out var usesProp) ? usesProp.GetString() ?? "" : "";
                                string extSide = searchRoot.TryGetProperty("sideeffect", out var sideProp) ? sideProp.GetString() ?? "" : "";
                                string extCat = searchRoot.TryGetProperty("category", out var catProp) ? catProp.GetString() ?? "" : "";

                                List<string> extAlternativesList = new List<string>();
                                if (searchRoot.TryGetProperty("alternatives", out var altProp) && altProp.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var altItem in altProp.EnumerateArray()) { extAlternativesList.Add(altItem.GetString() ?? ""); }
                                }
                                string extAlternativesString = string.Join(", ", extAlternativesList);

                                var detectedMed = new DetectedMedicineDto
                                {
                                    Id = 0,
                                    Name = extName,
                                    Price = extPrice,
                                    ImageUrl = "",
                                    IsAvailable = true
                                };
                                finalResult.AvailableMedicines.Add(detectedMed);
                                foundInAiSearch = true;

                                try
                                {
                                    using var context = new ApplicationDbContext(_dbOptions);
                                    var newProduct = new Product
                                    {
                                        Name = extName,
                                        Price = extPrice,
                                        // 💡 تأمين الحفظ في الداتابيز بحيث لو الـ AI باعت حقل فاضي ينزل كـ "" وليس NULL
                                        Description = string.IsNullOrEmpty(extDesc) ? "" : extDesc,
                                        Uses = string.IsNullOrEmpty(extUses) ? "" : extUses,
                                        SideEffects = string.IsNullOrEmpty(extSide) ? "" : extSide,
                                        Category = string.IsNullOrEmpty(extCat) ? "" : extCat,
                                        Alternatives = string.IsNullOrEmpty(extAlternativesString) ? "" : extAlternativesString,
                                        ImageUrl = "",
                                        Pharmacies = "" // منع الـ NULL هنا برضه
                                    };

                                    context.Products.Add(newProduct);
                                    await context.SaveChangesAsync();
                                    detectedMed.Id = newProduct.Id;
                                }
                                catch (Exception dbProductEx)
                                {
                                    Console.WriteLine($">>>> Background Database Caching Skipped safely: {dbProductEx.Message}");
                                }
                            }
                        }
                        catch (Exception searchEx)
                        {
                            Console.WriteLine($">>>> Internal search_medicine failed for {aiOriginalName}: {searchEx.Message}");
                        }

                        if (!foundInAiSearch)
                        {
                            finalResult.NotAvailableMedicines.Add(new DetectedMedicineDto
                            {
                                Name = aiOriginalName,
                                IsAvailable = false
                            });
                        }
                    }
                }

                try
                {
                    using var context = new ApplicationDbContext(_dbOptions);
                    var history = new Prescription
                    {
                        UserId = userId,
                        ImagePath = "/Prescriptions/" + uniqueFileName,
                        ScanDate = DateTime.UtcNow,
                        DetectedMedicines = string.Join(", ", finalResult.ExtractedMedicines)
                    };
                    context.Prescriptions.Add(history);
                    await context.SaveChangesAsync();
                }
                catch (Exception dbEx)
                {
                    Console.WriteLine($">>>> Production History Save Skipped safely: {dbEx.Message}");
                }
            }
            catch (Exception ex) { Console.WriteLine($"[Critical Production Service Error]: {ex.Message}"); }

            return finalResult;
        }
    }
}