using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Salamaty.API.DTOs.AuthDTOS;
using Salamaty.API.Models.ProfileModels;

namespace Salamaty.API.Services.AuthServices
{
    public class AuthService(
        UserManager<ApplicationUser> userManager,
        IConfiguration config) : IAuthService
    {
        // ================== REGISTER ==================
        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
        {
            var userExists = await userManager.FindByEmailAsync(dto.Email);
            if (userExists != null)
                return new AuthResponseDto { Success = false, Message = "Email already registered." };

            var user = new ApplicationUser
            {
                Email = dto.Email,
                UserName = dto.Email,
                FullName = dto.FullName,
                EmailConfirmed = false // إجباري: الحساب غير مفعل عند الإنشاء
            };

            var result = await userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Registration failed.",
                    Errors = result.Errors.Select(e => e.Description).ToList()
                };
            }

            return new AuthResponseDto { Success = true, Message = "Registration successful. Verify OTP.", IsEmailConfirmed = false };
        }

        // ================== LOGIN  ==================
        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var user = await userManager.FindByEmailAsync(dto.Email);
            if (user == null || !await userManager.CheckPasswordAsync(user, dto.Password))
                return new AuthResponseDto { Success = false, Message = "Invalid email or password." };

            // منع الدخول إذا لم يتم تفعيل الحساب
            if (!user.EmailConfirmed)
                return new AuthResponseDto { Success = false, Message = "Please verify your email first.", IsEmailConfirmed = false };

            return await GenerateTokenAsync(user);
        }

        // ================== VERIFY OTP  ==================
        public async Task<AuthResponseDto> VerifyOtpAsync(OtpDto dto)
        {
            var user = await userManager.FindByEmailAsync(dto.Email);
            if (user == null) return new AuthResponseDto { Success = false, Message = "User not found." };

            if (user.OtpCode == dto.OtpCode && user.OtpExpiry > DateTime.UtcNow)
            {
                user.EmailConfirmed = true; // تفعيل الحساب في قاعدة البيانات
                // ملاحظة للـ Lead: نترك الـ OtpCode لو نحتاجه للـ Reset Password أو نمسحه هنا
                await userManager.UpdateAsync(user);

                return await GenerateTokenAsync(user); // إرجاع توكن للدخول الفوري
            }

            return new AuthResponseDto { Success = false, Message = "Invalid or expired OTP." };
        }
        // ================== FORGOT PASSWORD ==================

        public async Task<AuthResponseDto> ForgotPasswordAsync(string email)
        {
            var user = await userManager.FindByEmailAsync(email);
            // لو مش موجود، هنقول الحقيقة عشان نوقف الـ Navigation في الـ Front
            if (user == null)
                return new AuthResponseDto { Success = false, Message = "This email is not registered." };

            var otp = new Random().Next(10000, 99999).ToString();
            user.OtpCode = otp;
            user.OtpExpiry = DateTime.UtcNow.AddMinutes(10);
            await userManager.UpdateAsync(user);
            return new AuthResponseDto { Success = true, Message = "OTP sent successfully.", Email = email };
        }

        // ================== RESEND OTP ==================
        public async Task<AuthResponseDto> ResendOtpAsync(string email)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
                return new AuthResponseDto { Success = false, Message = "User not found." };
            // منع الدخول إذا لم يتم تفعيل الحساب
            if (!user.EmailConfirmed)
                return new AuthResponseDto { Success = false, Message = "Please verify your email first.", IsEmailConfirmed = false };

            var newOtp = new Random().Next(10000, 99999).ToString();
            user.OtpCode = newOtp;
            user.OtpExpiry = DateTime.UtcNow.AddMinutes(10);

            await userManager.UpdateAsync(user);
            return new AuthResponseDto { Success = true, Message = "New OTP sent." };
        }
        // ================== RESET PASSWORD ==================
        public async Task<AuthResponseDto> ResetPasswordAsync(ResetPasswordDto dto)
        {
            var user = await userManager.FindByEmailAsync(dto.Email);
            if (user == null) return new AuthResponseDto { Success = false, Message = "User not found." };

            // التأكد من الكود قبل تغيير الباسورد
            if (user.OtpCode != dto.OtpCode || user.OtpExpiry < DateTime.UtcNow)
                return new AuthResponseDto { Success = false, Message = "Invalid or expired OTP." };

            var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
            var result = await userManager.ResetPasswordAsync(user, resetToken, dto.NewPassword);

            if (result.Succeeded)
            {
                user.OtpCode = null; // مسح الكود بعد الاستخدام للأمان
                user.OtpExpiry = null;
                user.EmailConfirmed = true;
                await userManager.UpdateAsync(user);

                return new AuthResponseDto { Success = true, Message = "Password reset successfully.", IsEmailConfirmed = true };
            }

            return new AuthResponseDto { Success = false, Message = "Update failed.", Errors = result.Errors.Select(e => e.Description).ToList() };
        }

        // ================== GENERATE TOKEN ==================
        public async Task<AuthResponseDto> GenerateTokenAsync(ApplicationUser user)
        {
            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.FullName ?? ""),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            var userRoles = await userManager.GetRolesAsync(user);
            foreach (var role in userRoles) authClaims.Add(new Claim(ClaimTypes.Role, role));

            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["JWT:Key"]!));

            var token = new JwtSecurityToken(
                issuer: config["JWT:ValidIssuer"],
                audience: config["JWT:ValidAudience"],
                expires: DateTime.UtcNow.AddDays(1),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return new AuthResponseDto
            {
                Success = true,
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                FullName = user.FullName,
                Email = user.Email,
                IsEmailConfirmed = user.EmailConfirmed
            };
        }

        // ================== GOOGLE LOGIN ==================
        public async Task<AuthResponseDto> GoogleLoginAsync(GoogleLoginDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Token))
                return new AuthResponseDto { Success = false, Message = "Token missing." };

            try
            {

                var webClientId = config["Authentication:Google:ClientId"];
                var settings = new GoogleJsonWebSignature.ValidationSettings()
                {
                    Audience = new List<string> { webClientId! }
                };

                // التحقق من صحة التوكن
                var payload = await GoogleJsonWebSignature.ValidateAsync(dto.Token, settings);

                var user = await userManager.FindByEmailAsync(payload.Email);
                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = payload.Email,
                        Email = payload.Email,
                        FullName = payload.Name,
                        EmailConfirmed = true
                    };
                    await userManager.CreateAsync(user);
                }

                return await GenerateTokenAsync(user);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Google Auth Error: {ex.Message}");
                return new AuthResponseDto { Success = false, Message = "Security check failed. Please select an account." };
            }
        }
        // ================== DELETE ACCOUNT ==================
        public async Task<AuthResponseDto> DeleteAccountAsync(string userId)
        {
            // 1. البحث عن المستخدم بالـ ID
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
                return new AuthResponseDto { Success = false, Message = "User not found." };

            // 2. مسح المستخدم نهائياً
            var result = await userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                return new AuthResponseDto { Success = true, Message = "Account deleted successfully." };
            }

            // 3. لو فيه أخطاء (مثلاً قيود في قاعدة البيانات) نرجعها
            return new AuthResponseDto
            {
                Success = false,
                Message = "Delete failed.",
                Errors = result.Errors.Select(e => e.Description).ToList()
            };
        }
    }
}