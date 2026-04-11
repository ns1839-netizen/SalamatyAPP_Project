using System.ComponentModel.DataAnnotations.Schema; // تأكدي من إضافة هذا الـ using

namespace Salamaty.API.Models
{
    [Table("Favourites")] // هذا السطر هو الحل! يربط الكلاس بجدول Favourites الموجود في القاعدة
    public class Favorite
    {
        public int FavoriteId { get; set; }

        public string UserId { get; set; } = null!;

        public int ProductId { get; set; }
        public SalamatyAPI.Models.Product Product { get; set; } = default!;
    }
}