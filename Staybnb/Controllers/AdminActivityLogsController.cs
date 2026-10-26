using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;

namespace Staybnb.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class AdminActivityLogsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminActivityLogsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var logs = await _context.ActivityLogs
                .AsNoTracking()
                .Include(log => log.User)
                .OrderByDescending(log => log.CreatedAt)
                .Take(200)
                .ToListAsync();

            return View(logs);
        }
    }
}
