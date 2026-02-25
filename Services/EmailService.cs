using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace TasraPostaManager.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailWithAttachmentAsync(string toEmail, string subject, string body, byte[] attachmentData, string attachmentFileName)
        {
            // --- NULL SAFETY FIXES (CS8604 Çözümleri) ---
            // appsettings.json dosyasından ayarları oku, null gelirse varsayılan değerleri kullan.

            var mailServer = _configuration["EmailSettings:MailServer"] ?? "";

            // Port null gelirse "587" olarak kabul et
            var portString = _configuration["EmailSettings:MailPort"] ?? "587";
            var port = int.Parse(portString);

            // Gönderen mail null gelirse dummy bir adres ata
            var senderEmail = _configuration["EmailSettings:SenderEmail"] ?? "noreply@tasrapostamanager.com";
            var senderName = _configuration["EmailSettings:SenderName"] ?? "Tasra Posta Yöneticisi";

            var password = _configuration["EmailSettings:SenderPassword"] ?? "";

            // SSL ayarı yoksa varsayılan true kabul et
            var sslString = _configuration["EmailSettings:EnableSsl"] ?? "true";
            var enableSsl = bool.Parse(sslString);

            // Mail mesajını oluştur (using bloğu içine aldık ki işlem bitince bellekten silinsin)
            using (var mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail, senderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            })
            {
                mailMessage.To.Add(toEmail);

                // Dosya ekini oluştur ve maile ekle (MemoryStream kullanarak)
                if (attachmentData != null && attachmentData.Length > 0)
                {
                    // MemoryStream nesnesini using içine almıyoruz çünkü mailMessage Dispose edildiğinde
                    // Attachment nesnesi de stream'i kapatacaktır.
                    var stream = new MemoryStream(attachmentData);
                    var attachment = new Attachment(stream, attachmentFileName);
                    mailMessage.Attachments.Add(attachment);
                }

                // SMTP İstemcisini ayarla ve gönder
                using (var smtpClient = new SmtpClient(mailServer, port))
                {
                    smtpClient.Credentials = new NetworkCredential(senderEmail, password);
                    smtpClient.EnableSsl = enableSsl;

                    // Maili asenkron gönder
                    await smtpClient.SendMailAsync(mailMessage);
                }
            }
        }
    }
}