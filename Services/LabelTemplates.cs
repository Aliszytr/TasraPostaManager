using System.Collections.Generic;
using TasraPostaManager.Models;

namespace TasraPostaManager.Services
{
    public static class LabelTemplates
    {
        public static readonly IReadOnlyDictionary<LabelTemplateType, LabelTemplateDefinition> All
            = new Dictionary<LabelTemplateType, LabelTemplateDefinition>
            {
                // 🔵 65'lik mini adres etiketi (5x13)
                [LabelTemplateType.A4_65_38x21] = new LabelTemplateDefinition
                {
                    TemplateType = LabelTemplateType.A4_65_38x21,
                    DisplayName = "A4 – 65'lik Mini (38.1 x 21.2 mm, 5x13)",
                    PaperSize = PaperSizeType.A4,

                    LabelWidthMM = 38.1,
                    LabelHeightMM = 21.2,

                    PageMarginTopMM = 10.7,
                    PageMarginBottomMM = 10.7,
                    PageMarginLeftMM = 5.0,
                    PageMarginRightMM = 5.0,

                    HorizontalSpacingMM = 2.5,
                    VerticalSpacingMM = 0
                },

                // 🔵 21'lik adres etiketi (3x7)
                [LabelTemplateType.A4_21_63x38] = new LabelTemplateDefinition
                {
                    TemplateType = LabelTemplateType.A4_21_63x38,
                    DisplayName = "A4 – 21'lik Adres (63.5 x 38.1 mm, 3x7)",
                    PaperSize = PaperSizeType.A4,

                    LabelWidthMM = 63.5,
                    LabelHeightMM = 38.1,

                    PageMarginTopMM = 15.15,
                    PageMarginBottomMM = 15.15,
                    PageMarginLeftMM = 7.25,
                    PageMarginRightMM = 7.25,

                    HorizontalSpacingMM = 2.5,
                    VerticalSpacingMM = 0
                },

                // 🔵 24’lük (3x8) – 70x37 mm
                [LabelTemplateType.A4_24_70x37] = new LabelTemplateDefinition
                {
                    TemplateType = LabelTemplateType.A4_24_70x37,
                    DisplayName = "A4 – 24’lük (70 x 37 mm, 3x8)",
                    PaperSize = PaperSizeType.A4,

                    LabelWidthMM = 70,
                    LabelHeightMM = 37,

                    PageMarginTopMM = 0,
                    PageMarginBottomMM = 0,
                    PageMarginLeftMM = 0,
                    PageMarginRightMM = 0,

                    HorizontalSpacingMM = 0,
                    VerticalSpacingMM = 0
                },

                // 🔵 8’lik büyük etiket (2x4) – 99 x 67.7 mm
                [LabelTemplateType.A4_8_99x68] = new LabelTemplateDefinition
                {
                    TemplateType = LabelTemplateType.A4_8_99x68,
                    DisplayName = "A4 – 8’lik Büyük Kargo (99.1 x 67.7 mm, 2x4)",
                    PaperSize = PaperSizeType.A4,

                    LabelWidthMM = 99.1,
                    LabelHeightMM = 67.7,

                    PageMarginTopMM = 13.1,
                    PageMarginBottomMM = 13.1,
                    PageMarginLeftMM = 4.65,
                    PageMarginRightMM = 4.65,

                    HorizontalSpacingMM = 2.5,
                    VerticalSpacingMM = 0
                }
            };
    }
}
