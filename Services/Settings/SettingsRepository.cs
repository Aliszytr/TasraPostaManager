using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TasraPostaManager.Data;
using TasraPostaManager.Models;

namespace TasraPostaManager.Services.Settings
{
    /// <summary>
    /// Düşük seviye ayar CRUD operasyonları — DB ile doğrudan iletişim.
    /// AppSettingsService'in altyapı katmanını üstlenir.
    /// </summary>
    public class SettingsRepository
    {
        private readonly AppDbContext _db;
        private readonly ILogger<SettingsRepository> _logger;

        public SettingsRepository(AppDbContext db, ILogger<SettingsRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// Belirli bir ayarı okur (cache'siz, direkt DB).
        /// </summary>
        public async Task<string?> GetSettingAsync(string key)
        {
            var setting = await _db.AppSettings.FirstOrDefaultAsync(s => s.Key == key);
            return setting?.Value;
        }

        /// <summary>
        /// Belirli bir ayarı yazar (upsert mantığı).
        /// </summary>
        public async Task SetSettingAsync(string key, string value)
        {
            var existing = await _db.AppSettings.FirstOrDefaultAsync(s => s.Key == key);
            if (existing != null)
            {
                existing.Value = value;
                _db.AppSettings.Update(existing);
            }
            else
            {
                _db.AppSettings.Add(new AppSetting { Key = key, Value = value });
            }
            await _db.SaveChangesAsync();
            _logger.LogDebug("Ayar güncellendi: {Key} = {Value}", key, value.Length > 50 ? value[..50] + "..." : value);
        }

        /// <summary>
        /// Birden fazla ayarı toplu okur.
        /// </summary>
        public async Task<Dictionary<string, string>> GetSettingsAsync(IEnumerable<string> keys)
        {
            var keyList = keys.ToList();
            var settings = await _db.AppSettings
                .Where(s => keyList.Contains(s.Key))
                .ToDictionaryAsync(s => s.Key, s => s.Value);

            // Eksik key'leri boş string olarak doldur
            foreach (var key in keyList)
            {
                if (!settings.ContainsKey(key))
                    settings[key] = string.Empty;
            }

            return settings;
        }

        /// <summary>
        /// Birden fazla ayarı toplu yazar (transaction içinde).
        /// </summary>
        public async Task SetSettingsAsync(Dictionary<string, string> settings)
        {
            foreach (var (key, value) in settings)
            {
                var existing = await _db.AppSettings.FirstOrDefaultAsync(s => s.Key == key);
                if (existing != null)
                {
                    existing.Value = value;
                }
                else
                {
                    _db.AppSettings.Add(new AppSetting { Key = key, Value = value });
                }
            }
            await _db.SaveChangesAsync();
            _logger.LogDebug("{Count} ayar güncellendi", settings.Count);
        }

        /// <summary>
        /// Tüm ayarları okur.
        /// </summary>
        public async Task<Dictionary<string, string>> GetAllSettingsAsync()
        {
            return await _db.AppSettings
                .ToDictionaryAsync(s => s.Key, s => s.Value);
        }

        /// <summary>
        /// Varsayılan ayarın mevcut olmasını garanti eder.
        /// </summary>
        public async Task EnsureSettingExistsAsync(string key, string defaultValue)
        {
            var exists = await _db.AppSettings.AnyAsync(s => s.Key == key);
            if (!exists)
            {
                _db.AppSettings.Add(new AppSetting { Key = key, Value = defaultValue });
                await _db.SaveChangesAsync();
            }
        }
    }
}
