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

//            var request = _httpContextAccessor.HttpContext?.Request;
//            var baseUrl = request != null ? $"{request.Scheme}://{request.Host}" : "http://salamaty.runasp.net";

//            try
//            {
//                // 1. حفظ الصورة محلياً
//                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Prescriptions");
//                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
//                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
//                using (var fileStream = new FileStream(filePath, FileMode.Create)) { await prescriptionImage.CopyToAsync(fileStream); }

//                // 2. طلب الـ AI باستخدام عميل محلي بوقت مخصص لتجنب الـ Timeout التلقائي
//                using var aiClient = new HttpClient();
//                aiClient.Timeout = TimeSpan.FromMinutes(5);

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

//                // 3. الفلترة الذكية وهندسة الـ Confidence (تأمين قراءة الكلمات بنسبة >= 50%)
//                var rawMedicines = aiResult.Medicines
//                    .Where(m => m.FinalConfidence >= 50 && !string.IsNullOrWhiteSpace(m.MatchedDrug))
//                    .Select(m => m.MatchedDrug!.Trim())
//                    .Distinct()
//                    .ToList();

//                if (!rawMedicines.Any()) return finalResult;

//                // وضع الأسماء المستخرجة الصافية في الـ DTO
//                finalResult.ExtractedMedicines = rawMedicines;

//                // سحب قائمة أدوية الداتابيز المحلية كاملة للمطابقة المرنة
//                var allProducts = await _context.Products.ToListAsync();

//                foreach (var aiOriginalName in finalResult.ExtractedMedicines)
//                {
//                    // ✨ هندلة حالة الأحرف والمسافات: تنظيف الاسم تماماً (سمول وبدون مسافات)
//                    string cleanAiName = aiOriginalName.ToLower().Replace(" ", "");

//                    // المطابقة المحلية مع تجاهل المسافات وحالة الأحرف برمجياً برأس مرفوع
//                    var localProduct = allProducts.FirstOrDefault(p =>
//                    {
//                        string cleanDbName = (p.Name ?? "").ToLower().Replace(" ", "");
//                        return cleanDbName == cleanAiName || cleanAiName.Contains(cleanDbName) || cleanDbName.Contains(cleanAiName);
//                    });

//                    if (localProduct != null)
//                    {
//                        // الدواء متاح محلياً بالفعل في السيستم
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
//                        // 4. خطة الإنقاذ: الدواء مش في الداتابيز المحلية ⬅️ يذهب فوراً لـ openFDA لايف
//                        // تنظيف الاسم التجاري من أي تفاصيل تعيق الـ API (مثل المقادير والأوزان: 40mg، cap، syrup)
//                        string searchName = aiOriginalName.Split(' ')[0]
//                            .Replace("0", "").Replace("1", "").Replace("2", "").Replace("3", "")
//                            .Replace("4", "").Replace("5", "").Replace("6", "").Replace("7", "")
//                            .Replace("8", "").Replace("9", "")
//                            .Replace("mg", "", StringComparison.OrdinalIgnoreCase)
//                            .Replace("cap", "", StringComparison.OrdinalIgnoreCase)
//                            .Replace("capsule", "", StringComparison.OrdinalIgnoreCase)
//                            .Replace("syrup", "", StringComparison.OrdinalIgnoreCase)
//                            .Trim();

//                        string externalApiUrl = $"https://api.fda.gov/drug/ndc.json?search=brand_name:\"{Uri.EscapeDataString(searchName)}\"&limit=1";
//                        bool foundExternally = false;

//                        try
//                        {
//                            using var extClient = new HttpClient();
//                            var extResponse = await extClient.GetAsync(externalApiUrl);

//                            if (extResponse.IsSuccessStatusCode)
//                            {
//                                var jsonString = await extResponse.Content.ReadAsStringAsync();
//                                using var extDoc = JsonDocument.Parse(jsonString);
//                                var extRoot = extDoc.RootElement;

//                                // الدخول والتحقق من هيكلة مصفوفة نتائج openFDA المستلمة
//                                if (extRoot.TryGetProperty("results", out var resultsProp) &&
//                                    resultsProp.ValueKind == JsonValueKind.Array &&
//                                    resultsProp.GetArrayLength() > 0)
//                                {
//                                    var firstMatch = resultsProp[0];

//                                    // قراءة اسم البراند والمادة الفعالة مع هندلة الـ Fallback للاسم الأصلي
//                                    string extName = firstMatch.TryGetProperty("brand_name", out var nameProp) ? nameProp.GetString() ?? aiOriginalName : aiOriginalName;
//                                    string genericName = firstMatch.TryGetProperty("generic_name", out var genProp) ? genProp.GetString() ?? "" : "";

//                                    decimal estimatedPrice = 120.00m; // وضع سعر تقديري ثابت بالعملة المحلية

//                                    // حفظ الدواء الجديد في الداتابيز المحلية فوراً (Auto-Caching) ليتعلم السيستم تلقائياً
//                                    var newProduct = new Product
//                                    {
//                                        Name = extName,
//                                        Price = estimatedPrice,
//                                        Description = $"المادة الفعالة: {genericName} (تم جلب البيانات وتوثيقها لايف عبر الـ Global openFDA API)"
//                                    };
//                                    _context.Products.Add(newProduct);
//                                    await _context.SaveChangesAsync();

