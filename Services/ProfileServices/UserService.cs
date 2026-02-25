using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Salamaty.API.DTOs.ProfileDTOS;
using Salamaty.API.Models.ProfileModels;

namespace Salamaty.API.Services
{
    public class UserService(UserManager<ApplicationUser> userManager, IHttpContextAccessor httpContextAccessor) : IUserService
    {

        public async Task<object?> GetUserProfileAsync(string userId, string lang = "ar")
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null) return null;

            var request = httpContextAccessor.HttpContext?.Request;
            var baseUrl = $"{request?.Scheme}://{request?.Host}";

            return new
            {
                user.FullName,
                user.Email,
                ImageUrl = string.IsNullOrEmpty(user.ImageUrl) ? null : baseUrl + user.ImageUrl,

                // Gender Logic
                GenderText = user.Gender switch
                {
                    Models.Enums.Gender.Male => "Male",
                    Models.Enums.Gender.Female => "Female",
                    _ => "Gender"
                },
                GenderValue = (int)user.Gender,

                user.Address,
                // الشرط بقى أسهل بكتير: لو null اطبع الكلمة
                BirthDateText = user.BirthDate == null ? "Birthday" : user.BirthDate.Value.ToString("yyyy-MM-dd"),
                user.BirthDate
            };
        }
        public async Task<bool> UpdateProfileAsync(string userId, UserProfileDto dto)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null) return false;

            user.FullName = dto.FullName;
            user.Gender = dto.Gender;
            user.Address = dto.Address;

            // هنسيف القيمة مباشرة (لو فاضية هتبقى null في الداتابيز)
            user.BirthDate = dto.BirthDate;

            var result = await userManager.UpdateAsync(user);
            return result.Succeeded;
        }

        public async Task<string?> UpdateLocationAsync(string userId, double lat, double lng)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null) return null;

            user.LocationLat = lat;
            user.LocationLng = lng;

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "SalamatyApp/1.0");

                // طلب العنوان مع تفاصيل دقيقة وزووم عالي
                var url = $"https://nominatim.openstreetmap.org/reverse?format=jsonv2&lat={lat}&lon={lng}&accept-language=en&addressdetails=1&zoom=18";

                var response = await client.GetFromJsonAsync<JsonElement>(url);

                if (response.ValueKind != JsonValueKind.Undefined && response.TryGetProperty("address", out var addr))
                {
                    // استخراج كافة مكونات العنوان الممكنة
                    var amenity = addr.TryGetProperty("amenity", out var am) ? am.GetString() : ""; // مكان مشهور
                                                                                                    // var road = addr.TryGetProperty("road", out var r) ? r.GetString() : ""; // شارع
                                                                                                    // var neighbourhood = addr.TryGetProperty("neighbourhood", out var n) ? n.GetString() : ""; // حي
                    var suburb = addr.TryGetProperty("suburb", out var s) ? s.GetString() : ""; // منطقة
                    var city = addr.TryGetProperty("city", out var c) ? c.GetString() :
                               (addr.TryGetProperty("town", out var t) ? t.GetString() : ""); // مدينة أو مركز
                    var state = addr.TryGetProperty("state", out var st) ? st.GetString() : ""; // محافظة

                    // تجميع الأجزاء غير الفارغة بذكاء
                    var parts = new[] { amenity, suburb, city, state }
                                .Where(p => !string.IsNullOrWhiteSpace(p))
                                .Distinct();

                    var fullAddress = string.Join(", ", parts);

                    // تعيين العنوان النهائي
                    user.Address = string.IsNullOrWhiteSpace(fullAddress)
                        ? (response.TryGetProperty("display_name", out var dn) ? dn.GetString() : "عنوان غير معروف")
                        : fullAddress;
                }
                else
                {
                    user.Address = response.TryGetProperty("display_name", out var dn) ? dn.GetString() : "تعذر تحديد العنوان";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Location Error]: {ex.Message}");
            }

            await userManager.UpdateAsync(user);
            return user.Address;
        }
        public async Task<string?> UploadPhotoAsync(string userId, IFormFile file)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null) return null;

            var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
            if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var path = Path.Combine(uploads, fileName);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            user.ImageUrl = $"/uploads/{fileName}";
            await userManager.UpdateAsync(user);
            return user.ImageUrl;
        }
    }
}
