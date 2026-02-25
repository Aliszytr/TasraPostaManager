using System.Collections.Generic;
using TasraPostaManager.Models;

namespace TasraPostaManager.Services
{
    public interface IPdfService
    {
        // V3 Etiket Metodları
        byte[] LabelsV3(IReadOnlyList<PostaRecord> records, LabelSettings settings, string? title = null);
        byte[] LabelsV3(IReadOnlyList<PostaRecord> records, LabelSettings settings, string? title, bool hasOverflowWarning);

        // Modernize Edilmiş Liste Metodları
        byte[] ListPdf(IReadOnlyList<PostaRecord> records, LabelSettings settings, string? title = null);
        byte[] ListPdf(IReadOnlyList<PostaRecord> records, LabelSettings settings, string? title, IReadOnlyList<string>? selectedFields);
    }
}