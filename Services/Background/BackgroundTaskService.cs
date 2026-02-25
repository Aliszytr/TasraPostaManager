using TasraPostaManager.Core.Interfaces;

namespace TasraPostaManager.Services.Background;

/// <summary>
/// Arka plan görev işleyicisi (IHostedService).
/// Kuyruktan görevleri alıp sırayla çalıştırır.
/// </summary>
public sealed class BackgroundTaskService : BackgroundService
{
    private readonly IBackgroundTaskQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackgroundTaskService> _logger;

    public BackgroundTaskService(
        IBackgroundTaskQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<BackgroundTaskService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Arka plan görev servisi başlatıldı");

        while (!stoppingToken.IsCancellationRequested)
        {
            BackgroundWorkItem? workItem = null;
            try
            {
                workItem = await _queue.DequeueAsync(stoppingToken);

                _logger.LogInformation(
                    "Arka plan görevi başlıyor: [{TaskId}] {Description}",
                    workItem.TaskId, workItem.Description);

                // Her görev kendi DI scope'unda çalışır (DB context tekrarlanabilir)
                using var scope = _scopeFactory.CreateScope();
                await workItem.WorkFunc(scope.ServiceProvider, stoppingToken);

                _logger.LogInformation(
                    "Arka plan görevi tamamlandı: [{TaskId}] {Description} (Süre: {Duration:F1}s)",
                    workItem.TaskId, workItem.Description,
                    (DateTime.UtcNow - workItem.QueuedAt).TotalSeconds);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Uygulama kapanırken normal davranış
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Arka plan görevi başarısız: [{TaskId}] {Description}",
                    workItem?.TaskId ?? "?", workItem?.Description ?? "?");
            }
        }

        _logger.LogInformation("Arka plan görev servisi durduruldu");
    }
}
