namespace SalamatyAPI.Dtos.Insurance
{

    /// <summary>
    /// Used by the "Insurance information" screen.
    /// </summary>
    public class SubmitInsuranceInfoDto
    {
        // From previous screen: selected insurance provider
        public int ProviderId { get; set; }

        // "Add Your Insurance id" text field
        public string CardHolderId { get; set; } = null!;

        // "Insurance Front" image
        public IFormFile? FrontImage { get; set; }

        // "Insurance Back Side" image
        public IFormFile? BackImage { get; set; }
    }
}
