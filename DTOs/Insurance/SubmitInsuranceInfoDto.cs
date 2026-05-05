
using System.ComponentModel.DataAnnotations;

namespace SalamatyAPI.Dtos.Insurance
{

    /// <summary>
    /// Used by the "Insurance information" screen.
    /// </summary>
    public class SubmitInsuranceInfoDto
    {
        public int ProviderId { get; set; }

        // 👇 1. CONSTRAINT: Must not be empty
        [Required(ErrorMessage = "Card Holder ID is required.")]

        // 👇 2. CONSTRAINT: Must be between 5 and 14 characters long
        [StringLength(14, MinimumLength = 5, ErrorMessage = "Card Holder ID must be between 5 and 14 characters long.")]

        // 👇 3. CONSTRAINT (Optional): Must be ONLY numbers (no letters or spaces)
        [RegularExpression("^[0-9]*$", ErrorMessage = "Card Holder ID must contain only numbers.")]

        // "Add Your Insurance id" text field
        public string CardHolderId { get; set; } = null!;
        public string? PolicyNumber { get; set; }
        public string? ValidUntil { get; set; }
        public string? Status { get; set; }

        // "Insurance Front" image
        [Required(ErrorMessage = "The Front Image of the insurance card is absolutely required.")]
        public IFormFile? FrontImage { get; set; }

        // "Insurance Back Side" image
        [Required(ErrorMessage = "The Back Image of the insurance card is absolutely required.")]
        public IFormFile? BackImage { get; set; }

        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Name must contain English letters and spaces only.")]
        public string? FullName { get; set; }
    }
}
