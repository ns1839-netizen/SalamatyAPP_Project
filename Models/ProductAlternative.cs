

namespace Salamaty.API.Models
{
    public class ProductAlternative
    {
        public int ProductId { get; set; }
        public Product Product { get; set; } = default!; // No need for the full namespace

        public int AlternativeProductId { get; set; }
        public Product AlternativeProduct { get; set; } = default!; // Simpler!
    }
}
