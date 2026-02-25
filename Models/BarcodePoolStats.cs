namespace TasraPostaManager.Models;

public class BarcodePoolStats
{
    public long Total { get; set; }
    public long Used { get; set; }
    public long Available { get; set; }
    public long Disabled { get; set; }
}
