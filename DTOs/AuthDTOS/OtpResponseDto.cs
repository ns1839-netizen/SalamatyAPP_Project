namespace Salamaty.API.DTOs.AuthDTOS
{
    public class OtpResponseDto
    {
        /// <summary>
        /// OTP code (مثلاً 5 أرقام)
        /// </summary>
        public string OtpCode { get; set; } = null!;

        /// <summary>
        /// Email of the user
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// User full name (optional)
        /// </summary>
        public string? FullName { get; set; }

        /// <summary>
        /// General message about success or failure
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Operation success flag
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// Detailed validation errors (rules)
        /// </summary>
        public Dictionary<string, string[]>? ValidationErrors { get; set; }
    }
}
