using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TasraPostaManager.Core.Interfaces;
using TasraPostaManager.Data;
using TasraPostaManager.Models;

namespace TasraPostaManager.Data.Repositories
{
    /// <summary>
    /// EF Core implementation of IBarcodePoolRepository.
    /// </summary>
    public class BarcodePoolRepository : IBarcodePoolRepository
    {
        private readonly AppDbContext _db;
        private readonly ILogger<BarcodePoolRepository> _logger;

        public BarcodePoolRepository(AppDbContext db, ILogger<BarcodePoolRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        // ═══════════════════════════════════════
        //  QUERIES
        // ═══════════════════════════════════════

        public async Task<BarcodePoolItem?> GetNextAvailableAsync()
            => await _db.BarcodePoolItems
                .Where(x => !x.IsUsed && x.Status == BarcodePoolStatus.Available)
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync();

        public async Task<List<BarcodePoolItem>> GetAvailableAsync(int count)
            => await _db.BarcodePoolItems
                .Where(x => !x.IsUsed && x.Status == BarcodePoolStatus.Available)
                .OrderBy(x => x.Id)
                .Take(count)
                .ToListAsync();

        public async Task<int> GetAvailableCountAsync()
            => await _db.BarcodePoolItems
                .CountAsync(x => !x.IsUsed && x.Status == BarcodePoolStatus.Available);

        public async Task<int> GetTotalCountAsync()
            => await _db.BarcodePoolItems.CountAsync();

        public async Task<int> GetUsedCountAsync()
            => await _db.BarcodePoolItems.CountAsync(x => x.IsUsed);

        public async Task<BarcodePoolItem?> GetByBarcodeAsync(string barcode)
            => await _db.BarcodePoolItems.FirstOrDefaultAsync(x => x.Barcode == barcode);

        // ═══════════════════════════════════════
        //  COMMANDS
        // ═══════════════════════════════════════

        public async Task AddAsync(BarcodePoolItem item)
            => await _db.BarcodePoolItems.AddAsync(item);

        public async Task AddRangeAsync(IEnumerable<BarcodePoolItem> items)
            => await _db.BarcodePoolItems.AddRangeAsync(items);

        public async Task MarkAsUsedAsync(int id, string? usedByRecordKey = null)
        {
            var item = await _db.BarcodePoolItems.FindAsync(id);
            if (item != null)
            {
                item.IsUsed = true;
                item.Status = BarcodePoolStatus.Used;
                item.UsedAt = DateTime.UtcNow;
                item.UsedByRecordKey = usedByRecordKey;
                _logger.LogDebug("Barkod kullanıldı: {Barcode} → {RecordKey}", item.Barcode, usedByRecordKey);
            }
        }

        public Task UpdateAsync(BarcodePoolItem item)
        {
            _db.BarcodePoolItems.Update(item);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(BarcodePoolItem item)
        {
            _db.BarcodePoolItems.Remove(item);
            return Task.CompletedTask;
        }

        public async Task<int> SaveChangesAsync()
            => await _db.SaveChangesAsync();

        // ═══════════════════════════════════════
        //  BATCH OPERATIONS
        // ═══════════════════════════════════════

        public async Task<int> ImportBatchAsync(IEnumerable<string> barcodes, string? batchId = null, string? source = null)
        {
            var items = barcodes.Select(b => new BarcodePoolItem
            {
                Barcode = b,
                IsUsed = false,
                Status = BarcodePoolStatus.Available,
                ImportedAt = DateTime.UtcNow,
                BatchId = batchId ?? Guid.NewGuid().ToString("N")[..8],
                Source = source
            }).ToList();

            await _db.BarcodePoolItems.AddRangeAsync(items);
            await _db.SaveChangesAsync();

            _logger.LogInformation("{Count} barkod havuza eklendi (Batch: {BatchId})", items.Count, batchId);
            return items.Count;
        }

        public async Task<int> PurgeUsedAsync()
        {
            var usedItems = await _db.BarcodePoolItems.Where(x => x.IsUsed).ToListAsync();
            _db.BarcodePoolItems.RemoveRange(usedItems);
            await _db.SaveChangesAsync();

            _logger.LogInformation("{Count} kullanılmış barkod temizlendi", usedItems.Count);
            return usedItems.Count;
        }
    }
}
