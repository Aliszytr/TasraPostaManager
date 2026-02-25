using System.Threading.Channels;

namespace TasraPostaManager.Core.Interfaces;

/// <summary>
/// Arka plan görev kuyruğu arayüzü.
/// Uzun süren işlemler (PDF üretimi, Excel import vb.) için kullanılır.
/// </summary>
public interface IBackgroundTaskQueue
{
    /// <summary>
    /// Kuyruğa yeni bir görev ekler.
    /// </summary>
    ValueTask QueueAsync(BackgroundWorkItem workItem, CancellationToken ct = default);

    /// <summary>
    /// Kuyruktan bir sonraki görevi alır (bekler yoksa).
    /// </summary>
    ValueTask<BackgroundWorkItem> DequeueAsync(CancellationToken ct);

    /// <summary>
    /// Kuyrukta bekleyen görev sayısı.
    /// </summary>
    int PendingCount { get; }
}

/// <summary>
/// Arka plan görev tanımı.
/// </summary>
public sealed class BackgroundWorkItem
{
    /// <summary>
    /// Görev kimliği (takip için).
    /// </summary>
    public string TaskId { get; init; } = Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// Görev açıklaması (loglarda görünür).
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Çalıştırılacak fonksiyon.
    /// </summary>
    public required Func<IServiceProvider, CancellationToken, Task> WorkFunc { get; init; }

    /// <summary>
    /// Görevin başlangıç zamanı.
    /// </summary>
    public DateTime QueuedAt { get; init; } = DateTime.UtcNow;
}
