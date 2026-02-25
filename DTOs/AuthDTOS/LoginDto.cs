using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

namespace Salamaty.API.DTOs.AuthDTOS
{
    public class LoginDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [SwaggerSchema(Description = "User email address")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Password is required.")]
        [SwaggerSchema(Description = "User password")]
        public string Password { get; set; } = null!;
    }
}
