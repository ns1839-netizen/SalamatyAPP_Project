namespace SalamatyAPI.Dtos.Products;

public class ProductDetailsDto
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public decimal Price { get; set; }
    public string ImageUrl { get; set; } = default!;
    public string Category { get; set; } = default!;
    public string? Description { get; set; }
    public string? SideEffects { get; set; }
}
