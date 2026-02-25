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
    /// EF Core implementation of IPostaRecordRepository.
    /// </summary>
    public class PostaRecordRepository : IPostaRecordRepository
    {
        private readonly AppDbContext _db;
        private readonly ILogger<PostaRecordRepository> _logger;

        public PostaRecordRepository(AppDbContext db, ILogger<PostaRecordRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        // ═══════════════════════════════════════
        //  QUERIES
        // ═══════════════════════════════════════

        public async Task<List<PostaRecord>> GetAllAsync()
            => await _db.PostaRecords.AsNoTracking().ToListAsync();

        public async Task<PostaRecord?> GetByMuhabereNoAsync(string muhabereNo)
            => await _db.PostaRecords.FirstOrDefaultAsync(x => x.MuhabereNo == muhabereNo);

        public async Task<PostaRecord?> GetByIdAsync(int id)
            => await _db.PostaRecords.FindAsync(id);

        public async Task<int> CountAsync()
            => await _db.PostaRecords.CountAsync();

        public async Task<List<PostaRecord>> GetRecentAsync(int count)
            => await _db.PostaRecords
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .Take(count)
                .ToListAsync();

        public IQueryable<PostaRecord> GetFilteredQuery(RecordFilterDto filter)
        {
            var query = _db.PostaRecords.AsNoTracking().AsQueryable();

            // Text search
            if (!string.IsNullOrWhiteSpace(filter.SearchQuery))
            {
                var q = filter.SearchQuery.Trim();
                switch (filter.SearchType)
                {
                    case "barkod":
                        query = query.Where(x => x.BarkodNo != null && x.BarkodNo.Contains(q));
                        break;
                    case "tarih":
                        if (DateTime.TryParse(q, out var searchDate))
                            query = query.Where(x => x.Tarih.HasValue && x.Tarih.Value.Date == searchDate.Date);
                        break;
                    case "muhabere":
                    default:
                        query = query.Where(x => x.MuhabereNo.Contains(q));
                        break;
                }
            }

            // Liste tipi filter
            if (!string.IsNullOrWhiteSpace(filter.ListeTipi) && Enum.TryParse<ListeTipi>(filter.ListeTipi, true, out var t))
                query = query.Where(x => x.ListeTipi == t);

            // Date range
            if (filter.DateFrom.HasValue)
                query = query.Where(x => x.Tarih >= filter.DateFrom.Value.Date);

            if (filter.DateTo.HasValue)
            {
                var toDate = filter.DateTo.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(x => x.Tarih <= toDate);
            }

            return query;
        }

        public async Task<List<PostaRecord>> GetByBarcodesAsync(IEnumerable<string> barcodes)
        {
            var barcodeList = barcodes.ToList();
            return await _db.PostaRecords
                .AsNoTracking()
                .Where(x => x.BarkodNo != null && barcodeList.Contains(x.BarkodNo))
                .ToListAsync();
        }

        // ═══════════════════════════════════════
        //  COMMANDS
        // ═══════════════════════════════════════

        public async Task AddAsync(PostaRecord record)
        {
            await _db.PostaRecords.AddAsync(record);
            _logger.LogDebug("Kayıt eklendi: {MuhabereNo}", record.MuhabereNo);
        }

        public async Task AddRangeAsync(IEnumerable<PostaRecord> records)
        {
            await _db.PostaRecords.AddRangeAsync(records);
        }

        public Task UpdateAsync(PostaRecord record)
        {
            record.UpdatedAt = DateTime.UtcNow;
            _db.PostaRecords.Update(record);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(PostaRecord record)
        {
            _db.PostaRecords.Remove(record);
            return Task.CompletedTask;
        }

        public Task DeleteRangeAsync(IEnumerable<PostaRecord> records)
        {
            _db.PostaRecords.RemoveRange(records);
            return Task.CompletedTask;
        }

        public async Task<int> SaveChangesAsync()
            => await _db.SaveChangesAsync();

        // ═══════════════════════════════════════
        //  STATISTICS
        // ═══════════════════════════════════════

        public async Task<RecordStatsDto> GetStatsAsync()
        {
            var today = DateTime.UtcNow.Date;
            var records = _db.PostaRecords.AsNoTracking();

            return new RecordStatsDto
            {
                TotalRecords = await records.CountAsync(),
                TodayRecords = await records.CountAsync(x => x.CreatedAt.Date == today),
                PaidRecords = await records.CountAsync(x => x.ListeTipi == ListeTipi.Parali),
                FreeRecords = await records.CountAsync(x => x.ListeTipi == ListeTipi.ParaliDegil),
                TotalAmount = await records
                    .Where(x => x.Miktar.HasValue)
                    .SumAsync(x => x.Miktar!.Value)
            };
        }
    }
}
