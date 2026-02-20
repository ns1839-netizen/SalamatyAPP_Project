using Salamaty.API.DTOs.ProfileDTOS;

namespace Salamaty.API.Services
{
    public interface IUserService
    {
        Task<object?> GetUserProfileAsync(string userId, string lang);
        Task<bool> UpdateProfileAsync(string userId, UserProfileDto dto);
        Task<string?> UpdateLocationAsync(string userId, double lat, double lng);
        Task<string?> UploadPhotoAsync(string userId, IFormFile file);
    }
}