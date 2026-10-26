
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Models;

namespace Staybnb.Controllers
{
    [Authorize(Roles = "Host")]
    public class HostCheckInProcessesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HostCheckInProcessesController(
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

            var processes = await _context.CheckInProcesses
                .Where(c => c.Property.HostId == hostId)
                .ToDictionaryAsync(c => c.PropertyId);

            ViewBag.Processes = processes;

            return View(properties);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int propertyId)
        {
            var hostId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(hostId))
            {
                return Challenge();
            }

            var property = await _context.HostProperties
                .FirstOrDefaultAsync(p =>
                    p.Id == propertyId && p.HostId == hostId);

            if (property == null)
            {
                return NotFound();
            }

            var process = await _context.CheckInProcesses
                .FirstOrDefaultAsync(c => c.PropertyId == propertyId);

            ViewBag.Property = property;
            ViewBag.ProcessId = process?.Id ?? 0;
            ViewBag.TitleValue = process?.Title ?? "Guest Check-in";
            ViewBag.StepsValue = process == null
                ? string.Empty
                : string.Join(
                    Environment.NewLine,
                    JsonSerializer.Deserialize<List<string>>(
                        process.StepsJson) ?? new List<string>());

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int propertyId,
            string title,
            string steps,
            string requiredDocuments)
        {
            var hostId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(hostId))
            {
                return Challenge();
            }

            var property = await _context.HostProperties
                .FirstOrDefaultAsync(p =>
                    p.Id == propertyId && p.HostId == hostId);

            if (property == null)
            {
                return NotFound();
            }

            ViewBag.Property = property;
            ViewBag.ProcessId = 0;
            ViewBag.TitleValue = title;
            ViewBag.StepsValue = steps;

            if (string.IsNullOrWhiteSpace(title)
                || title.Length > 200
                || string.IsNullOrWhiteSpace(steps)
                || string.IsNullOrWhiteSpace(requiredDocuments))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Enter a title, check-in instructions and required documents.");

                return View();
            }

            var instructionList = steps
                .Split(
                    new[] { "\r\n", "\n" },
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries)
                .ToList();

            var documentList = requiredDocuments
                .Split(
                    new[] { "\r\n", "\n" },
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries)
                .ToList();

            if (instructionList.Count == 0 || documentList.Count == 0)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Provide at least one instruction and one required document.");

                return View();
            }

            var savedSteps = new List<string>
            {
                "CHECK-IN INSTRUCTIONS"
            };

            savedSteps.AddRange(instructionList);
            savedSteps.Add("REQUIRED DOCUMENTS");
            savedSteps.AddRange(documentList);

            var process = await _context.CheckInProcesses
                .FirstOrDefaultAsync(c => c.PropertyId == propertyId);

            if (process == null)
            {
                process = new CheckInProcess
                {
                    PropertyId = propertyId,
                    Title = title.Trim(),
                    StepsJson = JsonSerializer.Serialize(savedSteps),
                    CreatedAt = DateTime.UtcNow
                };

                _context.CheckInProcesses.Add(process);
            }
            else
            {
                process.Title = title.Trim();
                process.StepsJson = JsonSerializer.Serialize(savedSteps);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "The check-in instructions have been saved.";

            return RedirectToAction(nameof(Index));
        }
    }
}
