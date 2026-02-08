using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Salamaty.API.DTOs.ProfileDTOS;
using Salamaty.API.Models.ProfileModels;
using SalamatyAPI.Data;

namespace Salamaty.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    // حل مشكلة Use primary constructor
    public class UserController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : ControllerBase
    {
        private async Task<ApplicationUser?> GetCurrentUserAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return userId == null ? null : await userManager.FindByIdAsync(userId);
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var user = await GetCurrentUserAsync();
            // حل مشكلة Operator '==' cannot be applied
            if (user is null) return Unauthorized();

            return Ok(new
            {
                Success = true,
                Data = new UserProfileDto
                {
                    FullName = user.FullName,
                    ImageUrl = user.ImageUrl,
                    Gender = user.Gender,
                    BirthDate = user.BirthDate,
                    Address = user.Address,
                    LocationLat = user.LocationLat,
                    LocationLng = user.LocationLng
                }
            });
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile(UserProfileDto dto)
        {
            var user = await GetCurrentUserAsync();
            if (user is null) return Unauthorized();

            // تحديث البيانات الأساسية
            user.FullName = dto.FullName;
            user.Gender = dto.Gender;
            user.BirthDate = dto.BirthDate;
            user.Address = dto.Address;

            // هندلة الموقع: إذا رفض المستخدم الإذن، ستصل القيم null وتُحفظ كـ null
            user.LocationLat = dto.LocationLat;
            user.LocationLng = dto.LocationLng;

            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded) return BadRequest(result.Errors);

            return Ok(new { Success = true, Message = "Profile updated successfully" });
        }

        [HttpPost("upload-photo")]
        public async Task<IActionResult> UploadPhoto(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("File is empty");

            var user = await GetCurrentUserAsync();
            if (user is null) return Unauthorized();

            var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
            if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var path = Path.Combine(uploads, fileName);

            using var stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);

            user.ImageUrl = $"/uploads/{fileName}";
            await userManager.UpdateAsync(user);

            return Ok(new { Success = true, ImageUrl = user.ImageUrl });
        }

    }
}