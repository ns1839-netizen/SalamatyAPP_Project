namespace Salamaty.API.Models.HomeModels
{
    public class Banner
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty; // ده اللي هيشيل مسار الصورة زي /banners/Diabetes.jpg
        public string DetailsUrl { get; set; } = string.Empty;
    }
}