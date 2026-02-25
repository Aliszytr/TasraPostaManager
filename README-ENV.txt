Ali için Hızlı Kurulum (GEÇİCİ .env Yöntemi)

1) .env dosyasını aç ve sadece şurayı doldur:
   TasraPosta__EmailSettings__SenderPassword=BURAYA_GMAIL_APP_PASSWORD

2) Çalıştırma şekline göre .env konumu:
   - Visual Studio (F5) / source çalıştırma: .env dosyası, TasraPostaManager.csproj ile aynı klasörde olmalı.
   - Publish/EXE çalıştırma: .env dosyası, TasraPostaManager.exe ile aynı klasörde olmalı.

3) Güvenlik:
   - .env git'e girmemeli (bu pakette .gitignore hazır).
   - Canlıya çıkarken bunu Windows/IIS Environment Variable'a taşıyacağız.
