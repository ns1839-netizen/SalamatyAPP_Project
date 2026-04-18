namespace SalamatyAPI.Dtos.Favorites
{
    public class CreateFavoriteDto
    {
        // لازم يكون string عشان يستقبل الـ GUID من السواجر
        public string UserId { get; set; } = null!;
        public int ProductId { get; set; }
    }
}