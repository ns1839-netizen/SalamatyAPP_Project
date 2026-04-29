using System.Text.Json.Serialization;

namespace Salamaty.API.DTOs.Insurance
{
    public class ScannerResponse
    {
        [JsonPropertyName("data")]
        public ScannerData Data { get; set; }

        [JsonPropertyName("raw_text_debug")]
        public string RawTextDebug { get; set; }
    }

    public class ScannerData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("Valid date")]
        public string ValidDate { get; set; }

        [JsonPropertyName("Policy")]
        public string Policy { get; set; }
    }

    public class UploadInsuranceCardDto
    {
        public int ProviderId { get; set; }
        public string? CardHolderId { get; set; }
        public IFormFile? FrontImage { get; set; }
        public IFormFile? BackImage { get; set; }
    }
}