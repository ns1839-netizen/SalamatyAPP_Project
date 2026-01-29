using Salamaty.API.DTOs;
using Salamaty.API.Models;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task<AuthResponseDto> GoogleLoginAsync(GoogleLoginDto dto);
    Task<AuthResponseDto> ForgotPasswordAsync(string email);
    Task<AuthResponseDto> VerifyOtpAsync(OtpDto dto);
    Task<AuthResponseDto> ResetPasswordAsync(ResetPasswordDto dto);
    Task<AuthResponseDto> GenerateTokenAsync(ApplicationUser user);
    Task<AuthResponseDto> ResendOtpAsync(string email);
}
