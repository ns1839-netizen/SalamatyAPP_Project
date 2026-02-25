namespace SalamatyAPI.Models
{
    public class User
    {
       
            public int Id { get; set; }
            public string FullName { get; set; } = null!;
            public string Email { get; set; } = null!;

            public ICollection<InsuranceProfile> InsuranceProfiles { get; set; } = new List<InsuranceProfile>();
        
    }
}
