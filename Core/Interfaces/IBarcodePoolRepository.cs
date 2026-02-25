using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TasraPostaManager.Models;

namespace TasraPostaManager.Core.Interfaces
{
    /// <summary>
    /// Repository interface for BarcodePoolItem entity operations.
    /// </summary>
    public interface IBarcodePoolRepository
    {
        // === QUERIES ===
        Task<BarcodePoolItem?> GetNextAvailableAsync();
        Task<List<BarcodePoolItem>> GetAvailableAsync(int count);
        Task<int> GetAvailableCountAsync();
        Task<int> GetTotalCountAsync();
        Task<int> GetUsedCountAsync();
        Task<BarcodePoolItem?> GetByBarcodeAsync(string barcode);

        // === COMMANDS ===
        Task AddAsync(BarcodePoolItem item);
        Task AddRangeAsync(IEnumerable<BarcodePoolItem> items);
        Task MarkAsUsedAsync(int id, string? usedByRecordKey = null);
        Task UpdateAsync(BarcodePoolItem item);
        Task DeleteAsync(BarcodePoolItem item);
        Task<int> SaveChangesAsync();

        // === BATCH OPERATIONS ===
        Task<int> ImportBatchAsync(IEnumerable<string> barcodes, string? batchId = null, string? source = null);
        Task<int> PurgeUsedAsync();
    }
}
