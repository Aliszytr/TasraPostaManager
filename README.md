
# TasraPostaManager — Posta Etiket & Liste Sistemi (C#/.NET 8, MVC, SQL Server)

- İki Excel: **Ücretsiz** ve **Paralı** (sabit dosya adları)
- Excel içe aktarma → SQL Server (MuhabereNo benzersiz anahtar)
- Kayıt yönetimi (listeleme, arama, düzenleme, silme, ekleme)
- **Etiket PDF** (seçilebilir alanlar, hazır boyutlar + özel boyut)
- **Liste PDF**
- Tek komut (Docker): `docker compose up --build`

## Sabit Dosyalar
- Klasör: `fixed-files/`
- Ücretsiz: `PostaListesiParaliDegil.xlsx`
- Paralı: `PostaListesiParali.xlsx`
Uygulamadan **Sabit İçe Aktar** sayfasından içeri alabilirsiniz.

## Çalıştırma
- Docker ile: `docker compose up --build` → `http://localhost:8080`
- SDK ile: `dotnet restore && dotnet run` → `http://localhost:5231`
