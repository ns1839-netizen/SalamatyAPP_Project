namespace SalamatyAPI.Dtos.Products
{
    public class ProductAlternativeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public string ImageUrl { get; set; } = default!;
        public string? SideEffect { get; set; }
    }
}
