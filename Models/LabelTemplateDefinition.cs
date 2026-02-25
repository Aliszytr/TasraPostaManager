using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Linq;

namespace TasraPostaManager.Models
{
    /// <summary>
    /// Etiket şablonu tanımı - Tüm şablon özelliklerini içerir
    /// </summary>
    public class LabelTemplateDefinition
    {
        public LabelTemplateType TemplateType { get; set; }

        [Required(ErrorMessage = "Şablon görünen adı zorunludur")]
        [Display(Name = "Görünen Ad")]
        public string DisplayName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kağıt boyutu zorunludur")]
        [Display(Name = "Kağıt Boyutu")]
        public PaperSizeType PaperSize { get; set; } = PaperSizeType.A4;

        [Required(ErrorMessage = "Kağıt yönü zorunludur")]
        [Display(Name = "Kağıt Yönü")]
        public PaperOrientation Orientation { get; set; } = PaperOrientation.Portrait;

        [Required(ErrorMessage = "Etiket genişliği zorunludur")]
        [Range(10, 200, ErrorMessage = "Etiket genişliği 10-200 mm arası olmalı")]
        [Display(Name = "Etiket Genişliği (mm)")]
        public double LabelWidthMM { get; set; }

        [Required(ErrorMessage = "Etiket yüksekliği zorunludur")]
        [Range(10, 200, ErrorMessage = "Etiket yüksekliği 10-200 mm arası olmalı")]
        [Display(Name = "Etiket Yüksekliği (mm)")]
        public double LabelHeightMM { get; set; }

        [Required(ErrorMessage = "Sütun sayısı zorunludur")]
        [Range(1, 10, ErrorMessage = "Sütun sayısı 1-10 arası olmalı")]
        [Display(Name = "Sütun Sayısı")]
        public int Columns { get; set; }

        [Required(ErrorMessage = "Satır sayısı zorunludur")]
        [Range(1, 30, ErrorMessage = "Satır sayısı 1-30 arası olmalı")]
        [Display(Name = "Satır Sayısı")]
        public int Rows { get; set; }

        [Required(ErrorMessage = "Üst boşluk zorunludur")]
        [Range(0, 50, ErrorMessage = "Üst boşluk 0-50 mm arası olmalı")]
        [Display(Name = "Üst Boşluk (mm)")]
        public double PageMarginTopMM { get; set; }

        [Required(ErrorMessage = "Sol boşluk zorunludur")]
        [Range(0, 50, ErrorMessage = "Sol boşluk 0-50 mm arası olmalı")]
        [Display(Name = "Sol Boşluk (mm)")]
        public double PageMarginLeftMM { get; set; }

        [Required(ErrorMessage = "Sağ boşluk zorunludur")]
        [Range(0, 50, ErrorMessage = "Sağ boşluk 0-50 mm arası olmalı")]
        [Display(Name = "Sağ Boşluk (mm)")]
        public double PageMarginRightMM { get; set; }

        [Required(ErrorMessage = "Alt boşluk zorunludur")]
        [Range(0, 50, ErrorMessage = "Alt boşluk 0-50 mm arası olmalı")]
        [Display(Name = "Alt Boşluk (mm)")]
        public double PageMarginBottomMM { get; set; }

        [Required(ErrorMessage = "Yatay boşluk zorunludur")]
        [Range(0, 20, ErrorMessage = "Yatay boşluk 0-20 mm arası olmalı")]
        [Display(Name = "Yatay Boşluk (mm)")]
        public double HorizontalSpacingMM { get; set; }

        [Required(ErrorMessage = "Dikey boşluk zorunludur")]
        [Range(0, 20, ErrorMessage = "Dikey boşluk 0-20 mm arası olmalı")]
        [Display(Name = "Dikey Boşluk (mm)")]
        public double VerticalSpacingMM { get; set; }

        [Display(Name = "Toplam Etiket Sayısı")]
        public int TotalLabels => Columns * Rows;

        [Display(Name = "Etiket Alanı Genişliği")]
        public double TotalLabelAreaWidthMM => (LabelWidthMM * Columns) + (HorizontalSpacingMM * (Columns - 1));

        [Display(Name = "Etiket Alanı Yüksekliği")]
        public double TotalLabelAreaHeightMM => (LabelHeightMM * Rows) + (VerticalSpacingMM * (Rows - 1));

