using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Salamaty.API.Models
{
    public class Product
    {
        public int Id { get; set; }

        public string Name { get; set; } = default!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public string ImageUrl { get; set; } = default!;
        public string Category { get; set; } = default!;

        public string? Description { get; set; }
        public string? SideEffects { get; set; }

        public ICollection<ProductAlternative> Alternatives { get; set; } = new List<ProductAlternative>();
        public ICollection<ProductAlternative> AlternativeTo { get; set; } = new List<ProductAlternative>();
        public string? Pharmacies { get; set; }
    }
}