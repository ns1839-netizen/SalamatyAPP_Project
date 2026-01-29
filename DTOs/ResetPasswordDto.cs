using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

namespace Salamaty.API.DTOs
{
    public class ResetPasswordDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [SwaggerSchema(Description = "User email address")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "New Password is required.")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&""'#^])[A-Za-z\d@$!%*?&""'#^]{8,}$",
            ErrorMessage = "Password must contain uppercase, lowercase, number, and special character.")]
        [SwaggerSchema(Description = "New password")]
        public string NewPassword { get; set; } = null!;
        [Required(ErrorMessage = "OtpCode is required.")]
        [RegularExpression(@"^\d{5}$", ErrorMessage = "OTP must be 5 digits.")]
        [SwaggerSchema(Description = "One Time Password sent to email")]
        public string OtpCode { get; set; } = null!;

        [Required(ErrorMessage = "Confirm Password is required.")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        [SwaggerSchema(Description = "Confirm new password")]
        public string ConfirmPassword { get; set; } = null!;
    }
}
