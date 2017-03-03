using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using MusicStoreB2C.Models;
using MusicStoreB2C.ViewModels;
using Microsoft.Extensions.Logging;

namespace MusicStoreB2C.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppSettings _appSettings;

        public HomeController(IOptions<AppSettings> options, ILoggerFactory loggerFactory)
        {
            _appSettings = options.Value;
            _logger = loggerFactory.CreateLogger<HomeController>();
        }
        //
        // GET: /Home/
        public async Task<IActionResult> Index(
            [FromServices] MusicStoreContext dbContext,
            [FromServices] IMemoryCache cache)
        {
            _logger.LogInformation("HomeController.Index()");
            // Get most popular albums
            var cacheKey = "topselling";
            List<Album> albums;
            if (!cache.TryGetValue(cacheKey, out albums))
            {
                albums = await GetTopSellingAlbumsAsync(dbContext, 6);

                if (albums != null && albums.Count > 0)
                {
                    if (_appSettings.CacheDbResults)
                    {
                        // Refresh it every 10 minutes.
                        // Let this be the last item to be removed by cache if cache GC kicks in.
                        cache.Set(
                            cacheKey,
                            albums,
                            new MemoryCacheEntryOptions()
                            .SetAbsoluteExpiration(TimeSpan.FromMinutes(10))
                            .SetPriority(CacheItemPriority.High));
                    }
                }
            }

            return View(albums);
        }

        public async Task<IActionResult> About()
        {
            _logger.LogInformation("HomeController.About()");
            ViewData["Message"] = String.Format("Claims available for the user {0}", (User.FindFirst("name")?.Value));
            ViewData["ADMessage"] = String.Format("AD User Properties available for the user {0}", (User.FindFirst("name")?.Value));
            AboutViewModel model = new AboutViewModel
            {
                ADUserProps = await Startup.adB2C.GetUserProperties(User).Result
            };
            return View(model);
        }

        public IActionResult Contact()
        {
            _logger.LogInformation("HomeController.Contact()");
            ViewData["Message"] = "Your contact page.";

            return View();
        }
        public IActionResult Error()
        {
            _logger.LogInformation("HomeController.Error()");
            return View("~/Views/Shared/Error.cshtml");
        }

        public IActionResult StatusCodePage()
        {
            _logger.LogInformation("HomeController.StatusCodePage()");
            return View("~/Views/Shared/StatusCodePage.cshtml");
        }

        public IActionResult AccessDenied()
        {
            _logger.LogInformation("HomeController.AccessDenied()");
            return View("~/Views/Shared/AccessDenied.cshtml");
        }

        private Task<List<Album>> GetTopSellingAlbumsAsync(MusicStoreContext dbContext, int count)
        {
            // Group the order details by album and return
            // the albums with the highest count

            return dbContext.Albums
                .OrderByDescending(a => a.OrderDetails.Count)
                .Take(count)
                .ToListAsync();
        }
    }
}