using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TasraPostaManager.Data;

namespace TasraPostaManager.Services
{
    // ────────────────────────────────────────────────────────
    //  Interface
    // ────────────────────────────────────────────────────────
    public interface IDatabaseBackupService
    {
        Task<BackupResult> BackupAsync(CancellationToken ct = default);
        Task<List<BackupFileInfo>> GetBackupListAsync(CancellationToken ct = default);
        Task<Stream?> GetBackupStreamAsync(string fileName, CancellationToken ct = default);
        Task<bool> DeleteBackupAsync(string fileName, CancellationToken ct = default);
    }

    // ────────────────────────────────────────────────────────
    //  DTOs
    // ────────────────────────────────────────────────────────
    public class BackupResult
    {
        public bool Success { get; set; }
        public string? FileName { get; set; }
        public string? FilePath { get; set; }
        public long FileSizeBytes { get; set; }
        public string? ErrorMessage { get; set; }
        public TimeSpan Duration { get; set; }
    }

    public class BackupFileInfo
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public DateTime CreatedAt { get; set; }

        public string SizeDisplay => SizeBytes switch
        {
            < 1024 => $"{SizeBytes} B",
            < 1024 * 1024 => $"{SizeBytes / 1024.0:F1} KB",
            < 1024 * 1024 * 1024 => $"{SizeBytes / (1024.0 * 1024.0):F1} MB",
            _ => $"{SizeBytes / (1024.0 * 1024.0 * 1024.0):F2} GB"
        };
    }

    // ────────────────────────────────────────────────────────
    //  Implementation
    // ────────────────────────────────────────────────────────
    public class DatabaseBackupService : IDatabaseBackupService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<DatabaseBackupService> _logger;
        private const string BackupPrefix = "TasraPostaDB";

        /// <summary>
        /// Backup dosyaları bu klasöre yazılır — proje kökü altında.
        /// SQL Server servisi bu klasöre yazabilmeli.
        /// </summary>
        private readonly string _backupDir;

        public DatabaseBackupService(AppDbContext db, ILogger<DatabaseBackupService> logger)
        {
            _db = db;
            _logger = logger;

            // Proje kökü altında Backups klasörü oluştur
            // ContentRootPath yerine daha güvenilir: bin klasöründen 3 seviye yukarı
            // ama en basiti Environment.CurrentDirectory
            _backupDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Backups");
            _backupDir = Path.GetFullPath(_backupDir); // normalize et

            // Klasör yoksa oluştur
            Directory.CreateDirectory(_backupDir);
            _logger.LogInformation("Backup dizini: {BackupDir}", _backupDir);
        }

        public async Task<BackupResult> BackupAsync(CancellationToken ct = default)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var fileName = $"{BackupPrefix}_{timestamp}.bak";
                var filePath = Path.Combine(_backupDir, fileName);

                var dbName = _db.Database.GetDbConnection().Database;

                // ── Strateji 1: Doğrudan proje Backups klasörüne yaz ──
                try
                {
                    await ExecuteBackupCommand(dbName, filePath, timestamp, ct);
                }
                catch (SqlException ex) when (ex.Number == 3201 || ex.Number == 3013)
                {
                    // SQL Server proje klasörüne yazamıyor — SQL Server'ın kendi dizinine yaz, sonra kopyala
                    _logger.LogWarning("SQL Server '{BackupDir}' klasörüne yazamadı, kendi backup dizinine yazılıp kopyalanacak", _backupDir);

                    var sqlBackupDir = await GetSqlServerBackupDirAsync(ct);
                    var tempPath = Path.Combine(sqlBackupDir, fileName);

                    await ExecuteBackupCommand(dbName, tempPath, timestamp, ct);

                    // SQL Server dizininden proje dizinine kopyala
                    File.Copy(tempPath, filePath, overwrite: true);

                    // SQL Server dizinindeki geçici dosyayı temizle
                    try { File.Delete(tempPath); }
                    catch { /* loglama yeterli */ }

                    _logger.LogInformation("Backup SQL Server dizininden kopyalandı: {From} → {To}", tempPath, filePath);
                }

                var fileInfo = new FileInfo(filePath);
                sw.Stop();

                _logger.LogInformation(
                    "Veritabanı yedekleme başarılı: {FileName} ({Size}) — {Duration:F1}s",
                    fileName, FormatSize(fileInfo.Length), sw.Elapsed.TotalSeconds);

                return new BackupResult
                {
                    Success = true,
                    FileName = fileName,
                    FilePath = filePath,
                    FileSizeBytes = fileInfo.Length,
                    Duration = sw.Elapsed
                };
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "Veritabanı yedekleme başarısız");
                return new BackupResult
                {
                    Success = false,
                    ErrorMessage = $"Yedekleme hatası: {ex.Message}",
                    Duration = sw.Elapsed
                };
            }
        }

        public Task<List<BackupFileInfo>> GetBackupListAsync(CancellationToken ct = default)
        {
            try
            {
                if (!Directory.Exists(_backupDir))
                    return Task.FromResult(new List<BackupFileInfo>());

                var files = Directory.GetFiles(_backupDir, $"{BackupPrefix}_*.bak")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .Select(f => new BackupFileInfo
                    {
                        FileName = f.Name,
                        FilePath = f.FullName,
                        SizeBytes = f.Length,
                        CreatedAt = f.CreationTime
                    })
                    .ToList();

                return Task.FromResult(files);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Yedek listesi alınırken hata oluştu");
                return Task.FromResult(new List<BackupFileInfo>());
            }
        }

        public Task<Stream?> GetBackupStreamAsync(string fileName, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(fileName) || fileName.Contains("..") || Path.GetExtension(fileName) != ".bak")
                return Task.FromResult<Stream?>(null);

            var filePath = Path.Combine(_backupDir, Path.GetFileName(fileName));

            if (!File.Exists(filePath))
                return Task.FromResult<Stream?>(null);

            Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return Task.FromResult<Stream?>(stream);
        }

        public Task<bool> DeleteBackupAsync(string fileName, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(fileName) || fileName.Contains("..") || Path.GetExtension(fileName) != ".bak")
                return Task.FromResult(false);

            try
            {
                var filePath = Path.Combine(_backupDir, Path.GetFileName(fileName));

                if (!File.Exists(filePath))
                    return Task.FromResult(false);

                File.Delete(filePath);
                _logger.LogInformation("Yedek dosyası silindi: {FileName}", fileName);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Yedek dosyası silinemedi: {FileName}", fileName);
                return Task.FromResult(false);
            }
        }

        // ────────────────────────────────────────────────────
        //  Private Helpers
        // ────────────────────────────────────────────────────

        private async Task ExecuteBackupCommand(string dbName, string filePath, string timestamp, CancellationToken ct)
        {
            var sql = $@"
                BACKUP DATABASE [{dbName}]
                TO DISK = @path
                WITH FORMAT, INIT,
                     NAME = @backupName,
                     COMPRESSION,
                     STATS = 10;";

            await using var conn = new SqlConnection(_db.Database.GetConnectionString());
            await conn.OpenAsync(ct);

            await using var cmd = new SqlCommand(sql, conn);
            cmd.CommandTimeout = 300;
            cmd.Parameters.AddWithValue("@path", filePath);
            cmd.Parameters.AddWithValue("@backupName", $"{dbName} Full Backup {timestamp}");

            await cmd.ExecuteNonQueryAsync(ct);
        }

        /// <summary>
        /// SQL Server'ın kendi backup klasörünü sorgular (fallback için).
        /// </summary>
        private async Task<string> GetSqlServerBackupDirAsync(CancellationToken ct)
        {
            try
            {
                await using var conn = new SqlConnection(_db.Database.GetConnectionString());
                await conn.OpenAsync(ct);

                await using var cmd = new SqlCommand(
                    "SELECT SERVERPROPERTY('InstanceDefaultBackupPath') AS BackupPath", conn);

                var result = await cmd.ExecuteScalarAsync(ct);
                if (result != null && result != DBNull.Value)
                {
                    var path = result.ToString()!;
                    if (Directory.Exists(path))
                        return path;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SQL Server backup klasörü sorgulanamadı");
            }

            // Last resort: temp
            var fallback = Path.Combine(Path.GetTempPath(), "TasraPostaBackups");
            Directory.CreateDirectory(fallback);
            return fallback;
        }

        private static string FormatSize(long bytes) => bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
            < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024.0):F1} MB",
            _ => $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB"
        };
    }
}
