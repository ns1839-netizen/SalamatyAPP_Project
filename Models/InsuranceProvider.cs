namespace SalamatyAPI.Models
{
    public class InsuranceProvider
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;
        public string? LogoUrl { get; set; }
        //public string? Description { get; set; }

        public ICollection<InsuranceProfile> InsuranceProfiles { get; set; } =
            new List<InsuranceProfile>();

        public ICollection<InsuranceNetworkService> NetworkServices { get; set; } =
            new List<InsuranceNetworkService>();
    }
}
