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
        [JsonPropertyName("customer_name")]
        public string? Name { get; set; }

        [JsonPropertyName("member_id")]
        public string? Id { get; set; }

        [JsonPropertyName("policy_no")]
        public string? Policy { get; set; }

        [JsonPropertyName("expiry_date")]
        public string? ExpiryDate { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }
      
        [JsonPropertyName("insurance_provider")]
        public string? InsuranceProvider { get; set; }
    }

    public class UploadInsuranceCardDto
    {
        public int ProviderId { get; set; }
        public string? CardHolderId { get; set; }
        public IFormFile? FrontImage { get; set; }
        public IFormFile? BackImage { get; set; }
    }
}