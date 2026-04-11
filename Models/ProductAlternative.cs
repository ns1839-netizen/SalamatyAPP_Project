namespace SalamatyAPI.Models
{
    public class ProductAlternative
    {
        public int ProductId { get; set; }
        public Product Product { get; set; } = default!;

        public int AlternativeProductId { get; set; }
        public Product AlternativeProduct { get; set; } = default!;
    }
}