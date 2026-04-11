namespace Salamaty.API.Models
{
    public class MedicalProduct
    {
        public int Id { get; set; }

        // Medicine Name في الشيت
        public string Name { get; set; } = string.Empty;

        // Composition - المادة الفعالة (مهمة جداً للـ AI Matching)
        public string? Composition { get; set; }

        // Uses - دواعي الاستعمال (بدل Category)
        public string? Uses { get; set; }

        // Side_effects
        public string? SideEffects { get; set; }

        // Image URL
        public string? ImageUrl { get; set; }

        // Manufacturer - الشركة المصنعة
        public string? Manufacturer { get; set; }

        // التقييمات (اختياري لو محتاجة تعرضيها)
        public int ExcellentReviewPercent { get; set; }
        public int AverageReviewPercent { get; set; }

        // السعر مش موجود في الشيت، فخليه nullable عشان ميعملش Error وقت الرفع
        public decimal? Price { get; set; }
    }
}