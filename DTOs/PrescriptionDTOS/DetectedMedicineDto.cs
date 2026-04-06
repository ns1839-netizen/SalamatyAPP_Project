namespace Salamaty.API.DTOs.PrescriptionDTOS
{

    public class DetectedMedicineDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

}
