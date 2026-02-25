using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TasraPostaManager.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(1000)", maxLength: 4000, nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PdfOutputPath = table.Column<string>(type: "nvarchar(max)", nullable: true, defaultValue: "D:\\TasraPostaManagerOutput")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PostaRecords",
                columns: table => new
                {
                    MuhabereNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GittigiYer = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Durum = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ListeTipi = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Miktar = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Adres = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Tarih = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BarkodNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Gonderen = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    IsLabelGenerated = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostaRecords", x => x.MuhabereNo);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PostaRecords_BarkodNo",
                table: "PostaRecords",
                column: "BarkodNo");

            migrationBuilder.CreateIndex(
                name: "IX_PostaRecords_CreatedAt",
                table: "PostaRecords",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PostaRecords_Tarih",
                table: "PostaRecords",
                column: "Tarih");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSettings");

            migrationBuilder.DropTable(
                name: "PostaRecords");
        }
    }
}
