using System.Threading.Tasks;

namespace TasraPostaManager.Services
{
    public interface IEmailService
    {
        /// <summary>
        /// Dosya ekiyle birlikte mail gönderir.
        /// </summary>
        /// <param name="toEmail">Alıcı mail adresi</param>
        /// <param name="subject">Konu</param>
        /// <param name="body">Mesaj içeriği</param>
        /// <param name="attachmentData">Dosyanın byte verisi (Excel/PDF)</param>
        /// <param name="attachmentFileName">Dosya adı (örn: Liste.xlsx)</param>
        Task SendEmailWithAttachmentAsync(string toEmail, string subject, string body, byte[] attachmentData, string attachmentFileName);
    }
}