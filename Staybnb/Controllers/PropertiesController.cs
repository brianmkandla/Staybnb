
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;

namespace Staybnb.Controllers
{
    public class PropertiesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PropertiesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var properties = await _context.HostProperties
                .Where(property => property.IsActive)
                .Include(property => property.Images)
                .OrderBy(property => property.City)
                .ToListAsync();

            return View(properties);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var property = await _context.HostProperties
                .Where(property => property.IsActive && property.Id == id)
                .Include(property => property.Images)
                .Include(property => property.Host)
                .FirstOrDefaultAsync();

            if (property == null)
            {
                return NotFound();
            }

            return View(property);
        }
    }
}
