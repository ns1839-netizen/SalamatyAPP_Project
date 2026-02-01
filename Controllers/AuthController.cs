using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Salamaty.API.DTOs.AuthDTOS;
using Salamaty.API.Models.ProfileModels;
using Salamaty.API.Services.AuthServices;

namespace Salamaty.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        IAuthService _authService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        IEmailService _emailService;

        public AuthController(
            IAuthService authService,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailService emailService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        }

        // ================== REGISTER (With Real Email Service) ==================
        [HttpPost("Sign-UP")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // 1. تنفيذ عملية التسجيل الأساسية (إنشاء اليوزر في قاعدة البيانات)
            var result = await _authService.RegisterAsync(dto);
            if (!result.Success) return BadRequest(result);

            // 2. البحث عن المستخدم لتوليد كود التفعيل
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user != null)
            {
                var otpCode = new Random().Next(10000, 99999).ToString();
                user.OtpCode = otpCode;
                user.OtpExpiry = DateTime.UtcNow.AddMinutes(10);
                await _userManager.UpdateAsync(user);

                // 3. إرسال الإيميل الحقيقي بتنسيق احترافي
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

            // التأكد من صحة الكود وصلاحيته
            if (user.OtpCode == dto.OtpCode && user.OtpExpiry.HasValue && user.OtpExpiry > DateTime.UtcNow)
            {
                // 1. تفعيل الحساب في كل الأحوال (سواء كان لسه مسجل أو بيغير باسورد)
                user.EmailConfirmed = true;

                // ملاحظة: لا نمسح الـ OtpCode هنا فوراً 
                // لأننا سنحتاجه في خطوة الـ Reset-Password للتأكد من هوية المستخدم
                await _userManager.UpdateAsync(user);

                // 2. توليد توكن (هينفع مبرمج الفلوتر لو كان اليوزر بيسجل لأول مرة وعايز يدخل الـ Home)
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
            // تأكدي إن authService محقون صح في الكلاس
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



        // ==================  FORGOT PASSWORD  ==================
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] EmailRequestDto dto)
        {
            // 1. التحقق من أن الحقل ليس فارغاً (Validation)
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // 2. طلب تنفيذ المنطق من الـ Service
            var result = await _authService.ForgotPasswordAsync(dto.Email);

            // 3. حالة الفشل: لو الإيميل مش موجود أو غير مسجل
            // هنا بنرجع BadRequest عشان الموبايل يظهر رسالة الخطأ ويفضل في نفس الصفحة
            if (!result.Success)
                return BadRequest(result);

            // 4. حالة النجاح: نجلب المستخدم لإرسال الكود له
            var user = await _userManager.FindByEmailAsync(dto.Email);

            // 5. إرسال الإيميل الحقيقي
            await _emailService.SendEmailAsync(dto.Email, "Salamaty - Reset Password", $"Your reset code is: <b>{user!.OtpCode}</b>");

            // 6. نرجع Ok عشان الموبايل يفهم إن العملية نجحت وينقل المستخدم لشاشة الـ OTP
            return Ok(result);
        }


        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // 1. التأكد من أن المستخدم موجود والكود لسه صح (أمان إضافي)
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null || user.OtpCode != dto.OtpCode)
                return BadRequest(new { Success = false, Message = "Invalid session. Please start over." });

            // 2. تغيير الباسورد
            var result = await _authService.ResetPasswordAsync(dto);
            if (!result.Success) return BadRequest(result);

            // 3. الخطوة الأهم: تصفير الكود بعد النجاح عشان محدش يستخدمه تاني
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


        // ================== Delet Account ==================

        [HttpDelete("delete-account")]
        [Authorize]
        public async Task<IActionResult> DeleteAccount()
        {
            // 1. جلب الـ ID الخاص بالمستخدم من التوكن (Token Claims)
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            // 2. البحث عن المستخدم في قاعدة البيانات
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound(new { Success = false, Message = "User not found." });

            // 3. تنفيذ عملية المسح
            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                return Ok(new { Success = true, Message = "Your account has been permanently deleted." });
            }

            return BadRequest(new { Success = false, Message = "Failed to delete account.", Errors = result.Errors.Select(e => e.Description) });
        }
    }
}