        [Display(Name = "Kullanılabilir Genişlik")]
        public double AvailablePageWidthMM => PaperSize == PaperSizeType.A4 ? 210 : 148;

        [Display(Name = "Kullanılabilir Yükseklik")]
        public double AvailablePageHeightMM => PaperSize == PaperSizeType.A4 ? 297 : 210;

        /// <summary>
        /// Şablonun geçerli olup olmadığını kontrol eder
        /// </summary>
        public bool IsValid()
        {
            double totalWidth = TotalLabelAreaWidthMM + PageMarginLeftMM + PageMarginRightMM;
            double totalHeight = TotalLabelAreaHeightMM + PageMarginTopMM + PageMarginBottomMM;

            return totalWidth <= AvailablePageWidthMM && totalHeight <= AvailablePageHeightMM;
        }

        /// <summary>
        /// Hata mesajlarını döndürür
        /// </summary>
        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();

            double totalWidth = TotalLabelAreaWidthMM + PageMarginLeftMM + PageMarginRightMM;
            double totalHeight = TotalLabelAreaHeightMM + PageMarginTopMM + PageMarginBottomMM;

            if (totalWidth > AvailablePageWidthMM)
            {
                errors.Add($"Toplam genişlik ({totalWidth}mm) kağıt genişliğini ({AvailablePageWidthMM}mm) aşıyor");
            }

            if (totalHeight > AvailablePageHeightMM)
            {
                errors.Add($"Toplam yükseklik ({totalHeight}mm) kağıt yüksekliğini ({AvailablePageHeightMM}mm) aşıyor");
            }

            if (Columns < 1 || Columns > 10)
            {
                errors.Add("Sütun sayısı 1-10 arası olmalı");
            }

            if (Rows < 1 || Rows > 30)
            {
                errors.Add("Satır sayısı 1-30 arası olmalı");
            }

            return errors;
        }

        /// <summary>
        /// LabelSettings'e dönüştürür
        /// </summary>
        public LabelSettings ToLabelSettings()
        {
            return new LabelSettings
            {
                PaperSize = this.PaperSize,
                Orientation = this.Orientation,
                Template = this.TemplateType,
                LabelWidthMm = this.LabelWidthMM,
                LabelHeightMm = this.LabelHeightMM,
                Columns = this.Columns,
                Rows = this.Rows,
                TopMarginMm = this.PageMarginTopMM,
                LeftMarginMm = this.PageMarginLeftMM,
                RightMarginMm = this.PageMarginRightMM,
                BottomMarginMm = this.PageMarginBottomMM,
                HorizontalGapMm = this.HorizontalSpacingMM,
                VerticalGapMm = this.VerticalSpacingMM
            };
        }

