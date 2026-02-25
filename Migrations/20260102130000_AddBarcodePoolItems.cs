using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TasraPostaManager.Migrations
{
    /// <inheritdoc />
    public partial class AddBarcodePoolItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ✅ Idempotent (SQL Server) migration:
            // - Tablo zaten varsa tekrar CREATE TABLE denemez.
            // - Yoksa oluşturur.
            // - Index/unique constraint'leri yoksa ekler.
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[BarcodePoolItems]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[BarcodePoolItems] (
        [Id] int NOT NULL IDENTITY(1,1),
        [Barcode] nvarchar(64) NOT NULL,
        [IsUsed] bit NOT NULL CONSTRAINT [DF_BarcodePoolItems_IsUsed] DEFAULT (CAST(0 AS bit)),
        [Status] int NOT NULL CONSTRAINT [DF_BarcodePoolItems_Status] DEFAULT (0),
        [ImportedAt] datetime2 NOT NULL CONSTRAINT [DF_BarcodePoolItems_ImportedAt] DEFAULT (SYSUTCDATETIME()),
        [UsedAt] datetime2 NULL,
        [BatchId] nvarchar(64) NULL,
        [Source] nvarchar(128) NULL,
        [UsedByRecordKey] nvarchar(64) NULL,
        CONSTRAINT [PK_BarcodePoolItems] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END

-- Unique barcode (no duplicates in pool)
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_BarcodePoolItems_Barcode'
      AND object_id = OBJECT_ID(N'[dbo].[BarcodePoolItems]')
)
BEGIN
    CREATE UNIQUE INDEX [UX_BarcodePoolItems_Barcode]
    ON [dbo].[BarcodePoolItems]([Barcode]);
END

-- Fast availability / filtering
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_BarcodePoolItems_IsUsed'
      AND object_id = OBJECT_ID(N'[dbo].[BarcodePoolItems]')
)
BEGIN
    CREATE INDEX [IX_BarcodePoolItems_IsUsed]
    ON [dbo].[BarcodePoolItems]([IsUsed]);
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_BarcodePoolItems_Status'
      AND object_id = OBJECT_ID(N'[dbo].[BarcodePoolItems]')
)
BEGIN
    CREATE INDEX [IX_BarcodePoolItems_Status]
    ON [dbo].[BarcodePoolItems]([Status]);
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_BarcodePoolItems_BatchId'
      AND object_id = OBJECT_ID(N'[dbo].[BarcodePoolItems]')
)
BEGIN
    CREATE INDEX [IX_BarcodePoolItems_BatchId]
    ON [dbo].[BarcodePoolItems]([BatchId]);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Idempotent drop (rollback sırasında da patlamasın)
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[BarcodePoolItems]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[BarcodePoolItems];
END
");
        }
    }
}
