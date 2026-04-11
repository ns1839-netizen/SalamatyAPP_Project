namespace Salamaty.API.Models
{
    public class InsuranceNetworkService
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // أصبح string ليتوافق مع "hospital", "pharmacy"
        public double? Latitude { get; set; } // Nullable double
        public double? Longitude { get; set; } // Nullable double
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Governorate { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string InsuranceProviderName { get; set; } = string.Empty;

        public int? InsuranceProviderId { get; set; }
        public virtual InsuranceProvider InsuranceProvider { get; set; }

        // إضافة المواعيد لإنهاء خطأ "does not contain a definition for OpenFrom"
        public TimeSpan? OpenFrom { get; set; }
        public TimeSpan? OpenTo { get; set; }
    }
}