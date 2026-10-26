using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Enums;
using Staybnb.Models;

namespace Staybnb.Controllers
{
    [Authorize(Roles = "Host")]
    public class HostBookingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HostBookingsController(
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

            var bookings = await _context.Bookings
                .Include(b => b.Property)
                    .ThenInclude(p => p!.Images)
                .Include(b => b.Guest)
                .Where(b => b.Property != null
                    && b.Property.HostId == hostId)
                .OrderByDescending(b => b.CheckInDate)
                .ToListAsync();

            return View(bookings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var hostId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(hostId))
            {
                return Challenge();
            }

            var booking = await _context.Bookings
                .Include(b => b.Property)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null || booking.Property == null)
            {
                return NotFound();
            }

            if (booking.Property.HostId != hostId)
            {
                return Forbid();
            }

            if (booking.Status != BookingStatus.Pending)
            {
                TempData["ErrorMessage"] =
                    "Only pending booking requests can be approved.";

                return RedirectToAction(nameof(Index));
            }

            var hasConflict = await _context.Bookings.AnyAsync(b =>
                b.PropertyId == booking.PropertyId
                && b.Id != booking.Id
                && b.Status == BookingStatus.Approved
                && b.CheckInDate < booking.CheckOutDate
                && b.CheckOutDate > booking.CheckInDate);

            if (hasConflict)
            {
                TempData["ErrorMessage"] =
                    "This booking overlaps with another approved booking. It cannot be approved.";

                return RedirectToAction(nameof(Index));
            }

            booking.Status = BookingStatus.Approved;

            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = hostId,
                Action = $"Approved booking #{booking.Id} for property \"{booking.Property.Title}\"",
                ActivityType = ActivityType.BookingStatusUpdate,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Booking request approved successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var hostId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(hostId))
            {
                return Challenge();
            }

            var booking = await _context.Bookings
                .Include(b => b.Property)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null || booking.Property == null)
            {
                return NotFound();
            }

            if (booking.Property.HostId != hostId)
            {
                return Forbid();
            }

            if (booking.Status != BookingStatus.Pending)
            {
                TempData["ErrorMessage"] =
                    "Only pending booking requests can be rejected.";

                return RedirectToAction(nameof(Index));
            }

            booking.Status = BookingStatus.Rejected;

            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = hostId,
                Action = $"Rejected booking #{booking.Id} for property \"{booking.Property.Title}\"",
                ActivityType = ActivityType.BookingStatusUpdate,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Booking request rejected.";

            return RedirectToAction(nameof(Index));
        }
    }
}