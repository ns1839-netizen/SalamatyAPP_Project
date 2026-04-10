using Salamaty.API.Models.ProfileModels;
using Salamaty.API.Models;

public class InsuranceProfile
{
    public int Id { get; set; }

    // تأكدي أن الـ UserId نوعه string
    public string UserId { get; set; } = null!;

    // التعديل المهم: الربط بـ ApplicationUser
    public virtual ApplicationUser User { get; set; } = null!;

    public int InsuranceProviderId { get; set; }
    public virtual InsuranceProvider InsuranceProvider { get; set; } = null!;

    public string CardHolderId { get; set; } = null!;
    public string FrontImagePath { get; set; } = null!;
    public string BackImagePath { get; set; } = null!;
}