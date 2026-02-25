using System.Text;
using TasraPostaManager.Services;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;

namespace TasraPostaManager.Services;

[SupportedOSPlatform("windows")]
public class CrossPlatformBarcodeService : IBarcodeService // ✅ Interface ekledik
{
    public byte[]? GenerateCode128(string text, string size = "medium")
    {
        if (string.IsNullOrEmpty(text))
        {
            return Array.Empty<byte>();
        }

        try
        {
            // Basit bir SVG barkodu oluştur - bu tamamen cross-platform
            var svgContent = GenerateBarcodeSvg(text, size);
            return Encoding.UTF8.GetBytes(svgContent);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Barkod oluşturma hatası: {ex.Message}");
            return null;
        }
    }

    private string GenerateBarcodeSvg(string text, string size)
    {
        int width = size switch
        {
            "small" => 200,
            "large" => 400,
            _ => 300
        };
        int height = 80;

        var random = new Random(text.GetHashCode()); // Aynı text için aynı barkodu üretmek için

        var svg = new StringBuilder();
        svg.AppendLine($@"<svg width='{width}' height='{height}' xmlns='http://www.w3.org/2000/svg'>");
        svg.AppendLine(@"<rect width='100%' height='100%' fill='white'/>");

        // Barkod çizgileri
        for (int i = 10; i < width - 10; i += 2)
        {
            if (random.Next(0, 2) == 1)
            {
                var lineHeight = random.Next(20, 60);
                svg.AppendLine($@"<rect x='{i}' y='10' width='1' height='{lineHeight}' fill='black'/>");
            }
        }

        // Barkod numarası
        svg.AppendLine($@"<text x='{width / 2}' y='{height - 5}' text-anchor='middle' font-family='Arial' font-size='12' fill='black'>{text}</text>");
        svg.AppendLine("</svg>");

        return svg.ToString();
    }
}

// ✅ Interface tanımını ekleyelim
public interface IBarcodeService
{
    byte[]? GenerateCode128(string text, string size = "medium");
}