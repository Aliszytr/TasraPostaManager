using ClosedXML.Excel;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using TasraPostaManager.Models;

namespace TasraPostaManager.Services;

/// <summary>
/// Barkod havuzu dışa aktarım servisleri.
/// - Remaining: tek kolon Barcode
/// - All: tek kolon Barcode (kurumsal isterse Snapshot kullanılır)
/// - Snapshot: Barcode + Status + UsedAt + UsedByRecordKey + BatchId
/// </summary>
public sealed class BarcodePoolExportService : IBarcodePoolExportService
{
    public byte[] ExportCsv(IEnumerable<BarcodePoolExportRow> rows, BarcodePoolExportScope scope, bool includeHeader = true)
    {
        if (rows == null) throw new ArgumentNullException(nameof(rows));

        var sb = new StringBuilder();
        // UTF-8 BOM: Excel'de Türkçe/Unicode sorunsuz açılsın
        // (BOM'u byte[] üretiminde ekleyeceğiz.)

        var list = rows.ToList();

        if (scope == BarcodePoolExportScope.Snapshot)
        {
            sb.AppendLine("Barcode,Status,UsedAt,UsedByRecordKey,BatchId");
            foreach (var r in list)
            {
                var usedAt = r.UsedAt.HasValue
                    ? r.UsedAt.Value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
                    : string.Empty;

                sb.Append(Escape(r.Barcode)); sb.Append(',');
                sb.Append(Escape(r.Status)); sb.Append(',');
                sb.Append(Escape(usedAt)); sb.Append(',');
                sb.Append(Escape(r.UsedByRecordKey ?? string.Empty)); sb.Append(',');
                sb.Append(Escape(r.BatchId ?? string.Empty));
                sb.AppendLine();
            }
        }
        else
        {
            // PTT format: başlıksız tek kolon (isteğe bağlı)
            if (includeHeader)
                sb.AppendLine("Barcode");
            foreach (var r in list)
            {
                sb.AppendLine(Escape(r.Barcode));
            }
        }

        var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        return utf8.GetBytes(sb.ToString());
    }

    public byte[] ExportXlsx(IEnumerable<BarcodePoolExportRow> rows, BarcodePoolExportScope scope, bool includeHeader = true)
    {
        if (rows == null) throw new ArgumentNullException(nameof(rows));

        var list = rows.ToList();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add(scope == BarcodePoolExportScope.Snapshot ? "BarcodePool_Snapshot" : "BarcodePool");

        // IMPORTANT (PTT format / Excel scientific notation):
        // Barkodlar sadece rakamlardan oluştuğu için Excel bunları "sayı" gibi yorumlayıp
        // 2,88013E+12 şeklinde scientific notation gösterebiliyor.
        // PTT'nin verdiği dosya formatına yakın ve "288013..." düz metin görünümü için
        // barkod kolonunu TEXT formatına zorluyoruz.
        ws.Column(1).Style.NumberFormat.Format = "@";

        if (scope == BarcodePoolExportScope.Snapshot)
        {
            ws.Cell(1, 1).Value = "Barcode";
            ws.Cell(1, 2).Value = "Status";
            ws.Cell(1, 3).Value = "UsedAt";
            ws.Cell(1, 4).Value = "UsedByRecordKey";
            ws.Cell(1, 5).Value = "BatchId";

            ws.Range(1, 1, 1, 5).Style.Font.Bold = true;

            var row = 2;
            foreach (var r in list)
            {
                // Force text
                // ClosedXML'de IXLCell.DataType read-only olabilir; metin zorlamak için
                // hücreye string value set etmek ve column format'ı '@' yapmak yeterlidir.
                // ClosedXML version compatibility: avoid generic SetValue<T>
                ws.Cell(row, 1).Value = r.Barcode;
                ws.Cell(row, 2).Value = r.Status;
                if (r.UsedAt.HasValue)
                {
                    ws.Cell(row, 3).Value = r.UsedAt.Value;
                    ws.Cell(row, 3).Style.DateFormat.Format = "yyyy-MM-dd HH:mm:ss";
                }
                else
                {
                    ws.Cell(row, 3).Value = string.Empty;
                }
                ws.Cell(row, 4).Value = r.UsedByRecordKey ?? string.Empty;
                ws.Cell(row, 5).Value = r.BatchId ?? string.Empty;
                row++;
            }
        }
        else
        {
            var startRow = includeHeader ? 2 : 1;
            if (includeHeader)
            {
                ws.Cell(1, 1).Value = "Barcode";
                ws.Range(1, 1, 1, 1).Style.Font.Bold = true;
            }

            var row = startRow;
            foreach (var r in list)
            {
                // ClosedXML version compatibility: avoid generic SetValue<T>
                ws.Cell(row, 1).Value = r.Barcode;
                row++;
            }
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private static string Escape(string value)
    {
        // CSV standard escaping
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        return value;
    }
}