        /// <summary>
        /// LabelSettings'ten oluşturur
        /// </summary>
        public static LabelTemplateDefinition FromLabelSettings(LabelSettings settings)
        {
            return new LabelTemplateDefinition
            {
                TemplateType = settings.Template,
                DisplayName = settings.Template.GetDisplayName(),
                PaperSize = settings.PaperSize,
                Orientation = settings.Orientation,
                LabelWidthMM = settings.LabelWidthMm,
                LabelHeightMM = settings.LabelHeightMm,
                Columns = settings.Columns,
                Rows = settings.Rows,
                PageMarginTopMM = settings.TopMarginMm,
                PageMarginLeftMM = settings.LeftMarginMm,
                PageMarginRightMM = settings.RightMarginMm,
                PageMarginBottomMM = settings.BottomMarginMm,
                HorizontalSpacingMM = settings.HorizontalGapMm,
                VerticalSpacingMM = settings.VerticalGapMm
            };
        }
    }

    /// <summary>
    /// Önceden tanımlanmış şablonlar
    /// </summary>
    public static class PredefinedTemplates
    {
        public static readonly Dictionary<LabelTemplateType, LabelTemplateDefinition> All = new()
        {
            {
                LabelTemplateType.A4_3x7_70x38,
                new LabelTemplateDefinition
                {
                    TemplateType = LabelTemplateType.A4_3x7_70x38,
                    DisplayName = "A4 - 3x7 - 70x38 mm (Standart)",
                    PaperSize = PaperSizeType.A4,
                    Orientation = PaperOrientation.Portrait,
                    LabelWidthMM = 70,
                    LabelHeightMM = 38,
                    Columns = 3,
                    Rows = 7,
                    PageMarginTopMM = 14,
                    PageMarginLeftMM = 5,
                    PageMarginRightMM = 5,
                    PageMarginBottomMM = 5,
                    HorizontalSpacingMM = 2,
                    VerticalSpacingMM = 0
                }
            },
            {
                LabelTemplateType.A4_2x8_100x38,
                new LabelTemplateDefinition
                {
                    TemplateType = LabelTemplateType.A4_2x8_100x38,
                    DisplayName = "A4 - 2x8 - 100x38 mm (Kargo)",
                    PaperSize = PaperSizeType.A4,
                    Orientation = PaperOrientation.Portrait,
                    LabelWidthMM = 100,
                    LabelHeightMM = 38,
                    Columns = 2,
                    Rows = 8,
                    PageMarginTopMM = 14,
                    PageMarginLeftMM = 5,
                    PageMarginRightMM = 5,
                    PageMarginBottomMM = 5,
                    HorizontalSpacingMM = 2,
                    VerticalSpacingMM = 0
                }
            },
            {
                LabelTemplateType.A4_3x8_70x35,
                new LabelTemplateDefinition
                {
                    TemplateType = LabelTemplateType.A4_3x8_70x35,
                    DisplayName = "A4 - 3x8 - 70x35 mm (Yoğun)",
                    PaperSize = PaperSizeType.A4,
                    Orientation = PaperOrientation.Portrait,
                    LabelWidthMM = 70,
                    LabelHeightMM = 35,
                    Columns = 3,
                    Rows = 8,
                    PageMarginTopMM = 10,
                    PageMarginLeftMM = 5,
                    PageMarginRightMM = 5,
                    PageMarginBottomMM = 5,
                    HorizontalSpacingMM = 2,
                    VerticalSpacingMM = 0
                }
            },
            {
                LabelTemplateType.A4_24_70x37,
                new LabelTemplateDefinition
                {
                    TemplateType = LabelTemplateType.A4_24_70x37,
                    DisplayName = "A4 - 24 - 70x37 mm",
                    PaperSize = PaperSizeType.A4,
                    Orientation = PaperOrientation.Portrait,
                    LabelWidthMM = 70,
                    LabelHeightMM = 37,
                    Columns = 4,
                    Rows = 6,
                    PageMarginTopMM = 10,
                    PageMarginLeftMM = 5,
                    PageMarginRightMM = 5,
                    PageMarginBottomMM = 5,
                    HorizontalSpacingMM = 2,
                    VerticalSpacingMM = 1
                }
            },
            {
                LabelTemplateType.A4_21_63x38,
                new LabelTemplateDefinition
                {
                    TemplateType = LabelTemplateType.A4_21_63x38,
                    DisplayName = "A4 - 21 - 63x38 mm",
                    PaperSize = PaperSizeType.A4,
                    Orientation = PaperOrientation.Portrait,
                    LabelWidthMM = 63,
                    LabelHeightMM = 38,
                    Columns = 3,
                    Rows = 7,
                    PageMarginTopMM = 10,
                    PageMarginLeftMM = 5,
                    PageMarginRightMM = 5,
                    PageMarginBottomMM = 5,
                    HorizontalSpacingMM = 2,
                    VerticalSpacingMM = 1
                }
            },
            {
                LabelTemplateType.A4_65_38x21,
                new LabelTemplateDefinition
                {
                    TemplateType = LabelTemplateType.A4_65_38x21,
                    DisplayName = "A4 - 65 - 38x21 mm (Mini)",
                    PaperSize = PaperSizeType.A4,
                    Orientation = PaperOrientation.Portrait,
                    LabelWidthMM = 38,
                    LabelHeightMM = 21,
                    Columns = 5,
                    Rows = 13,
                    PageMarginTopMM = 5,
                    PageMarginLeftMM = 5,
                    PageMarginRightMM = 5,
                    PageMarginBottomMM = 5,
                    HorizontalSpacingMM = 1,
                    VerticalSpacingMM = 1
                }
            },
            {
                LabelTemplateType.A4_8_99x68,
                new LabelTemplateDefinition
                {
                    TemplateType = LabelTemplateType.A4_8_99x68,
                    DisplayName = "A4 - 8 - 99x68 mm (Büyük Kargo)",
                    PaperSize = PaperSizeType.A4,
                    Orientation = PaperOrientation.Portrait,
                    LabelWidthMM = 99,
                    LabelHeightMM = 68,
                    Columns = 2,
                    Rows = 4,
                    PageMarginTopMM = 10,
                    PageMarginLeftMM = 10,
                    PageMarginRightMM = 10,
                    PageMarginBottomMM = 10,
                    HorizontalSpacingMM = 2,
                    VerticalSpacingMM = 2
                }
            },
            {
                LabelTemplateType.A4_2x10_90x42,
                new LabelTemplateDefinition
                {
                    TemplateType = LabelTemplateType.A4_2x10_90x42,
                    DisplayName = "A4 - 2x10 - 90x42 mm",
                    PaperSize = PaperSizeType.A4,
                    Orientation = PaperOrientation.Portrait,
                    LabelWidthMM = 90,
                    LabelHeightMM = 42,
                    Columns = 2,
                    Rows = 10,
                    PageMarginTopMM = 10,
                    PageMarginLeftMM = 5,
                    PageMarginRightMM = 5,
                    PageMarginBottomMM = 5,
                    HorizontalSpacingMM = 2,
                    VerticalSpacingMM = 1
                }
            },
            {
                LabelTemplateType.A4_4x6_52x29,
                new LabelTemplateDefinition
                {
                    TemplateType = LabelTemplateType.A4_4x6_52x29,
                    DisplayName = "A4 - 4x6 - 52x29 mm",
                    PaperSize = PaperSizeType.A4,
                    Orientation = PaperOrientation.Portrait,
                    LabelWidthMM = 52,
                    LabelHeightMM = 29,
                    Columns = 4,
                    Rows = 6,
                    PageMarginTopMM = 10,
                    PageMarginLeftMM = 5,
                    PageMarginRightMM = 5,
                    PageMarginBottomMM = 5,
                    HorizontalSpacingMM = 2,
                    VerticalSpacingMM = 1
                }
            },
            {
                LabelTemplateType.Continuous_1xN_100x50,
                new LabelTemplateDefinition
                {
                    TemplateType = LabelTemplateType.Continuous_1xN_100x50,
                    DisplayName = "Sürekli Form - 1xN - 100x50 mm",
                    PaperSize = PaperSizeType.ContinuousForm,
                    Orientation = PaperOrientation.Portrait,
                    LabelWidthMM = 100,
                    LabelHeightMM = 50,
                    Columns = 1,
                    Rows = 0, // N değişken
                    PageMarginTopMM = 5,
                    PageMarginLeftMM = 5,
                    PageMarginRightMM = 5,
                    PageMarginBottomMM = 5,
                    HorizontalSpacingMM = 0,
                    VerticalSpacingMM = 2
                }
            },
            {
                LabelTemplateType.Roll_1xN_80x40,
                new LabelTemplateDefinition
                {
                    TemplateType = LabelTemplateType.Roll_1xN_80x40,
                    DisplayName = "Rulo - 1xN - 80x40 mm",
                    PaperSize = PaperSizeType.Roll,
                    Orientation = PaperOrientation.Portrait,
                    LabelWidthMM = 80,
                    LabelHeightMM = 40,
                    Columns = 1,
                    Rows = 0, // N değişken
                    PageMarginTopMM = 5,
                    PageMarginLeftMM = 5,
                    PageMarginRightMM = 5,
                    PageMarginBottomMM = 5,
                    HorizontalSpacingMM = 0,
                    VerticalSpacingMM = 2
                }
            }
        };

        /// <summary>
        /// TemplateType'a göre önceden tanımlanmış şablonu getirir
        /// </summary>
        public static LabelTemplateDefinition? GetTemplate(LabelTemplateType templateType)
        {
            return All.TryGetValue(templateType, out var template) ? template : null;
        }

        /// <summary>
        /// Tüm önceden tanımlanmış şablonları listeler
        /// </summary>
        public static List<LabelTemplateDefinition> GetAllTemplates()
        {
            return All.Values.ToList();
        }

        /// <summary>
        /// Custom olmayan tüm şablonları listeler
        /// </summary>
        public static List<LabelTemplateDefinition> GetPredefinedTemplates()
        {
            return All.Where(x => x.Key != LabelTemplateType.Custom)
                     .Select(x => x.Value)
                     .ToList();
        }
    }
}