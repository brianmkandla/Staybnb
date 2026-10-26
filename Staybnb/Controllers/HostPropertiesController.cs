
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Models;

namespace Staybnb.Controllers
{
    [Authorize(Roles = "Host")]
    public class HostPropertiesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HostPropertiesController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var hostId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(hostId))
            {
                return Challenge();
            }

            var properties = await _context.HostProperties
                .Where(p => p.HostId == hostId)
                .Include(p => p.Images)
                .OrderBy(p => p.Title)
                .ToListAsync();

            return View(properties);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var hostId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(hostId))
            {
                return Challenge();
            }

            var property = await _context.HostProperties
                .FirstOrDefaultAsync(p =>
                    p.Id == id && p.HostId == hostId);

            if (property == null)
            {
                return NotFound();
            }

            property.IsActive = !property.IsActive;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = property.IsActive
                ? $"'{property.Title}' is now visible to guests."
                : $"'{property.Title}' has been deactivated.";

            return RedirectToAction(nameof(Index));
        }
    }
}
