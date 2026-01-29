using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

namespace Salamaty.API.DTOs
{
    public class GoogleLoginDto
    {
        [Required(ErrorMessage = "Google token is required.")]
        [SwaggerSchema(Description = "Google OAuth token")]
        public string Token { get; set; } = null!;
    }
}
