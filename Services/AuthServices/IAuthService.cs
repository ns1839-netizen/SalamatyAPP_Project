using Salamaty.API.DTOs.AuthDTOS;
using Salamaty.API.Models.ProfileModels;

public interface IAuthService
{

    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task<AuthResponseDto> VerifyOtpAsync(OtpDto dto);
    Task<AuthResponseDto> ForgotPasswordAsync(string email);
    Task<AuthResponseDto> ResendOtpAsync(string email);
    Task<AuthResponseDto> ResetPasswordAsync(ResetPasswordDto dto);
    Task<AuthResponseDto> GoogleLoginAsync(GoogleLoginDto dto);
    Task<AuthResponseDto> GenerateTokenAsync(ApplicationUser user);
    Task<AuthResponseDto> DeleteAccountAsync(string userId);
}
