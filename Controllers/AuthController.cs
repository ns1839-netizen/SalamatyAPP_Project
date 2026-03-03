using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Salamaty.API.DTOs.AuthDTOS;
using Salamaty.API.Models.ProfileModels;
using Salamaty.API.Services.AuthServices;
using SalamatyAPI.Data; // تأكدي إن ده مسار الـ DbContext الصح عندك

namespace Salamaty.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailService _emailService;
        private readonly ApplicationDbContext _context; // إضافة الـ Context

        public AuthController(
            IAuthService authService,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailService emailService,
            ApplicationDbContext context) // حقن الـ Context هنا
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _context = context ?? throw new ArgumentNullException(nameof(context)); // تعيين الـ Context
        }

        // ================== REGISTER (With Real Email Service) ==================
        [HttpPost("Sign-UP")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _authService.RegisterAsync(dto);
            if (!result.Success) return BadRequest(result);

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user != null)
            {
                var otpCode = new Random().Next(10000, 99999).ToString();
                user.OtpCode = otpCode;
                user.OtpExpiry = DateTime.UtcNow.AddMinutes(10);
                await _userManager.UpdateAsync(user);

                string emailBody = $@"
                <div style='font-family: Arial, sans-serif; border: 1px solid #eee; padding: 20px; border-radius: 10px; max-width: 500px; margin: auto;'>
                    <h2 style='color: #2D89EF; text-align: center;'>Welcome to Salamaty!</h2>
                    <p>Please use the following code to verify your account:</p>
                    <div style='background: #f4f4f4; padding: 15px; text-align: center; font-size: 24px; font-weight: bold; letter-spacing: 5px;'>
                        {otpCode}
                    </div>
                    <p style='color: #777; font-size: 12px; text-align: center; margin-top: 20px;'>This code expires in 10 minutes.</p>
                </div>";

                await _emailService.SendEmailAsync(user.Email!, "Salamaty - Verify Your Account", emailBody);

                return Ok(new AuthResponseDto
                {
                    Success = true,
                    Message = "Account created. Please check your email for the verification code.",
                    IsEmailConfirmed = false,
                    Email = user.Email
                });
            }

            return BadRequest(new { Success = false, Message = "An error occurred during registration." });
        }

        // ================== VERIFY OTP (Unified for Registration & Reset) ==================
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] OtpDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null) return NotFound(new { Success = false, Message = "User not found." });

            if (user.OtpCode == dto.OtpCode && user.OtpExpiry.HasValue && user.OtpExpiry > DateTime.UtcNow)
            {
                user.EmailConfirmed = true;
                await _userManager.UpdateAsync(user);

                var authResult = await _authService.GenerateTokenAsync(user);



                return Ok(new
                {
                    Success = true,
                    Message = "Code verified successfully!",
                    Token = authResult.Token,
                    Data = new { Email = user.Email, FullName = user.FullName, IsEmailConfirmed = true }
                });
            }

            return BadRequest(new { Success = false, Message = "Invalid or expired OTP code." });
        }

        // ================== LOGIN ==================
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);

            if (user != null && !user.EmailConfirmed)
            {
                return Ok(new AuthResponseDto
                {
                    Success = false,
                    Message = "Account not verified. Please enter the OTP.",
                    IsEmailConfirmed = false,
                    Email = user.Email
                });
            }

            var result = await _authService.LoginAsync(dto);
            if (!result.Success) return Unauthorized(result);

            return Ok(result);
        }

        // ================== GOOGLE LOGIN ==================
        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto dto)
        {
            var result = await _authService.GoogleLoginAsync(dto);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        // ================== RESEND OTP ==================
        [HttpPost("resend-otp")]
        public async Task<IActionResult> ResendOtp([FromBody] EmailRequestDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null) return NotFound(new { Message = "User not found" });

            var newOtp = new Random().Next(10000, 99999).ToString();
            user.OtpCode = newOtp;
            user.OtpExpiry = DateTime.UtcNow.AddMinutes(7);

            await _userManager.UpdateAsync(user);
            await _emailService.SendEmailAsync(user.Email!, "Salamaty - New OTP Code", $"Your new code is: <b>{newOtp}</b>");

            return Ok(new
            {
                Email = user.Email,
                FullName = user.FullName,
                Message = "A new OTP has been generated and sent to your email.",
                Success = true
            });
        }

        // ================== FORGOT PASSWORD ==================
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] EmailRequestDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _authService.ForgotPasswordAsync(dto.Email);
            if (!result.Success) return BadRequest(result);

            var user = await _userManager.FindByEmailAsync(dto.Email);
            await _emailService.SendEmailAsync(dto.Email, "Salamaty - Reset Password", $"Your reset code is: <b>{user!.OtpCode}</b>");

            return Ok(result);
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null || user.OtpCode != dto.OtpCode)
                return BadRequest(new { Success = false, Message = "Invalid session. Please start over." });

            var result = await _authService.ResetPasswordAsync(dto);
            if (!result.Success) return BadRequest(result);

            user.OtpCode = null;
            user.OtpExpiry = null;
            await _userManager.UpdateAsync(user);

            return Ok(new { Success = true, Message = "Password updated! You can login now." });
        }

        // ================== LOGOUT ==================
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok(new { Message = "Logged out successfully" });
        }

        // ================== Delete Account ==================
        [HttpDelete("delete-account")]
        [Authorize]
        public async Task<IActionResult> DeleteAccount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound(new { Success = false, Message = "User not found." });

            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                return Ok(new { Success = true, Message = "Your account has been permanently deleted." });
            }

            return BadRequest(new { Success = false, Message = "Failed to delete account.", Errors = result.Errors.Select(e => e.Description) });
        }
    }
}