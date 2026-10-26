using System.Text.Json;
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
    public class GuestCheckInsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        private const long MaximumFileSize = 5 * 1024 * 1024;

        public GuestCheckInsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        private string GetDocumentFolder()
        {
            return Path.Combine(
                _environment.ContentRootPath,
                "App_Data",
                "GuestDocuments");
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var guestId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(guestId))
            {
                return Challenge();
            }

            var bookings = await _context.Bookings
                .Where(b => b.GuestId == guestId
                    && b.Status == BookingStatus.Approved)
                .Include(b => b.Property)
                .OrderByDescending(b => b.CheckInDate)
                .ToListAsync();

            var propertyIds = bookings
                .Select(b => b.PropertyId)
                .Distinct()
                .ToList();

            var bookingIds = bookings
                .Select(b => b.Id)
                .ToList();

            ViewBag.Processes = await _context.CheckInProcesses
                .Where(p => propertyIds.Contains(p.PropertyId))
                .ToDictionaryAsync(p => p.PropertyId);

            ViewBag.ExistingCheckIns = await _context.GuestCheckIns
                .Where(c => bookingIds.Contains(c.BookingId))
                .ToDictionaryAsync(c => c.BookingId);

            return View(bookings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Start(int bookingId)
        {
            var guestId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(guestId))
            {
                return Challenge();
            }

            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b =>
                    b.Id == bookingId
                    && b.GuestId == guestId
                    && b.Status == BookingStatus.Approved);

            if (booking == null)
            {
                return NotFound();
            }

            var process = await _context.CheckInProcesses
                .FirstOrDefaultAsync(p => p.PropertyId == booking.PropertyId);

            if (process == null)
            {
                TempData["ErrorMessage"] =
                    "The host has not configured check-in instructions for this property.";

                return RedirectToAction(nameof(Index));
            }

            var existingCheckIn = await _context.GuestCheckIns
                .FirstOrDefaultAsync(c => c.BookingId == bookingId);

            if (existingCheckIn != null)
            {
                return RedirectToAction(
                    nameof(Details),
                    new { id = existingCheckIn.Id });
            }

            var checkIn = new GuestCheckIn
            {
                BookingId = booking.Id,
                CheckInProcessId = process.Id,
                Status = CheckInStatus.NotStarted,
                CreatedAt = DateTime.UtcNow
            };

            _context.GuestCheckIns.Add(checkIn);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = checkIn.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var guestId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(guestId))
            {
                return Challenge();
            }

            var checkIn = await _context.GuestCheckIns
                .Include(c => c.Booking)
                    .ThenInclude(b => b.Property)
                .Include(c => c.CheckInProcess)
                .FirstOrDefaultAsync(c =>
                    c.Id == id && c.Booking.GuestId == guestId);

            if (checkIn == null)
            {
                return NotFound();
            }

            try
            {
                ViewBag.Instructions =
                    JsonSerializer.Deserialize<List<string>>(
                        checkIn.CheckInProcess.StepsJson)
                    ?? new List<string>();
            }
            catch (JsonException)
            {
                ViewBag.Instructions = new List<string>();
            }

            ViewBag.Documents = await _context.GuestDocuments
                .Where(d => d.GuestCheckInId == checkIn.Id)
                .OrderByDescending(d => d.Id)
                .ToListAsync();

            return View(checkIn);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDocument(
            int checkInId,
            string documentType,
            IFormFile? file)
        {
            var guestId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(guestId))
            {
                return Challenge();
            }

            var checkIn = await _context.GuestCheckIns
                .Include(c => c.Booking)
                .FirstOrDefaultAsync(c =>
                    c.Id == checkInId
                    && c.Booking.GuestId == guestId);

            if (checkIn == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(documentType)
                || documentType.Trim().Length > 100)
            {
                TempData["ErrorMessage"] =
                    "Enter a document type of no more than 100 characters.";

                return RedirectToAction(nameof(Details), new { id = checkInId });
            }

            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Choose a document to upload.";

                return RedirectToAction(nameof(Details), new { id = checkInId });
            }

            if (file.Length > MaximumFileSize)
            {
                TempData["ErrorMessage"] =
                    "The document must be 5 MB or smaller.";

                return RedirectToAction(nameof(Details), new { id = checkInId });
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };

            if (!allowedExtensions.Contains(extension))
            {
                TempData["ErrorMessage"] =
                    "Only PDF, JPG, JPEG and PNG documents are allowed.";

                return RedirectToAction(nameof(Details), new { id = checkInId });
            }

            if (!await HasValidSignatureAsync(file, extension))
            {
                TempData["ErrorMessage"] =
                    "The file content does not match its file type.";

                return RedirectToAction(nameof(Details), new { id = checkInId });
            }

            var uploadFolder = GetDocumentFolder();
            Directory.CreateDirectory(uploadFolder);

            // Generate the stored name; never trust the uploaded filename.
            var storedFileName = $"{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadFolder, storedFileName);

            try
            {
                await using (var stream = new FileStream(
                    filePath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None))
                {
                    await file.CopyToAsync(stream);
                }

                var document = new GuestDocument
                {
                    GuestCheckInId = checkIn.Id,
                    DocumentType = documentType.Trim(),
                    FileName = storedFileName,
                    Status = DocumentStatus.Pending
                };

                _context.GuestDocuments.Add(document);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Your document was uploaded for review.";
            }
            catch
            {
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                throw;
            }

            return RedirectToAction(nameof(Details), new { id = checkInId });
        }

        [HttpGet]
        public async Task<IActionResult> DownloadDocument(int id)
        {
            var guestId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(guestId))
            {
                return Challenge();
            }

            var document = await _context.GuestDocuments
                .Include(d => d.GuestCheckIn)
                    .ThenInclude(c => c.Booking)
                .FirstOrDefaultAsync(d =>
                    d.Id == id
                    && d.GuestCheckIn.Booking.GuestId == guestId);

            if (document == null)
            {
                return NotFound();
            }

            // Only generated filenames are valid. Reject stored paths.
            if (string.IsNullOrWhiteSpace(document.FileName)
                || document.FileName != Path.GetFileName(document.FileName))
            {
                return NotFound();
            }

            var filePath = Path.Combine(
                GetDocumentFolder(),
                document.FileName);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            var extension = Path.GetExtension(document.FileName)
                .ToLowerInvariant();

            var contentType = extension switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };

            var downloadName = Path.GetFileName(
                $"{document.DocumentType}{extension}");

            var stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);

            return File(stream, contentType, downloadName);
        }

        private static async Task<bool> HasValidSignatureAsync(
            IFormFile file,
            string extension)
        {
            await using var stream = file.OpenReadStream();

            var header = new byte[8];
            var bytesRead = await stream.ReadAsync(header.AsMemory(0, 8));

            return extension switch
            {
                ".pdf" => bytesRead >= 5
                    && header[0] == 0x25
                    && header[1] == 0x50
                    && header[2] == 0x44
                    && header[3] == 0x46
                    && header[4] == 0x2D,

                ".jpg" or ".jpeg" => bytesRead >= 3
                    && header[0] == 0xFF
                    && header[1] == 0xD8
                    && header[2] == 0xFF,

                ".png" => bytesRead >= 8
                    && header[0] == 0x89
                    && header[1] == 0x50
                    && header[2] == 0x4E
                    && header[3] == 0x47
                    && header[4] == 0x0D
                    && header[5] == 0x0A
                    && header[6] == 0x1A
                    && header[7] == 0x0A,

                _ => false
            };
        }
    }
}