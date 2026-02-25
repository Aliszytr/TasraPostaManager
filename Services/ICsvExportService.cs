using System.Collections.Generic;
using TasraPostaManager.Controllers; // PostaGroupViewModel için gerekli
using TasraPostaManager.Models;

namespace TasraPostaManager.Services
{
    public interface ICsvExportService
    {
        /// <summary>
        /// PTT sistemine yüklenmeye uygun olacak şekilde Detaylı CSV içeriği üretir.
        /// </summary>
        byte[] ExportPttCsv(IEnumerable<PostaRecord> records);

        /// <summary>
        /// PTT sistemine uygun Gruplandırılmış (Zarf Bazlı) CSV içeriği üretir.
        /// </summary>
        byte[] ExportGroupedPttCsv(IEnumerable<PostaGroupViewModel> groups);
    }
}