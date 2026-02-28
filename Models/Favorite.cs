namespace SalamatyAPI.Models
{
    public class Favorite
    {
        public int FavoriteId { get; set; }

        public string UserId { get; set; } = null!;


        public int ProductId { get; set; }
        public Product Product { get; set; } = default!;
    }
}