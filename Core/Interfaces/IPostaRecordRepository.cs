using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TasraPostaManager.Models;

namespace TasraPostaManager.Core.Interfaces
{
    /// <summary>
    /// Repository interface for PostaRecord entity operations.
    /// </summary>
    public interface IPostaRecordRepository
    {
        // === QUERIES ===
        Task<List<PostaRecord>> GetAllAsync();
        Task<PostaRecord?> GetByMuhabereNoAsync(string muhabereNo);
        Task<PostaRecord?> GetByIdAsync(int id);
        Task<int> CountAsync();
        Task<List<PostaRecord>> GetRecentAsync(int count);

        /// <summary>
        /// Filtrelenmiş sorgu döndürür (IQueryable olarak — lazy evaluation).
        /// </summary>
        IQueryable<PostaRecord> GetFilteredQuery(RecordFilterDto filter);

        /// <summary>
        /// Belirli barcodlara sahip tüm kayıtları döndürür (zarf kardeşleri için).
        /// </summary>
        Task<List<PostaRecord>> GetByBarcodesAsync(IEnumerable<string> barcodes);

        // === COMMANDS ===
        Task AddAsync(PostaRecord record);
        Task AddRangeAsync(IEnumerable<PostaRecord> records);
        Task UpdateAsync(PostaRecord record);
        Task DeleteAsync(PostaRecord record);
        Task DeleteRangeAsync(IEnumerable<PostaRecord> records);
        Task<int> SaveChangesAsync();

        // === STATISTICS ===
        Task<RecordStatsDto> GetStatsAsync();
    }

    /// <summary>
    /// Filtreleme parametreleri DTO'su.
    /// </summary>
    public class RecordFilterDto
    {
        public string? SearchQuery { get; set; }
        public string? SearchType { get; set; }  // "muhabere", "barkod", "tarih"
        public string? ListeTipi { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? SortField { get; set; }
        public string? SortDirection { get; set; }
    }

    /// <summary>
    /// İstatistik DTO'su.
    /// </summary>
    public class RecordStatsDto
    {
        public int TotalRecords { get; set; }
        public int TodayRecords { get; set; }
        public int PaidRecords { get; set; }
        public int FreeRecords { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
