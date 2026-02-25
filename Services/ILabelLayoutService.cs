// Services/ILabelLayoutService.cs
using TasraPostaManager.Models;

namespace TasraPostaManager.Services
{
    /// <summary>
    /// Etiket yerleşim motoru için giriş noktası.
    /// </summary>
    public interface ILabelLayoutService
    {
        /// <summary>
        /// Modern tek ayar modeliyle layout hesaplar.
        /// </summary>
        LabelLayoutResult CalculateLayout(LabelSettings settings, int totalItemCount);

        /// <summary>
        /// Eski kodlarla uyumluluk için LabelLayoutRequest tabanlı hesaplama.
        /// İçeride LabelSettings'e map edilerek tek motora yönlendirilir.
        /// </summary>
        LabelLayoutResult CalculateLayout(LabelLayoutRequest request, int totalItemCount);
    }
}
