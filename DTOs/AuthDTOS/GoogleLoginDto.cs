using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

namespace Salamaty.API.DTOs.AuthDTOS
{
    public class GoogleLoginDto
    {
        [Required(ErrorMessage = "Google token is required.")]
        [SwaggerSchema(Description = "Google OAuth token")]
        public string Token { get; set; } = null!;
    }
}
//"1059381805208-fkh4e4dj97eoulq2bbs8o47tdhg6m943.apps.googleusercontent.com"