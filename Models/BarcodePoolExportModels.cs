using System;

namespace TasraPostaManager.Models;

/// <summary>
/// Barkod havuzu dışa aktarma kapsamı.
/// </summary>
public enum BarcodePoolExportScope
{
    /// <summary>
    /// Sadece kalan barkodlar (IsUsed=false AND Status=Available)
    /// </summary>
    Remaining = 1,

    /// <summary>
    /// Tüm barkodlar (kullanılan + kullanılmayan)
    /// </summary>
    All = 2,

    /// <summary>
    /// Denetim amaçlı snapshot: Barcode + Status + UsedAt + UsedByRecordKey + BatchId
    /// </summary>
    Snapshot = 3
}

/// <summary>
/// Export için satır DTO'su. Entity'yi doğrudan dışarı vermiyoruz.
/// </summary>
public sealed class BarcodePoolExportRow
{
    public string Barcode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? UsedAt { get; set; }
    public string? UsedByRecordKey { get; set; }
    public string? BatchId { get; set; }
}
