using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Enums;
using Staybnb.Models;

namespace Staybnb.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class AdminHostApplicationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AdminHostApplicationsController> _logger;

        public AdminHostApplicationsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<AdminHostApplicationsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var applications = await _context.HostApplications
                .Include(application => application.Applicant)
                .Include(application => application.Property)
                    .ThenInclude(property => property.Images)
                .OrderByDescending(application => application.AppliedAt)
                .ToListAsync();

            return View(applications);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var application = await _context.HostApplications
                .Include(application => application.Applicant)
                .Include(application => application.Property)
                .FirstOrDefaultAsync(application => application.Id == id);

            if (application == null)
            {
                return NotFound();
            }

            if (application.Status != ApplicationStatus.Pending)
            {
                TempData["ErrorMessage"] =
                    "This application has already been reviewed.";

                return RedirectToAction(nameof(Index));
            }

            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return Challenge();
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var applicant = application.Applicant;

                if (!await _userManager.IsInRoleAsync(applicant, "Host"))
                {
                    var addHostResult =
                        await _userManager.AddToRoleAsync(applicant, "Host");

                    if (!addHostResult.Succeeded)
                    {
                        await transaction.RollbackAsync();

                        _logger.LogWarning(
                            "Could not assign Host role to user {UserId}",
                            applicant.Id);

                        TempData["ErrorMessage"] =
                            "The Host role could not be assigned. No approval was saved.";

                        return RedirectToAction(nameof(Index));
                    }
                }

                if (await _userManager.IsInRoleAsync(applicant, "Guest"))
                {
                    var removeGuestResult =
                        await _userManager.RemoveFromRoleAsync(
                            applicant, "Guest");

                    if (!removeGuestResult.Succeeded)
                    {
                        await transaction.RollbackAsync();

                        _logger.LogWarning(
                            "Could not remove Guest role from user {UserId}",
                            applicant.Id);

                        TempData["ErrorMessage"] =
                            "The applicant's role could not be updated. No approval was saved.";

                        return RedirectToAction(nameof(Index));
                    }
                }

                application.Status = ApplicationStatus.Approved;
                application.Property.IsActive = true;

                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserId = currentUser.Id,
                    Action = $"Approved host application #{application.Id} for {applicant.Email ?? applicant.Id}",
                    ActivityType = ActivityType.HostApplicationUpdate,
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] =
                    "Application approved. The applicant is now a Host.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                _logger.LogError(
                    ex,
                    "Failed to approve host application {ApplicationId}",
                    id);

                TempData["ErrorMessage"] =
                    "Something went wrong while approving the application.";

                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var application = await _context.HostApplications
                .Include(application => application.Applicant)
                .Include(application => application.Property)
                .FirstOrDefaultAsync(application => application.Id == id);

            if (application == null)
            {
                return NotFound();
            }

            if (application.Status != ApplicationStatus.Pending)
            {
                TempData["ErrorMessage"] =
                    "This application has already been reviewed.";

                return RedirectToAction(nameof(Index));
            }

            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return Challenge();
            }

            application.Status = ApplicationStatus.Rejected;
            application.Property.IsActive = false;

            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = currentUser.Id,
                Action = $"Rejected host application #{application.Id} for {application.Applicant.Email ?? application.Applicant.Id}",
                ActivityType = ActivityType.HostApplicationUpdate,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "The host application was rejected.";

            return RedirectToAction(nameof(Index));
        }
    }
}