namespace Salamaty.API.Models.HomeModels
{
    public class MedicalProvider
    {
        public int Id { get; set; }
        public string Governorate { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string WorkingHours { get; set; } = string.Empty;
        public double Latitude { get; set; } // خط العرض للمستشفى
        public double Longitude { get; set; } // خط الطول للمستشفى
    }
}