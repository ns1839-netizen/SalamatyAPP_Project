namespace SalamatyAPI.Dtos.Services;

public class NearbyServiceDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string Address { get; set; } = null!;

    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? DistanceKm { get; set; }

    public string Status { get; set; } = null!;   // "open" / "closed"
    public string? OpenUntil { get; set; }        // "22:00"
}
