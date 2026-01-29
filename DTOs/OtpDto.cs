using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

namespace Salamaty.API.DTOs
{
    public class OtpDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [SwaggerSchema(Description = "User email to verify OTP")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "OtpCode is required.")]
        [RegularExpression(@"^\d{5}$", ErrorMessage = "OTP must be 5 digits.")]
        [SwaggerSchema(Description = "One Time Password sent to email")]
        public string OtpCode { get; set; } = null!;
    }
}
