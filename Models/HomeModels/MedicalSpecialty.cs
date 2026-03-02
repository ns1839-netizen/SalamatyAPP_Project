namespace Salamaty.API.Models.HomeModels
{
    public class MedicalSpecialty
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // مثل Dentistry, Neurology
                                                         // public string IconUrl { get; set; } = string.Empty; // مسار الأيقونة في wwwroot
    }
}