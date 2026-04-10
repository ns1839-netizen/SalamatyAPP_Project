namespace SalamatyAPI.Dtos.Favorites
{
    public class FavoriteDto
    {
        public int FavoriteId { get; set; }
        public ProductInFavoriteDto Product { get; set; } = default!;
    }

    public class ProductInFavoriteDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string ImageUrl { get; set; } = default!;
        public string? Description { get; set; }
    }
}
