namespace Salamaty.API.Models
{
    public class Favorite
    {
        public int FavoriteId { get; set; }

        public string UserId { get; set; } = null!;


        public int ProductId { get; set; }
        public Salamaty.API.Models.Product Product { get; set; } = default!;
    }
}