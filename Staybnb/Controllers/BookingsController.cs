using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Enums;
using Staybnb.Models;

namespace Staybnb.Controllers
{
    [Authorize(Roles = "Guest")]
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public BookingsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Create(int propertyId)
        {
            var guestId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(guestId))
            {
                return Challenge();
            }

            var property = await _context.HostProperties
                .FirstOrDefaultAsync(p =>
                    p.Id == propertyId && p.IsActive);

            if (property == null)
            {
                return NotFound();
            }

            ViewBag.Property = property;

            return View(new Booking
            {
                PropertyId = property.Id,
                GuestId = guestId,
                CheckInDate = DateTime.Today.AddDays(1),
                CheckOutDate = DateTime.Today.AddDays(2),
                NumberOfTravelers = 1
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int propertyId,
            DateTime checkInDate,
            DateTime checkOutDate,
            int numberOfTravelers)
        {
            var guestId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(guestId))
            {
                return Challenge();
            }

            var property = await _context.HostProperties
                .FirstOrDefaultAsync(p =>
                    p.Id == propertyId && p.IsActive);

            if (property == null)
            {
                return NotFound();
            }

            ViewBag.Property = property;

            if (!ModelState.IsValid
                || checkInDate.Date < DateTime.Today
                || checkOutDate.Date <= checkInDate.Date
                || numberOfTravelers < 1
                || numberOfTravelers > 20)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Enter valid dates and a traveller count between 1 and 20.");

                return View("Create", new Booking
                {
                    PropertyId = propertyId,
                    GuestId = guestId,
                    CheckInDate = checkInDate,
                    CheckOutDate = checkOutDate,
                    NumberOfTravelers = numberOfTravelers
                });
            }

            var hasConflict = await _context.Bookings.AnyAsync(b =>
                b.PropertyId == property.Id
                && b.Status == BookingStatus.Approved
                && b.CheckInDate < checkOutDate.Date
                && b.CheckOutDate > checkInDate.Date);

            if (hasConflict)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "This property is already booked for some or all of those dates.");

                return View("Create", new Booking
                {
                    PropertyId = propertyId,
                    GuestId = guestId,
                    CheckInDate = checkInDate,
                    CheckOutDate = checkOutDate,
                    NumberOfTravelers = numberOfTravelers
                });
            }

            var nights =
                (checkOutDate.Date - checkInDate.Date).Days;

            var totalPrice =
                (property.PricePerNight * nights)
                + property.CleaningFee
                + property.ServiceFee;

            var booking = new Booking
            {
                PropertyId = property.Id,
                GuestId = guestId,
                CheckInDate = checkInDate.Date,
                CheckOutDate = checkOutDate.Date,
                NumberOfTravelers = numberOfTravelers,
                TotalPrice = totalPrice
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = guestId,
                Action = $"Submitted booking request #{booking.Id} for property \"{property.Title}\"",
                ActivityType = ActivityType.BookingStatusUpdate,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Your booking request has been submitted.";

            return RedirectToAction(nameof(MyBookings));
        }

        [HttpGet]
        public async Task<IActionResult> MyBookings()
        {
            var guestId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(guestId))
            {
                return Challenge();
            }

            var bookings = await _context.Bookings
                .Where(b => b.GuestId == guestId)
                .Include(b => b.Property)
                    .ThenInclude(p => p.Images)
                .OrderByDescending(b => b.CheckInDate)
                .ToListAsync();

            return View(bookings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var guestId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(guestId))
            {
                return Challenge();
            }

            var booking = await _context.Bookings
                .Include(b => b.Property)
                .FirstOrDefaultAsync(b =>
                    b.Id == id && b.GuestId == guestId);

            if (booking == null)
            {
                return NotFound();
            }

            if (booking.Status != BookingStatus.Pending)
            {
                TempData["ErrorMessage"] =
                    "Only pending booking requests can be cancelled.";

                return RedirectToAction(nameof(MyBookings));
            }

            booking.Status = BookingStatus.Cancelled;

            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = guestId,
                Action = $"Cancelled booking #{booking.Id} for property \"{booking.Property?.Title ?? "Unknown property"}\"",
                ActivityType = ActivityType.BookingStatusUpdate,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Your booking request has been cancelled.";

            return RedirectToAction(nameof(MyBookings));
        }
    }
}