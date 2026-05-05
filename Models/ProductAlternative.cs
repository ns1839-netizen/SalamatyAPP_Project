namespace SalamatyAPI.Models;

#nullable enable

public class ProductAlternative
{
    public int ProductId { get; set; }
    public virtual Product? Product { get; set; }

    public int AlternativeProductId { get; set; }
    public virtual Product? AlternativeProduct { get; set; }
}