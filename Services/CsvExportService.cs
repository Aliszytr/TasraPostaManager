using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TasraPostaManager.Controllers; // PostaGroupViewModel erişimi için
using TasraPostaManager.Models;

namespace TasraPostaManager.Services
{
    public class CsvExportService : ICsvExportService
    {
        // 1. DETAYLI CSV (Eski Metot - Aynen Korundu)
        public byte[] ExportPttCsv(IEnumerable<PostaRecord> records)
        {
            if (records == null) throw new ArgumentNullException(nameof(records));

            var list = records.ToList();
            var sb = new StringBuilder();

            // Başlık satırı
            sb.AppendLine(string.Join(";", new[]
            {
                "Barkod",
                "MuhabereNo",
                "AliciAdi",
                "AliciAdresi",
                "IlIlce",
                "ListeTipi",
                "Durum",
                "Miktar",
                "Tarih",
                "Gonderen"
            }));

            foreach (var r in list)
            {
                var barkod = r.BarkodNo ?? string.Empty;
                var muhabereNo = r.MuhabereNo ?? string.Empty;
                var aliciAdi = r.GittigiYer ?? string.Empty;
                var adres = r.Adres ?? string.Empty;
                var ilIlce = r.GittigiYer ?? string.Empty; // Genelde Gittiği Yer il/ilçe bilgisidir
                var listeTipi = r.ListeTipi.ToString();
                var durum = r.Durum ?? string.Empty;
                var miktar = r.Miktar.HasValue
                    ? r.Miktar.Value.ToString("0.##", CultureInfo.GetCultureInfo("tr-TR"))
                    : string.Empty;
                var tarih = r.Tarih.HasValue
                    ? r.Tarih.Value.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("tr-TR"))
                    : string.Empty;
                var gonderen = r.Gonderen ?? string.Empty;

                var line = string.Join(";", new[]
                {
                    Escape(barkod),
                    Escape(muhabereNo),
                    Escape(aliciAdi),
                    Escape(adres),
                    Escape(ilIlce),
                    Escape(listeTipi),
                    Escape(durum),
                    Escape(miktar),
                    Escape(tarih),
                    Escape(gonderen)
                });

                sb.AppendLine(line);
            }

            var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
            return encoding.GetBytes(sb.ToString());
        }

        // 2. GRUPLANDIRILMIŞ CSV (YENİ EKLENDİ)
        public byte[] ExportGroupedPttCsv(IEnumerable<PostaGroupViewModel> groups)
        {
            if (groups == null) throw new ArgumentNullException(nameof(groups));

            var list = groups.ToList();
            var sb = new StringBuilder();

            // Başlık satırı (Aynı formatı koruyoruz)
            sb.AppendLine(string.Join(";", new[]
            {
                "Barkod",
                "MuhabereNo", // Burada Birleştirilmiş Muhabere No gelecek
                "AliciAdi",
                "AliciAdresi",
                "IlIlce",
                "ListeTipi",
                "Durum",
                "Miktar", // Toplam Miktar
                "Tarih",
                "Gonderen"
            }));

            foreach (var g in list)
            {
                var r = g.MainRecord; // Ortak bilgiler ana kayıttan alınır

                // Zarf Barkodu yoksa, boş bırak veya otomatik üretilen ID'yi kullanma kararı size ait.
                // PTT genelde gerçek barkod ister. Eğer barkod yoksa boş geçiyoruz.
                var barkod = !string.IsNullOrEmpty(r.BarkodNo) ? r.BarkodNo : string.Empty;

                // ÖNEMLİ: Tekil Muhabere yerine Birleştirilmiş (Display) Muhabere No
                var muhabereNo = g.DisplayMuhabereNo ?? string.Empty;

                var aliciAdi = r.GittigiYer ?? string.Empty;
                var adres = r.Adres ?? string.Empty;
                var ilIlce = r.GittigiYer ?? string.Empty;
                var listeTipi = r.ListeTipi.ToString();
                var durum = r.Durum ?? string.Empty;

                // ÖNEMLİ: Grubun Toplam Miktarı
                var miktar = g.TotalAmount > 0
                    ? g.TotalAmount.ToString("0.##", CultureInfo.GetCultureInfo("tr-TR"))
                    : string.Empty;

                var tarih = r.Tarih.HasValue
                    ? r.Tarih.Value.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("tr-TR"))
                    : string.Empty;
                var gonderen = r.Gonderen ?? string.Empty;

                var line = string.Join(";", new[]
                {
                    Escape(barkod),
                    Escape(muhabereNo),
                    Escape(aliciAdi),
                    Escape(adres),
                    Escape(ilIlce),
                    Escape(listeTipi),
                    Escape(durum),
                    Escape(miktar),
                    Escape(tarih),
                    Escape(gonderen)
                });

                sb.AppendLine(line);
            }

            // Türkçe karakterler için UTF-8 BOM
            var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
            return encoding.GetBytes(sb.ToString());
        }

        // Yardımcı Metot: CSV kaçış karakterleri temizliği
        private string Escape(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            // Noktalı virgülü virgüle çevir (CSV ayracı karışmasın)
            var v = value.Replace(";", ",");
            // Satır sonlarını temizle
            v = v.Replace("\r", " ").Replace("\n", " ");
            return v.Trim();
        }
    }
}