using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

namespace Salamaty.API.DTOs.AuthDTOS
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "Full Name is required.")]
        [MinLength(3, ErrorMessage = "Full Name must be at least 3 characters.")]
        [RegularExpression(@"^[\u0600-\u06FFa-zA-Z\s]+$", ErrorMessage = "Full Name must contain only letters and spaces.")]

        [SwaggerSchema(Description = "User's full name")]
        public string FullName { get; set; } = "Ahmed Mohamed";

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [MinLength(13, ErrorMessage = "Email must be at least 13 characters.")]  //Ali@gmail.com
        [SwaggerSchema(Description = "User email address")]
        public string Email { get; set; } = "user@example.com";

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&""'#^])[A-Za-z\d@$!%*?&""'#^]{8,}$",
            ErrorMessage = "Password must contain uppercase, lowercase, number, and special character.")]
        [SwaggerSchema(Description = "User password")]
        public string Password { get; set; } = "S1f3cF!FHIR?17W0*";

        [Required(ErrorMessage = "Confirm Password is required.")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        [SwaggerSchema(Description = "Confirm the password")]
        public string ConfirmPassword { get; set; } = "S1f3cF!FHIR?17W0*";
    }
}
