using System;
using System.ComponentModel.DataAnnotations;

namespace TasraPostaManager.Models
{
    /// <summary>
    /// Etiket pozisyon bilgileri - PDF oluşturma ve baskı için
    /// </summary>
    public class LabelPosition
    {
        [Display(Name = "Sayfa Numarası")]
        public int PageIndex { get; set; }

        [Display(Name = "X Pozisyonu (mm)")]
        public double X { get; set; }

        [Display(Name = "Y Pozisyonu (mm)")]
        public double Y { get; set; }

        [Display(Name = "Genişlik (mm)")]
        public double Width { get; set; }

        [Display(Name = "Yükseklik (mm)")]
        public double Height { get; set; }

        [Display(Name = "Sütun İndeksi")]
        public int ColumnIndex { get; set; }

        [Display(Name = "Satır İndeksi")]
        public int RowIndex { get; set; }

        [Display(Name = "Global İndeks")]
        public int GlobalIndex { get; set; }

        [Display(Name = "Etiket Numarası")]
        public int LabelNumber => GlobalIndex + 1;

        // 🔹 YENİ: Pozisyon doğrulama ve yardımcı property'ler
        [Display(Name = "Merkez X (mm)")]
        public double CenterX => X + (Width / 2);

        [Display(Name = "Merkez Y (mm)")]
        public double CenterY => Y + (Height / 2);

        [Display(Name = "Sağ Kenar (mm)")]
        public double RightEdge => X + Width;

        [Display(Name = "Alt Kenar (mm)")]
        public double BottomEdge => Y + Height;

        [Display(Name = "Alan (mm²)")]
        public double Area => Width * Height;

        [Display(Name = "Pozisyon Bilgisi")]
        public string PositionInfo => $"Sayfa {PageIndex + 1}, Sütun {ColumnIndex + 1}, Satır {RowIndex + 1}";

        [Display(Name = "Koordinatlar")]
        public string Coordinates => $"(X:{X:F1}, Y:{Y:F1}) - (W:{Width:F1}, H:{Height:F1})";

        public LabelPosition()
        {
        }

