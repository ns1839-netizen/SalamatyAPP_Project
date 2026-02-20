namespace Salamaty.API.DTOs.AuthDTOS
{
    public class AuthResponseDto
    {
        /// <summary>
        /// JWT Token for authentication (null if account is not verified or login fails)
        /// </summary>
        public string? Token { get; set; }

        public string? Email { get; set; }

        public string? FullName { get; set; }

        public string? Message { get; set; }

        public bool Success { get; set; } = true;

        /// <summary>
        /// Flag to tell Flutter whether to navigate to Verification Screen or Home Screen
        /// </summary>
        public bool IsEmailConfirmed { get; set; } // ضروري جداً للربط

        /// <summary>
        /// Only for testing in Swagger - should be hidden or removed in real production
        /// </summary>
        public string? OtpCode { get; set; } // لتسهيل التيست

        /// <summary>
        /// Detailed validation or Identity errors (e.g., Password too short)
        /// </summary>
        public List<string>? Errors { get; set; } // غيرناه لـ List ليتوافق مع أخطاء الـ Identity

        public Dictionary<string, string[]>? ValidationErrors { get; set; }
    }
}