namespace Salamaty.API.DTOs.PrescriptionDTOS
{
    public class DetectedMedicineDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;

        // المادة الفعالة بتدي ثقة لليوزر في نتيجة الـ Scan
        public string Composition { get; set; } = string.Empty;

        // الاستخدامات الأساسية
        public string Uses { get; set; } = string.Empty;

        public decimal Price { get; set; }
    }
}