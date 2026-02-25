using Microsoft.EntityFrameworkCore;
using TasraPostaManager.Models;

namespace TasraPostaManager.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<PostaRecord> PostaRecords => Set<PostaRecord>();
    public DbSet<AppSetting> AppSettings { get; set; }
    public DbSet<BarcodePoolItem> BarcodePoolItems => Set<BarcodePoolItem>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        mb.Entity<PostaRecord>(b =>
        {
            b.ToTable("PostaRecords");

            // 🔑 Atomik kimlik: MuhabereNo (Primary Key = zaten benzersiz)
            b.HasKey(x => x.MuhabereNo);

            // 📅 Sık kullanılan alanlar için index'ler
            b.HasIndex(x => x.Tarih);
            b.HasIndex(x => x.CreatedAt);

            // 📦 Grup/Zarf kimliği: BarkodNo
            // ESKİ: .HasIndex(x => x.BarkodNo).IsUnique();
            // YENİ: Aynı barkodla birden fazla kayıt olabilsin diye UNIQUE kaldırıldı.
            b.HasIndex(x => x.BarkodNo).IsUnique(false);

            b.Property(x => x.MuhabereNo)
                .HasColumnType("nvarchar(50)")
                .IsRequired();

            b.Property(x => x.GittigiYer)
                .HasColumnType("nvarchar(200)");

            b.Property(x => x.Durum)
                .HasColumnType("nvarchar(100)");

            b.Property(x => x.ListeTipi)
             .HasConversion<int>()
             .HasDefaultValue(ListeTipi.ParaliDegil);

            b.Property(x => x.Miktar)
                .HasColumnType("decimal(18,2)");

            b.Property(x => x.Adres)
                .HasColumnType("nvarchar(500)");

            b.Property(x => x.Tarih)
                .HasColumnType("datetime2");

            b.Property(x => x.BarkodNo)
             .HasColumnType("nvarchar(50)")
             .HasMaxLength(50);

            // YENİ: Gönderen alanı eklendi
            b.Property(x => x.Gonderen)
                .HasColumnType("nvarchar(200)");

            b.Property(x => x.CreatedAt)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("SYSUTCDATETIME()");

            b.Property(x => x.UpdatedAt)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("SYSUTCDATETIME()");
        });

        // YENİ: AppSettings configuration - PdfOutputPath düzeltildi
        mb.Entity<AppSetting>(b =>
        {
            b.ToTable("AppSettings");
            b.HasKey(x => x.Id);

            b.Property(x => x.Key)
                .HasColumnType("nvarchar(100)")
                .IsRequired();

            b.Property(x => x.Value)
                .HasColumnType("nvarchar(1000)");

            b.Property(x => x.PdfOutputPath)
                .HasColumnType("nvarchar(max)")
                .HasDefaultValue(@"D:\TasraPostaManagerOutput");
        });

        // ✅ Barkod Havuzu (Pool)
        mb.Entity<BarcodePoolItem>(b =>
        {
            b.ToTable("BarcodePoolItems");
            b.HasKey(x => x.Id);

            b.Property(x => x.Barcode)
                .HasColumnType("nvarchar(64)")
                .HasMaxLength(64)
                .IsRequired();

            b.HasIndex(x => x.Barcode).IsUnique();
            b.HasIndex(x => new { x.IsUsed, x.Status });

            b.Property(x => x.Status)
                .HasConversion<int>()
                .HasDefaultValue(BarcodePoolStatus.Available);

            b.Property(x => x.ImportedAt)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("SYSUTCDATETIME()");

            b.Property(x => x.UsedAt)
                .HasColumnType("datetime2");

            b.Property(x => x.BatchId)
                .HasColumnType("nvarchar(64)")
                .HasMaxLength(64);

            b.Property(x => x.Source)
                .HasColumnType("nvarchar(128)")
                .HasMaxLength(128);

            b.Property(x => x.UsedByRecordKey)
                .HasColumnType("nvarchar(64)")
                .HasMaxLength(64);
        });
    }
}
