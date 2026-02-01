using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Salamaty.API.Models.Enums;

namespace Salamaty.API.Models.ProfileModels
{
    public class ApplicationUser : IdentityUser
    {
        // ================== Required fields ==================
        [Required(ErrorMessage = "Full Name is required.")]
        [MaxLength(100, ErrorMessage = "Full Name cannot exceed 100 characters.")]
        public string FullName { get; set; } = null!;

        // ================== Optional fields ==================
        [Url(ErrorMessage = "Image URL must be a valid URL.")]
        public string? ImageUrl { get; set; } = string.Empty;


        [Url(ErrorMessage = "Link must be a valid URL.")]
        public string? Link { get; set; }

        // ================== OTP ==================
        public string? OtpCode { get; set; }
        public DateTime? OtpExpiry { get; set; }

        // ================== Profile fields ==================
        public Gender Gender { get; set; }

        public DateTime? BirthDate { get; set; }

        [MaxLength(200)]
        public string? Address { get; set; }

        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
        public double? LocationLat { get; set; }

        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
        public double? LocationLng { get; set; }
    }
}
