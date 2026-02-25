using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TasraPostaManager.Controllers;
using TasraPostaManager.Models;

namespace TasraPostaManager.Services
{
    public class ExcelExportService : IExcelExportService
    {
        // MEVCUT METOT (DETAYLI LİSTE - ATOMİK)
        public byte[] ExportPttExcel(IEnumerable<PostaRecord> records)
        {
            if (records == null) throw new ArgumentNullException(nameof(records));

            var list = records.ToList();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("PTTListesi");

            // Başlıklar
            ws.Cell(1, 1).Value = "Barkod";
            ws.Cell(1, 2).Value = "MuhabereNo";
            ws.Cell(1, 3).Value = "AliciAdi";
            ws.Cell(1, 4).Value = "AliciAdresi";
            ws.Cell(1, 5).Value = "IlIlce";
            ws.Cell(1, 6).Value = "ListeTipi";
            ws.Cell(1, 7).Value = "Durum";
            ws.Cell(1, 8).Value = "Miktar";
            ws.Cell(1, 9).Value = "Tarih";
            ws.Cell(1, 10).Value = "Gonderen";

            var headerRange = ws.Range(1, 1, 1, 10);
            headerRange.Style.Font.Bold = true;

            var culture = CultureInfo.GetCultureInfo("tr-TR");
            var row = 2;
            foreach (var r in list)
            {
                ws.Cell(row, 1).Value = r.BarkodNo ?? string.Empty;
                ws.Cell(row, 2).Value = r.MuhabereNo ?? string.Empty;
                ws.Cell(row, 3).Value = r.GittigiYer ?? string.Empty;
                ws.Cell(row, 4).Value = r.Adres ?? string.Empty;
                ws.Cell(row, 5).Value = r.GittigiYer ?? string.Empty;
                ws.Cell(row, 6).Value = r.ListeTipi.ToString();
                ws.Cell(row, 7).Value = r.Durum ?? string.Empty;

                if (r.Miktar.HasValue)
                {
                    ws.Cell(row, 8).Value = r.Miktar.Value;
                    ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00";
                }

                if (r.Tarih.HasValue)
                {
                    ws.Cell(row, 9).Value = r.Tarih.Value;
                    ws.Cell(row, 9).Style.DateFormat.Format = "dd.MM.yyyy";
                }

                ws.Cell(row, 10).Value = r.Gonderen ?? string.Empty;

                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        // YENİ EKLENEN METOT (GRUPLU LİSTE - PTT TESLİM FORMATI)
        public byte[] ExportGroupedPttExcel(IEnumerable<PostaGroupViewModel> groups)
        {
            if (groups == null) throw new ArgumentNullException(nameof(groups));

            var list = groups.ToList();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("PTT_Teslim_Listesi");

            // Başlıklar (PTT Teslim Listesi Formatına Uygun)
            ws.Cell(1, 1).Value = "Sıra No";
            ws.Cell(1, 2).Value = "Barkod No";
            ws.Cell(1, 3).Value = "Muhabere No (Aralık)";
            ws.Cell(1, 4).Value = "Alıcı Adı";
            ws.Cell(1, 5).Value = "Adres";
            ws.Cell(1, 6).Value = "İl / İlçe"; // Genelde Gittiği Yer
            ws.Cell(1, 7).Value = "Adet";      // Zarf içindeki evrak sayısı
            ws.Cell(1, 8).Value = "Toplam Ücret";
            ws.Cell(1, 9).Value = "Tarih";

            var headerRange = ws.Range(1, 1, 1, 9);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            var row = 2;
            int siraNo = 1;

            foreach (var g in list)
            {
                // Ana kayıt (MainRecord) üzerinden ortak bilgileri alıyoruz
                var main = g.MainRecord;

                ws.Cell(row, 1).Value = siraNo++;
                ws.Cell(row, 2).Value = main.BarkodNo ?? string.Empty; // Eğer barkod yoksa boş veya manuel grup id
                ws.Cell(row, 3).Value = g.DisplayMuhabereNo; // Örn: 2025/10-15
                ws.Cell(row, 4).Value = main.GittigiYer ?? string.Empty;
                ws.Cell(row, 5).Value = main.Adres ?? string.Empty;
                ws.Cell(row, 6).Value = main.GittigiYer ?? string.Empty; // İlçe bilgisi genelde burada oluyor
                ws.Cell(row, 7).Value = g.Count; // Zarf içi adet

                // Toplam Tutar
                ws.Cell(row, 8).Value = g.TotalAmount;
                ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00";

                if (main.Tarih.HasValue)
                {
                    ws.Cell(row, 9).Value = main.Tarih.Value;
                    ws.Cell(row, 9).Style.DateFormat.Format = "dd.MM.yyyy";
                }

                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}