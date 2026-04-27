namespace SalamatyAPI.Dtos.Services
{
    public class NearbyPharmacyDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Type { get; set; } = "Pharmacy";

        public double? DistanceKm { get; set; }
        public string DistanceText { get; set; } = null!;
        public string Address { get; set; } = null!;

        public string OpenStatusText { get; set; } = null!;
        public bool IsOpenNow { get; set; }

        public string? Phone { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string LocationUrl { get; set; }
    }
}
