using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TasraPostaManager.Data;
using TasraPostaManager.Models;

namespace TasraPostaManager.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(AppDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var dashboardStats = await GetDashboardStatistics();

            ViewBag.TotalRecords = dashboardStats.TotalRecords;
            ViewBag.ParaliCount = dashboardStats.ParaliCount;
            ViewBag.ParaliDegilCount = dashboardStats.ParaliDegilCount;
            ViewBag.TotalMiktar = dashboardStats.TotalMiktar;
            ViewBag.ThisMonthCount = dashboardStats.ThisMonthCount;
            ViewBag.TotalLabels = dashboardStats.TotalLabels;
            ViewBag.RecentActivities = dashboardStats.RecentActivities;

            return View();
        }

        private async Task<DashboardStats> GetDashboardStatistics()
        {
            var stats = new DashboardStats();

            try
            {
                stats.TotalRecords = await _context.PostaRecords.CountAsync();

                stats.ParaliCount = await _context.PostaRecords
                    .Where(r => r.Miktar > 0)
                    .CountAsync();

                stats.ParaliDegilCount = await _context.PostaRecords
                    .Where(r => r.Miktar == 0 || !r.Miktar.HasValue)
                    .CountAsync();

                stats.TotalMiktar = await _context.PostaRecords
                    .Where(r => r.Miktar > 0)
                    .SumAsync(r => r.Miktar ?? 0);

                var today = DateTime.Today;
                var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
                stats.ThisMonthCount = await _context.PostaRecords
                    .Where(r => r.CreatedAt >= firstDayOfMonth)
                    .CountAsync();

                // Barkod numarasi atanmis kayitlarin sayisi
                stats.TotalLabels = await _context.PostaRecords
                    .Where(r => r.BarkodNo != null && r.BarkodNo != "")
                    .CountAsync();

                // Son islemler - gercek veriye dayali
                var recentRecords = await _context.PostaRecords
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(5)
                    .Select(r => new RecentActivity
                    {
                        Title = r.MuhabereNo + " - " + (r.GittigiYer ?? "Bilinmiyor"),
                        Subtitle = r.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
                        Icon = r.Miktar > 0 ? "fa-coins text-success" : "fa-envelope text-primary",
                        Type = r.ListeTipi == ListeTipi.Parali ? "Parali" : "Ucretsiz"
                    })
                    .ToListAsync();

                stats.RecentActivities = recentRecords;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dashboard istatistikleri yuklenirken hata olustu");

                stats.TotalRecords = 0;
                stats.ParaliCount = 0;
                stats.ParaliDegilCount = 0;
                stats.TotalMiktar = 0;
                stats.ThisMonthCount = 0;
                stats.TotalLabels = 0;
                stats.RecentActivities = new List<RecentActivity>();
            }

            return stats;
        }
    }

    public class DashboardStats
    {
        public int TotalRecords { get; set; }
        public int ParaliCount { get; set; }
        public int ParaliDegilCount { get; set; }
        public decimal TotalMiktar { get; set; }
        public int ThisMonthCount { get; set; }
        public int TotalLabels { get; set; }
        public List<RecentActivity> RecentActivities { get; set; } = new();
    }

    public class RecentActivity
    {
        public string Title { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public string Icon { get; set; } = "fa-info-circle text-muted";
        public string Type { get; set; } = "";
    }
}