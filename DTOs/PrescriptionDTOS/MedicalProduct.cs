namespace Salamaty.API.Models
{
    // الـ Namespace لازم يحتوي على Class الأول
    public class MedicalProduct
    {
        // الأعضاء (Properties) بتتحط هنا جوه الكلاس
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public string? Category { get; set; }
        public string? Description { get; set; }
        public string? SideEffects { get; set; }
        public string? Pharmacies { get; set; }
    }
}