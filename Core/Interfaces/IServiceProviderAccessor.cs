namespace TasraPostaManager.Core.Interfaces;

/// <summary>
/// ServiceProvider erişimi için interface.
/// PostaRecord.cs'den taşındı — model dosyasında interface tanımlamak anti-pattern'dir.
/// </summary>
public interface IServiceProviderAccessor
{
    IServiceProvider ServiceProvider { get; }
}
