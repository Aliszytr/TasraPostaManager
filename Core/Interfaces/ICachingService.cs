namespace TasraPostaManager.Core.Interfaces;

/// <summary>
/// Uygulama genelinde kullanılacak önbellekleme arayüzü.
/// </summary>
public interface ICachingService
{
    /// <summary>
    /// Verilen anahtardaki değeri önbellekten okur. Yoksa null döner.
    /// </summary>
    T? Get<T>(string key);

    /// <summary>
    /// Değeri önbelleğe yazar (istenilen süre kadar saklar).
    /// </summary>
    void Set<T>(string key, T value, TimeSpan? expiration = null);

    /// <summary>
    /// Belirtilen anahtarı önbellekten kaldırır.
    /// </summary>
    void Remove(string key);

    /// <summary>
    /// Belirtilen ön ek ile başlayan TÜM anahtarları kaldırır.
    /// </summary>
    void RemoveByPrefix(string prefix);
}
