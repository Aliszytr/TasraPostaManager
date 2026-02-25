using System.Collections.Generic;
using TasraPostaManager.Models;
using TasraPostaManager.Controllers; // PostaGroupViewModel için eklendi

namespace TasraPostaManager.Services
{
    public interface IExcelExportService
    {
        /// <summary>
        /// PTT sisteme yüklemeye uygun formatta Excel (XLSX) çıktısı üretir (Detaylı - Atomik Satırlar).
        /// </summary>
        /// <param name="records">Dışa aktarılacak posta kayıtları</param>
        /// <returns>Excel dosyasının bayt dizisi</returns>
        byte[] ExportPttExcel(IEnumerable<PostaRecord> records);

        /// <summary>
        /// PTT Teslim Listesi formatında, aynı zarftaki evrakları birleştirerek Excel çıktısı üretir.
        /// </summary>
        /// <param name="groups">Gruplandırılmış veriler</param>
        /// <returns>Excel dosyasının bayt dizisi</returns>
        byte[] ExportGroupedPttExcel(IEnumerable<PostaGroupViewModel> groups);
    }
}