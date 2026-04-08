using System.ComponentModel.DataAnnotations.Schema;
using Salamaty.API.Models.ProfileModels; // السطر ده هو اللي هيحل الـ Error

namespace Salamaty.API.Models
{
    public class Prescription
    {
        public int Id { get; set; }

        public string? ImagePath { get; set; }

        public DateTime ScanDate { get; set; } = DateTime.UtcNow;

        public string? DetectedMedicines { get; set; }

        // الربط مع اليوزر بتاعك (ApplicationUser)
        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; } // غيرنا الاسم هنا لـ ApplicationUser
    }
}