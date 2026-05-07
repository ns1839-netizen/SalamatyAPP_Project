using System.ComponentModel.DataAnnotations.Schema;

namespace SalamatyAPI.Models;

#nullable enable // الحل السريع للـ Errors

public class Product
{
    public int Id { get; set; }
    public string? Name { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Price { get; set; }

    public string? ImageUrl { get; set; }
    public string? Category { get; set; }
    public string? Description { get; set; }
    public string? SideEffects { get; set; }

    // هذه الحقول التي كانت تسبب الأخطاء
    public string? Uses { get; set; }
    public string? Alternatives { get; set; } // تأكدي أنها string وليست ICollection هنا
    public string? Pharmacies { get; set; }

    // هذه العلاقات للربط البرمجي فقط (وليس لرفع الإكسيل)
    public virtual ICollection<ProductAlternative> ProductAlternatives { get; set; } = new List<ProductAlternative>();
}