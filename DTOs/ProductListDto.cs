namespace SalamatyAPI.Dtos.Products;

public class ProductListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public decimal Price { get; set; }
    public string ImageUrl { get; set; } = default!;
    public string Category { get; set; } = default!;
}