//                                    // نقله فوراً لقائمة المتاح ليعرض للمريض بكفاءة
//                                    finalResult.AvailableMedicines.Add(new DetectedMedicineDto
//                                    {
//                                        Id = newProduct.Id,
//                                        Name = newProduct.Name,
//                                        Price = newProduct.Price.GetValueOrDefault(),
//                                        ImageUrl = "",
//                                        IsAvailable = true
//                                    });

//                                    foundExternally = true;
//                                }
//                            }
//                        }
//                        catch (Exception extEx)
//                        {
//                            Console.WriteLine($">>>> openFDA Global Fallback Failed for {aiOriginalName}: {extEx.Message}");
//                        }

//                        // 5. إذا لم يعثر عليه محلياً ولا في الموسوعة العالمية ينزل في غير المتاح
//                        if (!foundExternally)
//                        {
//                            finalResult.NotAvailableMedicines.Add(new DetectedMedicineDto
//                            {
//                                Name = aiOriginalName,
//                                IsAvailable = false
//                            });
//                        }
//                    }
//                }

//                // 6. حفظ السجل التاريخي للروشتة المستخرجة للمريض (History)
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
//                catch (Exception dbEx) { Console.WriteLine($">>>> History Save Failed: {dbEx.Message}"); }
//            }
//            catch (Exception ex) { Console.WriteLine($"[Critical Service Error]: {ex.Message}"); }

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

namespace Salamaty.API.Services.PrescriptionServices
{
    // 1. الكلاسات المساعدة (خارج كلاس السيرفيس لسهولة الوصول)
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

        // التعديل الجديد: استقبال الـ final_confidence من الـ AI
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
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PrescriptionService(
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment,
            HttpClient httpClient,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
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

            var request = _httpContextAccessor.HttpContext?.Request;
            var baseUrl = request != null ? $"{request.Scheme}://{request.Host}" : "http://salamaty.runasp.net";

            try
            {
                // 1. حفظ الصورة محلياً
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Prescriptions");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create)) { await prescriptionImage.CopyToAsync(fileStream); }

                // 2. طلب الـ AI
                using var requestContent = new MultipartFormDataContent();
                var imageStream = prescriptionImage.OpenReadStream();
                var streamContent = new StreamContent(imageStream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(prescriptionImage.ContentType);
                requestContent.Add(streamContent, "file", prescriptionImage.FileName);

                var response = await _httpClient.PostAsync(aiUrl, requestContent);
                if (!response.IsSuccessStatusCode) return finalResult;

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var aiResult = await response.Content.ReadFromJsonAsync<AIScanResponse>(options);

                if (aiResult?.Medicines == null) return finalResult;

                // 3. المطابقة المثالية (Exact Match Only) بناءً على FinalConfidence
                // التعديل هنا: استخدام FinalConfidence >= 70
                var cleanAiNames = aiResult.Medicines
                    .Where(m => m.FinalConfidence >= 70 && !string.IsNullOrWhiteSpace(m.MatchedDrug))
                    .Select(m => m.MatchedDrug!.ToLower().Replace(" ", "").Trim())
                    .Distinct().ToList();

                if (!cleanAiNames.Any()) return finalResult;

                // وضع الأسماء الأصلية للعرض (أيضاً بناءً على الشرط الجديد)
                finalResult.ExtractedMedicines = aiResult.Medicines
                    .Where(m => m.FinalConfidence >= 70 && !string.IsNullOrWhiteSpace(m.MatchedDrug))
                    .Select(m => m.MatchedDrug ?? "")
                    .Distinct().ToList();

                // سحب الأدوية للمطابقة
                var allProducts = await _context.Products.ToListAsync();

                var matchedProducts = allProducts.Where(p =>
                {
                    var cleanDbName = (p.Name ?? "").ToLower().Replace(" ", "");
                    return cleanAiNames.Contains(cleanDbName);
                }).Select(p => new DetectedMedicineDto
                {
                    Id = p.Id,
                    Name = p.Name ?? "Unknown",
                    Price = p.Price.GetValueOrDefault(),
                    ImageUrl = string.IsNullOrEmpty(p.ImageUrl)
                               ? ""
                               : $"{baseUrl}/{p.ImageUrl.Replace("\\", "/")}",
                    IsAvailable = true
                }).ToList();

                finalResult.AvailableMedicines = matchedProducts;

                // 5. تحديد غير المتاح بالمطابقة الدقيقة
                finalResult.NotAvailableMedicines = finalResult.ExtractedMedicines
                    .Where(aiOriginal => !matchedProducts.Any(db =>
                        (db.Name ?? "").ToLower().Replace(" ", "") == aiOriginal.ToLower().Replace(" ", "")
                    ))
                    .Select(aiName => new DetectedMedicineDto { Name = aiName, IsAvailable = false })
                    .ToList();

                // 6. الهيستوري
                try
                {
                    var history = new Prescription
                    {
                        UserId = userId,
                        ImagePath = "/Prescriptions/" + uniqueFileName,
                        ScanDate = DateTime.UtcNow,
                        DetectedMedicines = string.Join(", ", finalResult.ExtractedMedicines)
                    };
                    _context.Prescriptions.Add(history);
                    await _context.SaveChangesAsync();
                }
                catch (Exception dbEx) { Console.WriteLine($">>>> History Save Failed: {dbEx.Message}"); }
            }
            catch (Exception ex) { Console.WriteLine($"[Critical Service Error]: {ex.Message}"); }

            return finalResult;
        }
    }
}

