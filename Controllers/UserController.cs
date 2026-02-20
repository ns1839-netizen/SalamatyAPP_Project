using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Salamaty.API.DTOs.ProfileDTOS;
using Salamaty.API.Services; // تأكدي أن هذا الـ Namespace يطابق الـ Service

namespace Salamaty.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController(IUserService userService) : ControllerBase
    {
        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            // شيلنا باراميتر اللغة وثبتناها جوه المناداة
            var profile = await userService.GetUserProfileAsync(GetUserId(), "ar");

            if (profile == null)
                return NotFound(new { success = false, message = "User not found" });

            return Ok(new { success = true, data = profile });
        }

        [HttpPut("EditProfile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UserProfileDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var success = await userService.UpdateProfileAsync(GetUserId(), dto);
            if (success)
                return Ok(new { success = true, message = "تم تحديث الملف الشخصي بنجاح" });

            return BadRequest(new { success = false, message = "حدث خطأ أثناء التحديث" });
        }

        [HttpPatch("update-location")]
        public async Task<IActionResult> UpdateLocation([FromBody] LocationUpdateDto dto)
        {
            var address = await userService.UpdateLocationAsync(GetUserId(), dto.LocationLat, dto.LocationLng);
            return Ok(new { Success = true, Address = address });
        }

        [HttpPost("upload-photo")]
        public async Task<IActionResult> UploadPhoto(IFormFile file)
        {
            var imagePath = await userService.UploadPhotoAsync(GetUserId(), file);
            return imagePath != null ? Ok(new { Success = true, ImageUrl = imagePath }) : BadRequest("Upload failed");
        }
    }
}