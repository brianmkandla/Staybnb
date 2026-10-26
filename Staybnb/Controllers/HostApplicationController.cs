
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
    public class HostApplicationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<HostApplicationController> _logger;

        private const long MaxImageSize = 5 * 1024 * 1024;

        public HostApplicationController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment,
            ILogger<HostApplicationController> logger)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new HostApplicationViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            HostApplicationViewModel model)
        {
            var images = model.Images?
                .Where(image => image != null && image.Length > 0)
                .ToList() ?? new List<IFormFile>();

            if (images.Count == 0)
            {
                ModelState.AddModelError(
                    nameof(model.Images),
                    "Upload at least one property image.");
            }

            foreach (var image in images)
            {
                if (image.Length > MaxImageSize)
                {
                    ModelState.AddModelError(
                        nameof(model.Images),
                        "Each image must be 5 MB or smaller.");
                    break;
                }

                if (!await HasValidImageSignature(image))
                {
                    ModelState.AddModelError(
                        nameof(model.Images),
                        "Only valid JPG, PNG or WebP images are allowed.");
                    break;
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var webRoot = _environment.WebRootPath
                ?? Path.Combine(_environment.ContentRootPath, "wwwroot");

            var uploadFolder = Path.Combine(
                webRoot, "uploads", "properties");

            var savedFiles = new List<string>();

            try
            {
                Directory.CreateDirectory(uploadFolder);

                await using var transaction =
                    await _context.Database.BeginTransactionAsync();

                var property = new HostProperty
                {
                    Title = model.Title,
                    Description = model.Description,
                    PricePerNight = model.PricePerNight,
                    Address = model.Address,
                    City = model.City,
                    PropertyType = model.PropertyType,
                    CleaningFee = model.CleaningFee,
                    ServiceFee = model.ServiceFee,
                    HostId = userId,
                    IsActive = false
                };

                _context.HostProperties.Add(property);
                await _context.SaveChangesAsync();

                foreach (var image in images)
                {
                    var extension =
                        Path.GetExtension(image.FileName).ToLowerInvariant();

                    var fileName = $"{Guid.NewGuid():N}{extension}";
                    var filePath = Path.Combine(uploadFolder, fileName);

                    await using (var fileStream = new FileStream(
                        filePath, FileMode.CreateNew, FileAccess.Write))
                    {
                        await image.CopyToAsync(fileStream);
                    }

                    savedFiles.Add(filePath);

                    property.Images.Add(new PropertyImage
                    {
                        ImageUrl = $"/uploads/properties/{fileName}"
                    });
                }

                var application = new HostApplication
                {
                    ApplicationUserId = userId,
                    PropertyId = property.Id,
                    Status = ApplicationStatus.Pending,
                    AppliedAt = DateTime.UtcNow
                };

                _context.HostApplications.Add(application);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] =
                    "Your host application has been submitted for review.";

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                foreach (var filePath in savedFiles)
                {
                    try
                    {
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }
                    }
                    catch (IOException cleanupException)
                    {
                        _logger.LogWarning(
                            cleanupException,
                            "Could not remove uploaded file {FilePath}",
                            filePath);
                    }
                }

                _logger.LogError(
                    ex, "Failed to save a host application for user {UserId}",
                    userId);

                ModelState.AddModelError(
                    string.Empty,
                    "We couldn't save your application. Please try again.");

                return View(model);
            }
        }

        private static async Task<bool> HasValidImageSignature(
            IFormFile image)
        {
            var extension =
                Path.GetExtension(image.FileName).ToLowerInvariant();

            if (extension is not ".jpg" and not ".jpeg"
                and not ".png" and not ".webp")
            {
                return false;
            }

            var header = new byte[12];

            await using var stream = image.OpenReadStream();

            var bytesRead = await stream.ReadAsync(header.AsMemory());

            if (extension is ".jpg" or ".jpeg")
            {
                return bytesRead >= 3
                    && header[0] == 0xFF
                    && header[1] == 0xD8
                    && header[2] == 0xFF;
            }

            if (extension == ".png")
            {
                byte[] pngSignature =
                    { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

                return bytesRead >= 8
                    && header.Take(8).SequenceEqual(pngSignature);
            }

            return bytesRead >= 12
                && header[0] == 'R'
                && header[1] == 'I'
                && header[2] == 'F'
                && header[3] == 'F'
                && header[8] == 'W'
                && header[9] == 'E'
                && header[10] == 'B'
                && header[11] == 'P';
        }
    }
}