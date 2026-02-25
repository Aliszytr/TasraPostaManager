namespace TasraPostaManager.Core.Constants;

/// <summary>
/// Barkod modu: Legacy (dinamik üretim) veya Pool (havuzdan tahsis).
/// Eski magic string "Legacy" / "Pool" yerine bu enum kullanılır.
/// </summary>
public enum BarcodeModeType
{
    /// <summary>Dinamik barkod üretimi (prefix + sequence)</summary>
    Legacy = 0,

    /// <summary>PTT Barkod Havuzu'ndan FIFO tahsis</summary>
    Pool = 1
}

/// <summary>
/// BarcodeModeType ↔ string dönüşüm yardımcıları.
/// Mevcut kodla geriye uyumlu kalır.
/// </summary>
public static class BarcodeModeExtensions
{
    public static string ToSettingValue(this BarcodeModeType mode) => mode switch
    {
        BarcodeModeType.Legacy => "Legacy",
        BarcodeModeType.Pool => "Pool",
        _ => "Legacy"
    };

    public static BarcodeModeType ParseBarcodeMode(string? value) =>
        string.Equals(value, "Pool", StringComparison.OrdinalIgnoreCase)
            ? BarcodeModeType.Pool
            : BarcodeModeType.Legacy;
}
