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

            var request = _httpContextAccessor.HttpContext?.Request;
            var baseUrl = request != null ? $"{request.Scheme}://{request.Host}" : "http://salamaty.runasp.net";

            // مسار الصورة الافتراضية للأدوية التي تفتقر لصورة
            string defaultImagePath = "medicine_images/images.jpg";

            // خط دفاع Production لضمان سحب قائمة الأدوية بشكل مستقل
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

                // 2. طلب الـ AI Scan بوقت مفتوح ومقاوم للـ Timeout أونلاين
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

                // 3. فلترة مرنة (Confidence >= 50%) لضمان التقاط خط الروشتة كاملاً
                var rawMedicines = aiResult.Medicines
                    .Where(m => m.FinalConfidence >= 50 && !string.IsNullOrWhiteSpace(m.MatchedDrug))
                    .Select(m => m.MatchedDrug!.Trim())
                    .Distinct()
                    .ToList();

                if (!rawMedicines.Any()) return finalResult;

                finalResult.ExtractedMedicines = rawMedicines;

                // 4. المطابقة المحلية المباشرة وفصل المتاح عن غير المتاح
                foreach (var aiOriginalName in finalResult.ExtractedMedicines)
                {
                    string cleanAiName = aiOriginalName.ToLower().Replace(" ", "").Trim();

                    // مطابقة دقيقة ومرنة للأحرف والمسافات من الـ Memory
                    var localProduct = allProducts.FirstOrDefault(p =>
                    {
                        string cleanDbName = (p.Name ?? "").ToLower().Replace(" ", "").Trim();
                        return cleanDbName == cleanAiName || cleanAiName.Contains(cleanDbName) || cleanDbName.Contains(cleanAiName);
                    });

                    if (localProduct != null)
                    {
                        // هندلة الصورة: لو الـ ImageUrl متخزن بـ NULL أو فاضي، يركب الـ Default Image
                        string finalLocalImg = string.IsNullOrEmpty(localProduct.ImageUrl)
                            ? $"{baseUrl}/{defaultImagePath}"
                            : $"{baseUrl}/{localProduct.ImageUrl.Replace("\\", "/")}";

                        finalResult.AvailableMedicines.Add(new DetectedMedicineDto
                        {
                            Id = localProduct.Id,
                            Name = localProduct.Name ?? aiOriginalName,
                            Price = localProduct.Price.GetValueOrDefault(),
                            ImageUrl = finalLocalImg,
                            IsAvailable = true
                        });
                    }
                    else
                    {
                        // الدواء ملوش أثر في الداتابيز المحلية ⬅️ يزل فوراً في غير المتاح بشكل نظيف وبدون أصفار
                        finalResult.NotAvailableMedicines.Add(new DetectedMedicineDto
                        {
                            Name = aiOriginalName,
                            IsAvailable = false
                        });
                    }
                }

                // 5. حفظ الهيستوري والروشتات في سياق مستقل تماماً لمنع حدوث أي Rollback للقوائم المستخرجة
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