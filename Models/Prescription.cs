using System.ComponentModel.DataAnnotations;

namespace Salamaty.API.Models
{
    public class Prescription
    {
        [Key]
        public int Id { get; set; }

        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
