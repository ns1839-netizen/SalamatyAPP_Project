using MailKit.Net.Smtp;
using MimeKit;

namespace Salamaty.API.Services.AuthServices
{
    // استخدام الـ Primary Constructor لجلب الـ IConfiguration مباشرة
    public class EmailService(IConfiguration config) : IEmailService
    {
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress("Salamaty System", config["EmailSettings:Sender"]));
            emailMessage.To.Add(new MailboxAddress("", email));
            emailMessage.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlMessage };
            emailMessage.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();

            // --- السطر السحري اللي هيحل المشكلة ---
            // بنقول للمكتبة اقبلي كل الشهادات وما تعمليش فحص للـ Revocation
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;

            try
            {
                await client.ConnectAsync(
                    config["EmailSettings:SmtpServer"],
                    int.Parse(config["EmailSettings:Port"] ?? "587"),
                    MailKit.Security.SecureSocketOptions.StartTls
                );

                await client.AuthenticateAsync(config["EmailSettings:Sender"], config["EmailSettings:Password"]);
                await client.SendAsync(emailMessage);
            }
            catch (Exception ex)
            {
                throw new Exception($"Email sending failed: {ex.Message}");
            }
            finally
            {
                await client.DisconnectAsync(true);
            }
        }
    }
}