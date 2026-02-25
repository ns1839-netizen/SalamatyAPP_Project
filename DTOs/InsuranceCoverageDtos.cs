namespace SalamatyAPI.Dtos.Insurance
{
    public class CoverageListDto
    {
        public bool IsCovered { get; set; }
        public List<string> Items { get; set; } = new();
    }

    public class UserSectionDto
    {
        public string FullName { get; set; } = null!;
        public string CardHolderId { get; set; } = null!;
    }

    public class ProviderSectionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? LogoUrl { get; set; }
        public string PolicyNumber { get; set; } = null!;
        public DateTime? ValidUntil { get; set; }
    }

    public class CoverageSectionDto
    {
        public CoverageListDto Medicines { get; set; } = new();
        public CoverageListDto LabTests { get; set; } = new();
        public CoverageListDto Hospitals { get; set; } = new();
    }

    public class InsuranceProfileDetailsDto
    {
        public UserSectionDto User { get; set; } = null!;
        public ProviderSectionDto Provider { get; set; } = null!;
        public CoverageSectionDto Coverage { get; set; } = null!;
    }
}
