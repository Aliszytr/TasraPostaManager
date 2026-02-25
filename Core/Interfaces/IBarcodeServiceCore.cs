namespace TasraPostaManager.Core.Interfaces;

/// <summary>
/// Barkod servisi interface'i.
/// PostaRecord.cs'den taşındı — model dosyasında interface tanımlamak anti-pattern'dir.
/// </summary>
public interface IBarcodeServiceCore
{
    string GenerateBarcode(string prefix, long number, int digitCount, string suffix);
    bool ValidateBarcode(string barcode);
}
