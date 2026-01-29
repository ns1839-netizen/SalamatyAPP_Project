using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Salamaty.API.DTOs;
using Salamaty.API.Models;

namespace Salamaty.API.Services
{
    // استخدام الـ Primary Constructor (C# 12) لتبسيط حقن التبعيات
    public class AuthService(
        UserManager<ApplicationUser> userManager,
        IConfiguration config) : IAuthService
    {
        // ================== REGISTER (الاشتراك) ==================
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
                EmailConfirmed = false // الحساب يحتاج تفعيل بالـ OTP لضمان صحة البيانات
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

            return new AuthResponseDto
            {
                Success = true,
                Message = "Registration successful. Please verify your OTP.",
                IsEmailConfirmed = false
            };
        }

        // ================== LOGIN (تسجيل الدخول) ==================
        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var user = await userManager.FindByEmailAsync(dto.Email);
            if (user == null || !await userManager.CheckPasswordAsync(user, dto.Password))
            {
                return new AuthResponseDto { Success = false, Message = "Invalid email or password." };
            }

            // ملاحظة: يتم التحقق من EmailConfirmed في الـ Controller لتوجيه الشاشات
            return await GenerateTokenAsync(user);
        }

        // ================== FORGOT PASSWORD (طلب كود الاستعادة) ==================
        public async Task<AuthResponseDto> ForgotPasswordAsync(string email)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
                return new AuthResponseDto { Success = false, Message = "User not found." };

            var otp = new Random().Next(10000, 99999).ToString();
            user.OtpCode = otp;
            user.OtpExpiry = DateTime.UtcNow.AddMinutes(10);

            await userManager.UpdateAsync(user);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Reset OTP sent successfully.",
                OtpCode = otp, // يُرجع هنا لتسهيل التيست في Swagger
                Email = email
            };
        }

        // ================== VERIFY OTP (التحقق من الكود) ==================
        public async Task<AuthResponseDto> VerifyOtpAsync(OtpDto dto)
        {
            var user = await userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return new AuthResponseDto { Success = false, Message = "User not found." };

            if (user.OtpCode == dto.OtpCode && user.OtpExpiry > DateTime.UtcNow)
            {
                return new AuthResponseDto { Success = true, Message = "OTP verified successfully." };
            }

            return new AuthResponseDto { Success = false, Message = "Invalid or expired OTP." };
        }
        // ================== RESEND OTP (إعادة إرسال الكود) ==================
        public async Task<AuthResponseDto> ResendOtpAsync(string email)
        {
            // 1. البحث عن المستخدم
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
                return new AuthResponseDto { Success = false, Message = "User not found." };

            // 2. التحقق مما إذا كان الحساب مفعلاً بالفعل (لا داعي لإرسال كود جديد)
            if (user.EmailConfirmed)
                return new AuthResponseDto { Success = false, Message = "Email is already verified." };

            // 3. توليد كود جديد وتحديث وقت الصلاحية [00:26]
            var newOtp = new Random().Next(10000, 99999).ToString();
            user.OtpCode = newOtp;
            user.OtpExpiry = DateTime.UtcNow.AddMinutes(10); // وقت جديد للصلاحية

            var result = await userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return new AuthResponseDto
                {
                    Success = true,
                    Message = "A new OTP has been generated and sent.",
                    OtpCode = newOtp, // للتجربة في Swagger
                    Email = user.Email
                };
            }

            return new AuthResponseDto { Success = false, Message = "Failed to resend OTP." };
        }

        // ================== RESET PASSWORD (تعيين باسورد جديدة) ==================
        public async Task<AuthResponseDto> ResetPasswordAsync(ResetPasswordDto dto)
        {
            var user = await userManager.FindByEmailAsync(dto.Email);
            if (user == null) return new AuthResponseDto { Success = false, Message = "User not found." };

            if (user.OtpCode != dto.OtpCode || user.OtpExpiry < DateTime.UtcNow)
            {
                return new AuthResponseDto { Success = false, Message = "Invalid or expired OTP." };
            }

            var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
            var result = await userManager.ResetPasswordAsync(user, resetToken, dto.NewPassword);

            if (result.Succeeded)
            {
                user.OtpCode = null;
                user.OtpExpiry = null;
                user.EmailConfirmed = true; // نعتبر الحساب مفعل طالما استطاع تغيير الباسورد عبر الإيميل
                await userManager.UpdateAsync(user);

                return new AuthResponseDto
                {
                    Success = true,
                    Message = "Password reset successfully. Please login with your new password.",
                    IsEmailConfirmed = true
                };
            }

            return new AuthResponseDto
            {
                Success = false,
                Message = "Password update failed.",
                Errors = result.Errors.Select(e => e.Description).ToList()
            };
        }

        // ================== GENERATE JWT TOKEN (توليد التوكن) ==================
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
            foreach (var role in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, role));
            }

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
                Message = "Success",
                FullName = user.FullName,
                Email = user.Email,
                IsEmailConfirmed = user.EmailConfirmed
            };
        }

        // ================== GOOGLE LOGIN (الدخول بجوجل) ==================
        public async Task<AuthResponseDto> GoogleLoginAsync(GoogleLoginDto dto)
        {
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings()
                {
                    Audience = new[] { config["Authentication:Google:ClientId"] }
                };

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
            catch
            {
                return new AuthResponseDto { Success = false, Message = "Google login failed." };
            }
        }
    }
}