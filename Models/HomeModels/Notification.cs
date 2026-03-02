namespace Salamaty.API.Models.HomeModels
{
    public class Notification
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty; // Welcome, Awareness, etc.
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? TargetSpecialty { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now; //
        public bool IsRead { get; set; } = false; //
        public string UserId { get; set; } = string.Empty; // ربط الإشعار بمستخدم
    }
}
