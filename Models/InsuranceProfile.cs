namespace SalamatyAPI.Models
{
    public class InsuranceProfile
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int InsuranceProviderId { get; set; }
        public string CardHolderId { get; set; } = null!;
        //public DateTime? ValidUntil { get; set; }
        public string FrontImagePath { get; set; } = null!;
        public string BackImagePath { get; set; } = null!;
        public User User { get; set; } = null!;
        public InsuranceProvider InsuranceProvider { get; set; } = null!;
    }
}