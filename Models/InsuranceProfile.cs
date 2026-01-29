using System.ComponentModel.DataAnnotations;

namespace Salamaty.API.Models
{
    public class InsuranceProfile
    {
        [Key]
        public int Id { get; set; }

        public string Provider { get; set; } = string.Empty;
        public string MembershipId { get; set; } = string.Empty;
        public string CoverageDetails { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }
    }
}
