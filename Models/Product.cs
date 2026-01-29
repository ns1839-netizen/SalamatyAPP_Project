namespace Salamaty.API.Models
{
    public class Product
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }

        public string SideEffect { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Uses { get; set; } = string.Empty;
        public string Alternatives { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
