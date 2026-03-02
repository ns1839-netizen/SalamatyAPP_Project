using System.ComponentModel.DataAnnotations;
using Salamaty.API.Models.Enums;
using Swashbuckle.AspNetCore.Annotations;

namespace Salamaty.API.DTOs.ProfileDTOS
{
    public class UserProfileDto
    {
        [Required(ErrorMessage = "Full Name is required.")]
        [MaxLength(100, ErrorMessage = "Full Name cannot exceed 100 characters.")]
        [RegularExpression(@"^[\u0600-\u06FFa-zA-Z\s]+$", ErrorMessage = "Full Name must contain only letters and spaces.")]

        [SwaggerSchema(Description = "The user's legal full name")]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "Gender is required.")]
        [SwaggerSchema(Description = "User gender: 1 for Male, 2 for Female")]
        public Gender Gender { get; set; }

        [DataType(DataType.Date)]
        [CustomValidation(typeof(UserProfileDto), nameof(ValidateBirthDate))]
        [SwaggerSchema(Description = "Date of birth (cannot be in the future)")]
        public DateOnly? BirthDate { get; set; }

        [MaxLength(200, ErrorMessage = "Address cannot exceed 200 characters.")]
        [SwaggerSchema(Description = "Residential or contact address")]
        public string? Address { get; set; }


        // ================== Custom Validators ==================
        public static ValidationResult? ValidateBirthDate(DateOnly? birthDate, ValidationContext context)
        {
            // بنجيب تاريخ النهاردة بصيغة DateOnly
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            if (birthDate.HasValue && birthDate.Value > today)
            {
                return new ValidationResult("BirthDate cannot be in the future.");
            }

            return ValidationResult.Success;
        }
    }
}