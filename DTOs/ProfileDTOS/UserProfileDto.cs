using System.ComponentModel.DataAnnotations;
using Salamaty.API.Models.Enums;
using Swashbuckle.AspNetCore.Annotations; // لإضافة وصف في Swagger

namespace Salamaty.API.DTOs.ProfileDTOS
{
    public class UserProfileDto
    {
        [Required(ErrorMessage = "Full Name is required.")]
        [MaxLength(100, ErrorMessage = "Full Name cannot exceed 100 characters.")]
        [RegularExpression(@"^[\u0600-\u06FFa-zA-Z\s]+$", ErrorMessage = "Full Name must contain only letters and spaces.")]

        [SwaggerSchema(Description = "The user's legal full name")]
        public string FullName { get; set; } = null!;
        //[EmailAddress] // لإظهاره في البروفايل
        //public string? Email { get; set; }

        //[SwaggerSchema(Description = "URL of the user's profile picture")]
        //public string? ImageUrl { get; set; } // أضفناه ليطابق الواجهة

        [Required(ErrorMessage = "Gender is required.")]
        [SwaggerSchema(Description = "User gender: 1 for Male, 2 for Female")]
        public Gender Gender { get; set; }

        [DataType(DataType.Date)]
        [CustomValidation(typeof(UserProfileDto), nameof(ValidateBirthDate))]
        [SwaggerSchema(Description = "Date of birth (cannot be in the future)")]
        public DateTime? BirthDate { get; set; }

        [MaxLength(200, ErrorMessage = "Address cannot exceed 200 characters.")]
        [SwaggerSchema(Description = "Residential or contact address")]
        public string? Address { get; set; }

        //[Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
        //public double? LocationLat { get; set; }

        //[Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
        //public double? LocationLng { get; set; }

        // ================== Custom Validators ==================
        public static ValidationResult? ValidateBirthDate(DateTime? birthDate, ValidationContext context)
        {
            if (birthDate.HasValue && birthDate.Value > DateTime.UtcNow)
            {
                return new ValidationResult("BirthDate cannot be in the future.");
            }
            return ValidationResult.Success;
        }
    }
}