        public LabelPosition(double x, double y, double width, double height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public LabelPosition(int pageIndex, int columnIndex, int rowIndex, int globalIndex,
                           double x, double y, double width, double height)
        {
            PageIndex = pageIndex;
            ColumnIndex = columnIndex;
            RowIndex = rowIndex;
            GlobalIndex = globalIndex;
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Pozisyonun geçerli olup olmadığını kontrol eder
        /// </summary>
        public bool IsValid()
        {
            return X >= 0 && Y >= 0 && Width > 0 && Height > 0 &&
                   PageIndex >= 0 && ColumnIndex >= 0 && RowIndex >= 0 &&
                   GlobalIndex >= 0;
        }

        /// <summary>
        /// Pozisyonun sayfa sınırları içinde olup olmadığını kontrol eder
        /// </summary>
        public bool IsWithinPageBounds(double pageWidth, double pageHeight)
        {
            return X >= 0 && Y >= 0 && RightEdge <= pageWidth && BottomEdge <= pageHeight;
        }

        /// <summary>
        /// Diğer bir etiketle çakışıp çakışmadığını kontrol eder
        /// </summary>
        public bool IntersectsWith(LabelPosition other)
        {
            return X < other.RightEdge && RightEdge > other.X &&
                   Y < other.BottomEdge && BottomEdge > other.Y;
        }

        /// <summary>
        /// Bu etiketin içinde belirtilen nokta var mı kontrol eder
        /// </summary>
        public bool ContainsPoint(double pointX, double pointY)
        {
            return pointX >= X && pointX <= RightEdge &&
                   pointY >= Y && pointY <= BottomEdge;
        }

        /// <summary>
        /// Bu etiketin merkez noktasını döndürür
        /// </summary>
        public (double CenterX, double CenterY) GetCenter()
        {
            return (CenterX, CenterY);
        }

        /// <summary>
        /// Etiket köşe noktalarını döndürür (sol-üst, sağ-üst, sağ-alt, sol-alt)
        /// </summary>
        public (double, double)[] GetCorners()
        {
            return new[]
            {
                (X, Y),                     // Sol-üst
                (RightEdge, Y),             // Sağ-üst
                (RightEdge, BottomEdge),    // Sağ-alt
                (X, BottomEdge)             // Sol-alt
            };
        }

        /// <summary>
        /// Pozisyonu belirtilen miktarda kaydırır
        /// </summary>
        public void Offset(double offsetX, double offsetY)
        {
            X += offsetX;
            Y += offsetY;
        }

        /// <summary>
        /// Pozisyonu belirtilen faktörle ölçeklendirir
        /// </summary>
        public void Scale(double scaleX, double scaleY)
        {
            X *= scaleX;
            Y *= scaleY;
            Width *= scaleX;
            Height *= scaleY;
        }

        /// <summary>
        /// Pozisyonu belirtilen boyuta yeniden boyutlandırır
        /// </summary>
        public void Resize(double newWidth, double newHeight)
        {
            Width = newWidth;
            Height = newHeight;
        }

        /// <summary>
        /// Pozisyonu belirtilen sayfaya taşır
        /// </summary>
        public void MoveToPage(int newPageIndex)
        {
            PageIndex = newPageIndex;
        }

        /// <summary>
        /// Pozisyon bilgilerini insan tarafından okunabilir formatta döndürür
        /// </summary>
        public string ToDisplayString()
        {
            return $"Etiket #{LabelNumber} - {PositionInfo} - {Coordinates}";
        }

        /// <summary>
        /// Debug için detaylı bilgi döndürür
        /// </summary>
        public string ToDebugString()
        {
            return $"LabelPosition[Page:{PageIndex}, Col:{ColumnIndex}, Row:{RowIndex}, " +
                   $"Global:{GlobalIndex}, X:{X:F2}, Y:{Y:F2}, W:{Width:F2}, H:{Height:F2}]";
        }

        /// <summary>
        /// JSON serileştirme için uygun formatta döndürür
        /// </summary>
        public object ToJsonObject()
        {
            return new
            {
                pageIndex = PageIndex,
                x = X,
                y = Y,
                width = Width,
                height = Height,
                columnIndex = ColumnIndex,
                rowIndex = RowIndex,
                globalIndex = GlobalIndex,
                labelNumber = LabelNumber,
                centerX = CenterX,
                centerY = CenterY,
                positionInfo = PositionInfo
            };
        }

        /// <summary>
        /// Bu pozisyonun bir kopyasını oluşturur
        /// </summary>
        public LabelPosition Clone()
        {
            return new LabelPosition
            {
                PageIndex = this.PageIndex,
                X = this.X,
                Y = this.Y,
                Width = this.Width,
                Height = this.Height,
                ColumnIndex = this.ColumnIndex,
                RowIndex = this.RowIndex,
                GlobalIndex = this.GlobalIndex
            };
        }

        /// <summary>
        /// Grid pozisyonundan LabelPosition oluşturur
        /// </summary>
        public static LabelPosition FromGridPosition(int pageIndex, int columnIndex, int rowIndex,
                                                   int globalIndex, LabelSettings settings)
        {
            double x = settings.LeftMarginMm + (columnIndex * (settings.LabelWidthMm + settings.HorizontalGapMm));
            double y = settings.TopMarginMm + (rowIndex * (settings.LabelHeightMm + settings.VerticalGapMm));

            return new LabelPosition(pageIndex, columnIndex, rowIndex, globalIndex,
                                   x, y, settings.LabelWidthMm, settings.LabelHeightMm);
        }

        /// <summary>
        /// Sayfa boyutuna göre pozisyonun normalize edilmiş değerlerini döndürür (0-1 aralığı)
        /// </summary>
        public (double NormalizedX, double NormalizedY, double NormalizedWidth, double NormalizedHeight)
            GetNormalizedPosition(double pageWidth, double pageHeight)
        {
            return (X / pageWidth, Y / pageHeight, Width / pageWidth, Height / pageHeight);
        }

        /// <summary>
        /// Normalize edilmiş değerlerden gerçek pozisyon oluşturur
        /// </summary>
        public static LabelPosition FromNormalizedPosition(double normalizedX, double normalizedY,
                                                         double normalizedWidth, double normalizedHeight,
                                                         double pageWidth, double pageHeight,
                                                         int pageIndex, int columnIndex, int rowIndex, int globalIndex)
        {
            return new LabelPosition
            {
                PageIndex = pageIndex,
                ColumnIndex = columnIndex,
                RowIndex = rowIndex,
                GlobalIndex = globalIndex,
                X = normalizedX * pageWidth,
                Y = normalizedY * pageHeight,
                Width = normalizedWidth * pageWidth,
                Height = normalizedHeight * pageHeight
            };
        }
    }

    /// <summary>
    /// LabelPosition koleksiyonu için extension metodlar
    /// </summary>
    public static class LabelPositionExtensions
    {
        /// <summary>
        /// Tüm pozisyonların geçerli olup olmadığını kontrol eder
        /// </summary>
        public static bool AllValid(this IEnumerable<LabelPosition> positions)
        {
            return positions.All(p => p.IsValid());
        }

        /// <summary>
        /// Belirtilen sayfadaki pozisyonları filtreler
        /// </summary>
        public static IEnumerable<LabelPosition> OnPage(this IEnumerable<LabelPosition> positions, int pageIndex)
        {
            return positions.Where(p => p.PageIndex == pageIndex);
        }

        /// <summary>
        /// Pozisyonları sayfa ve grid sırasına göre sıralar
        /// </summary>
        public static IOrderedEnumerable<LabelPosition> OrderByGrid(this IEnumerable<LabelPosition> positions)
        {
            return positions.OrderBy(p => p.PageIndex)
                          .ThenBy(p => p.RowIndex)
                          .ThenBy(p => p.ColumnIndex);
        }

        /// <summary>
        /// Global indekse göre sıralar
        /// </summary>
        public static IOrderedEnumerable<LabelPosition> OrderByGlobalIndex(this IEnumerable<LabelPosition> positions)
        {
            return positions.OrderBy(p => p.GlobalIndex);
        }

        /// <summary>
        /// Çakışan pozisyonları bulur
        /// </summary>
        public static IEnumerable<(LabelPosition First, LabelPosition Second)> FindIntersections(
            this IEnumerable<LabelPosition> positions)
        {
            var positionList = positions.ToList();

            for (int i = 0; i < positionList.Count; i++)
            {
                for (int j = i + 1; j < positionList.Count; j++)
                {
                    if (positionList[i].IntersectsWith(positionList[j]))
                    {
                        yield return (positionList[i], positionList[j]);
                    }
                }
            }
        }

        /// <summary>
        /// Sayfa başına etiket sayılarını döndürür
        /// </summary>
        public static Dictionary<int, int> GetLabelsPerPage(this IEnumerable<LabelPosition> positions)
        {
            return positions.GroupBy(p => p.PageIndex)
                           .ToDictionary(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// Belirtilen noktayı içeren pozisyonu bulur
        /// </summary>
        public static LabelPosition FindByPoint(this IEnumerable<LabelPosition> positions, double x, double y)
        {
            return positions.FirstOrDefault(p => p.ContainsPoint(x, y)) ?? new LabelPosition();
        }

        /// <summary>
        /// Pozisyonları sayfa boyutlarına göre kontrol eder
        /// </summary>
        public static IEnumerable<LabelPosition> WhereWithinPageBounds(
            this IEnumerable<LabelPosition> positions, double pageWidth, double pageHeight)
        {
            return positions.Where(p => p.IsWithinPageBounds(pageWidth, pageHeight));
        }

        /// <summary>
        /// Sayfa sınırlarını aşan pozisyonları bulur
        /// </summary>
        public static IEnumerable<LabelPosition> WhereOutsidePageBounds(
            this IEnumerable<LabelPosition> positions, double pageWidth, double pageHeight)
        {
            return positions.Where(p => !p.IsWithinPageBounds(pageWidth, pageHeight));
        }
    }
}