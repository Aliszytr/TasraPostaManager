using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using TasraPostaManager.Core.Interfaces;

namespace TasraPostaManager.Services.Caching;

/// <summary>
/// IMemoryCache tabanlı önbellekleme servisi.
/// Uygulama içi (process-level) cache sağlar.
/// </summary>
public class InMemoryCachingService : ICachingService
{
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, byte> _keys = new();
    private readonly TimeSpan _defaultExpiration = TimeSpan.FromMinutes(10);

    public InMemoryCachingService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public T? Get<T>(string key)
    {
        return _cache.TryGetValue(key, out T? value) ? value : default;
    }

    public void Set<T>(string key, T value, TimeSpan? expiration = null)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? _defaultExpiration
        };

        // Expire olduğunda key listesinden de sil
        options.RegisterPostEvictionCallback((evictedKey, _, _, _) =>
        {
            _keys.TryRemove(evictedKey.ToString()!, out _);
        });

        _cache.Set(key, value, options);
        _keys.TryAdd(key, 0);
    }

    public void Remove(string key)
    {
        _cache.Remove(key);
        _keys.TryRemove(key, out _);
    }

    public void RemoveByPrefix(string prefix)
    {
        var keysToRemove = _keys.Keys
            .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var key in keysToRemove)
        {
            _cache.Remove(key);
            _keys.TryRemove(key, out _);
        }
    }
}
