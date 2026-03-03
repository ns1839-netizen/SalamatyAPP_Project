namespace Salamaty.API.Models.HomeModels
{
    public class Facility
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; } // "Hospital", "Pharmacy", "Lab"
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public string OperatingHours { get; set; } // مثال: "Open 24 Hours" أو "Open until 11 PM"
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Governorate { get; set; }
    }
}
