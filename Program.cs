// DotNetEnv — .env dosyası opsiyonel fallback olarak desteklenir
// Hassas veriler öncelikli olarak User Secrets veya Environment Variables ile yönetilir.
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using Serilog;
using TasraPostaManager.Data;
using TasraPostaManager.Middleware;
using TasraPostaManager.Models;
using TasraPostaManager.Services;

// ─────────────────────────────────────────────────────────────
// 1. BOOTSTRAP LOGGER (Serilog — uygulama başlamadan önce)
// ─────────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("TasraPostaManager başlatılıyor...");

    // .env dosyası varsa ortam değişkenlerine yükle (opsiyonel fallback)
    try
    {
        var envPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".env");
        if (File.Exists(envPath))
        {
            DotNetEnv.Env.Load(envPath);
            Log.Information(".env dosyası yüklendi (fallback)");
        }
        else if (File.Exists(".env"))
        {
            DotNetEnv.Env.Load();
            Log.Information(".env dosyası yüklendi (çalışma dizini)");
        }
    }
    catch (Exception envEx)
    {
        Log.Warning(envEx, ".env dosyası yüklenemedi — User Secrets veya environment variables kullanılacak");
    }

    var builder = WebApplication.CreateBuilder(args);

    // ─────────────────────────────────────────────────────────
    // 2. SERILOG — Yapılandırılmış Loglama
    // ─────────────────────────────────────────────────────────
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "TasraPostaManager")
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.File(
            path: "Logs/TasraPosta-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
            shared: true)
    );

    // ─────────────────────────────────────────────────────────
    // 3. LİSANS & ENCODING
    // ─────────────────────────────────────────────────────────
    QuestPDF.Settings.License = LicenseType.Community;
    System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

    // ─────────────────────────────────────────────────────────
    // 4. SESSION KONFİGÜRASYONU
    // ─────────────────────────────────────────────────────────
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(15);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.Name = "TasraPosta.Session";
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

    // ─────────────────────────────────────────────────────────
    // 5. MVC KONFİGÜRASYONU
    // ─────────────────────────────────────────────────────────
    builder.Services.AddControllersWithViews()
        .AddRazorRuntimeCompilation()
        .AddViewLocalization()
        .AddDataAnnotationsLocalization()
        .AddMvcOptions(options =>
        {
            options.MaxModelBindingCollectionSize = 10000;
        });

    // ─────────────────────────────────────────────────────────
    // 6. KÜLTÜR AYARI — Türkçe
    // ─────────────────────────────────────────────────────────
    builder.Services.Configure<RequestLocalizationOptions>(options =>
    {
        options.SetDefaultCulture("tr-TR");
        options.AddSupportedCultures("tr-TR");
        options.AddSupportedUICultures("tr-TR");
    });

    // ─────────────────────────────────────────────────────────
    // 7. DATABASE KONFİGÜRASYONU
    // ─────────────────────────────────────────────────────────
    builder.Services.AddDbContext<AppDbContext>(opt =>
    {
        var cs = builder.Configuration.GetConnectionString("SqlConnection");
        if (!string.IsNullOrEmpty(cs))
        {
            opt.UseSqlServer(cs, sql =>
            {
                sql.EnableRetryOnFailure();
                sql.CommandTimeout(60);
                sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });
        }
        else
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (env == "Development")
            {
                opt.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=TasraPostaDB;Trusted_Connection=true;TrustServerCertificate=true;");
            }
            else
            {
                throw new InvalidOperationException("Connection string 'SqlConnection' not found.");
            }
        }
    });

    // ─────────────────────────────────────────────────────────
    // 8. SERVİS KAYITLARI
    // ─────────────────────────────────────────────────────────

    // Memory Cache
    builder.Services.AddMemoryCache();
    builder.Services.AddSingleton<TasraPostaManager.Core.Interfaces.ICachingService, TasraPostaManager.Services.Caching.InMemoryCachingService>();

    // Background Job Queue
    builder.Services.AddSingleton<TasraPostaManager.Core.Interfaces.IBackgroundTaskQueue, TasraPostaManager.Services.Background.BackgroundTaskQueue>();
    builder.Services.AddHostedService<TasraPostaManager.Services.Background.BackgroundTaskService>();

    // Repository Layer
    builder.Services.AddScoped<TasraPostaManager.Core.Interfaces.IPostaRecordRepository, TasraPostaManager.Data.Repositories.PostaRecordRepository>();
    builder.Services.AddScoped<TasraPostaManager.Core.Interfaces.IBarcodePoolRepository, TasraPostaManager.Data.Repositories.BarcodePoolRepository>();

    // Settings & PDF Sub-Services
    builder.Services.AddScoped<TasraPostaManager.Services.Settings.SettingsRepository>();
    builder.Services.AddScoped<TasraPostaManager.Services.Pdf.LabelPdfService>();
    builder.Services.AddScoped<TasraPostaManager.Services.Pdf.ListPdfService>();

    builder.Services.AddScoped<IAppSettingsService, AppSettingsService>();
    builder.Services.AddScoped<ILabelLayoutService, LabelLayoutService>();
    builder.Services.AddScoped<IPdfService, PdfService>();
    builder.Services.AddScoped<IDynamicBarcodeService, BarcodeService>();

    // Barkod Havuzu (Pool)
    builder.Services.AddScoped<IBarcodePoolService, BarcodePoolService>();
    builder.Services.AddScoped<IBarcodePoolImportService, BarcodePoolImportService>();
    builder.Services.AddScoped<IBarcodePoolExportService, BarcodePoolExportService>();

    // E-posta
    builder.Services.AddScoped<IEmailService, EmailService>();

    // Veritabanı Yedekleme
    builder.Services.AddScoped<IDatabaseBackupService, DatabaseBackupService>();

    // PTT Uyumlu Export
    builder.Services.AddScoped<IExcelExportService, ExcelExportService>();
    builder.Services.AddScoped<ICsvExportService, CsvExportService>();

    // ExcelImporter & File Reader
    builder.Services.AddScoped<TasraPostaManager.Services.Import.ExcelFileReader>();
    builder.Services.AddScoped<ExcelImporter>(provider =>
    {
        var db = provider.GetRequiredService<AppDbContext>();
        var env = provider.GetRequiredService<IWebHostEnvironment>();
        var serviceProvider = provider.GetRequiredService<IServiceProvider>();
        var logger = provider.GetRequiredService<ILogger<ExcelImporter>>();
        return new ExcelImporter(db, env, serviceProvider, logger);
    });

    // HTTP Client
    builder.Services.AddHttpClient();

    // ─────────────────────────────────────────────────────────
    // 10. ASP.NET IDENTITY — Authentication & Authorization
    // ─────────────────────────────────────────────────────────
    builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        // Şifre politikası (Türkçe dostu)
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 6;

        // Kullanıcı ayarları
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedAccount = false;
        options.SignIn.RequireConfirmedEmail = false;

        // Lockout
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

    // Cookie ayarları
    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

    // ─────────────────────────────────────────────────────────
    // 9. CORS — Geliştirme ortamı
    // ─────────────────────────────────────────────────────────
    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("DevCorsPolicy",
                policy =>
                {
                    policy.WithOrigins("http://localhost:3000", "http://localhost:5000")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
        });
    }

    // ─────────────────────────────────────────────────────────
    // 10. REQUEST BOYUT SINIRLARI
    // ─────────────────────────────────────────────────────────
    builder.Services.Configure<IISServerOptions>(options =>
    {
        options.MaxRequestBodySize = 50 * 1024 * 1024; // 50MB
    });

    builder.Services.Configure<KestrelServerOptions>(options =>
    {
        options.Limits.MaxRequestBodySize = 50 * 1024 * 1024;
        options.Limits.MaxRequestHeadersTotalSize = 64 * 1024;
    });

    // ═════════════════════════════════════════════════════════
    // MIDDLEWARE PIPELINE
    // ═════════════════════════════════════════════════════════
    var app = builder.Build();

    // Global Exception Handling (en üstte olmalı)
    app.UseGlobalExceptionHandling();

    // Serilog Request Logging
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "{RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    });

    // CORS
    if (app.Environment.IsDevelopment())
    {
        app.UseCors("DevCorsPolicy");
    }

    // Localization
    app.UseRequestLocalization();

    // Error Handling
    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseSession();

    // ─────────────────────────────────────────────────────────
    // DATABASE MIGRATIONS
    // ─────────────────────────────────────────────────────────
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (db.Database.GetPendingMigrations().Any())
            {
                Log.Information("Database migrations uygulanıyor...");
                db.Database.Migrate();
                Log.Information("Database migrations başarıyla uygulandı.");
            }
            else
            {
                Log.Information("Database güncel, migration gerekmiyor.");
            }

            var canConnect = await db.Database.CanConnectAsync();
            Log.Information(canConnect
                ? "Database bağlantısı başarılı."
                : "Database bağlantısı başarısız!");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Database başlatma hatası!");
            if (!app.Environment.IsDevelopment())
            {
                throw; // Production'da fail-fast
            }
        }
    }

    // ─────────────────────────────────────────────────────────
    // IDENTITY SEED — Admin kullanıcısı oluşturma
    // ─────────────────────────────────────────────────────────
    using (var seedScope = app.Services.CreateScope())
    {
        var roleManager = seedScope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = seedScope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        string[] roles = { "Admin", "User" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                Log.Information("Rol oluşturuldu: {Role}", role);
            }
        }

        const string adminEmail = "admin@tasraposta.local";
        const string adminPassword = "Admin123!";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new IdentityUser
            {
                UserName = "admin",
                Email = adminEmail,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                Log.Information("Admin kullanıcısı oluşturuldu: {Email}", adminEmail);
            }
            else
            {
                Log.Error("Admin kullanıcısı oluşturulamadı: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }

    // ─────────────────────────────────────────────────────────
    // HEALTH CHECK
    // ─────────────────────────────────────────────────────────
    app.MapGet("/health", () => Results.Ok(new
    {
        Status = "Healthy",
        Timestamp = DateTime.Now,
        Version = "2.0.0",
        Environment = app.Environment.EnvironmentName
    }));

    // ─────────────────────────────────────────────────────────
    // ROUTES
    // ─────────────────────────────────────────────────────────
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    app.MapControllerRoute(
        name: "labels",
        pattern: "Labels/{action=Index}/{selected?}",
        defaults: new { controller = "Labels" });

    app.MapControllerRoute(
        name: "records",
        pattern: "Records/{action=Index}/{id?}",
        defaults: new { controller = "Records" });

    Log.Information("TasraPostaManager başarıyla başlatıldı! Environment: {Environment}", app.Environment.EnvironmentName);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Uygulama başlatılamadı!");
    throw;
}
finally
{
    Log.CloseAndFlush();
}