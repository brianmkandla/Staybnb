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
    public class HostGuestDocumentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public HostGuestDocumentsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        private string? CurrentHostId => _userManager.GetUserId(User);

        private IQueryable<GuestDocument> HostDocuments(string hostId)
        {
            return _context.GuestDocuments
                .Include(d => d.GuestCheckIn)
                    .ThenInclude(c => c.Booking)
                        .ThenInclude(b => b.Property)
                .Include(d => d.GuestCheckIn)
                    .ThenInclude(c => c.Booking)
                        .ThenInclude(b => b.Guest)
                .Where(d =>
                    d.GuestCheckIn.Booking.Property.HostId == hostId);
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var hostId = CurrentHostId;

            if (string.IsNullOrEmpty(hostId))
                return Challenge();

            var documents = await HostDocuments(hostId)
                .AsNoTracking()
                .OrderBy(d => d.Status)
                .ThenByDescending(d => d.Id)
                .ToListAsync();

            return View(documents);
        }

        [HttpGet]
        public async Task<IActionResult> Download(int id)
        {
            var hostId = CurrentHostId;

            if (string.IsNullOrEmpty(hostId))
                return Challenge();

            var document = await HostDocuments(hostId)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (document == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(document.FileName) ||
                document.FileName != Path.GetFileName(document.FileName))
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
                _ => null
            };

            if (contentType == null)
                return NotFound();

            var folder = Path.Combine(
                _environment.ContentRootPath,
                "App_Data",
                "GuestDocuments");

            var filePath = Path.Combine(folder, document.FileName);

            if (!System.IO.File.Exists(filePath))
                return NotFound();

            var downloadName = $"{document.DocumentType}{extension}";

            var stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);

            return File(stream, contentType, downloadName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Approve(int id)
        {
            return UpdateStatus(
                id,
                DocumentStatus.Approved,
                null);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Reject(
            int id,
            string? rejectionReason)
        {
            return UpdateStatus(
                id,
                DocumentStatus.Rejected,
                rejectionReason);
        }

        private async Task<IActionResult> UpdateStatus(
            int id,
            DocumentStatus status,
            string? rejectionReason)
        {
            var hostId = CurrentHostId;

            if (string.IsNullOrEmpty(hostId))
                return Challenge();

            if (status != DocumentStatus.Approved &&
                status != DocumentStatus.Rejected)
            {
                return BadRequest();
            }

            if (status == DocumentStatus.Rejected)
            {
                rejectionReason = rejectionReason?.Trim();

                if (string.IsNullOrWhiteSpace(rejectionReason))
                {
                    TempData["ErrorMessage"] =
                        "Please enter a reason before rejecting a document.";

                    return RedirectToAction(nameof(Index));
                }

                if (rejectionReason.Length > 500)
                {
                    TempData["ErrorMessage"] =
                        "The rejection reason cannot exceed 500 characters.";

                    return RedirectToAction(nameof(Index));
                }
            }

            var document = await HostDocuments(hostId)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (document == null)
                return NotFound();

            if (document.Status != DocumentStatus.Pending)
            {
                TempData["ErrorMessage"] =
                    "Only pending documents can be reviewed.";

                return RedirectToAction(nameof(Index));
            }

            document.Status = status;
            document.RejectionReason =
                status == DocumentStatus.Rejected
                    ? rejectionReason
                    : null;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                status == DocumentStatus.Approved
                    ? "Document approved successfully."
                    : "Document rejected. The guest can now see your reason.";

            return RedirectToAction(nameof(Index));
        }
    }
}