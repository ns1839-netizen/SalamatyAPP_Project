namespace SalamatyAPI.Dtos.Insurance
{
    public class InsuranceProviderDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? LogoUrl { get; set; }
        public string PolicyNumber { get; set; } = null!;   // already there in your Swagger
        public DateTime? ValidUntil { get; set; }
    }
}
