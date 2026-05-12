using System.Diagnostics;
using FluentValidation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using PeopleManagement.Application.People;
using PeopleManagement.Domain.People;
using PeopleManagement.Web.Models;

namespace PeopleManagement.Web.Controllers;

public class PeopleController : Controller
{
    private readonly IPeopleService _peopleService;
    private readonly ILogger<PeopleController> _logger;
    private readonly IWebHostEnvironment _environment;

    public PeopleController(IPeopleService peopleService, ILogger<PeopleController> logger, IWebHostEnvironment environment)
    {
        _peopleService = peopleService ?? throw new ArgumentNullException(nameof(peopleService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    /// <summary>Lists all people with optional search.</summary>
    public async Task<IActionResult> Index(string? search, CancellationToken cancellationToken)
    {
        try
        {
            IReadOnlyList<Person> people;

            if (!string.IsNullOrWhiteSpace(search))
            {
                people = await _peopleService.SearchAsync(search, cancellationToken);
            }
            else
            {
                people = await _peopleService.ListAsync(cancellationToken);
            }

            var viewModel = new PeopleIndexViewModel
            {
                People = people,
                SearchQuery = search
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading people list");
            TempData["Error"] = "אירעה שגיאה בזמן טעינת רשימת האנשים. נסו שוב.";
            return View(new PeopleIndexViewModel { People = new List<Person>() });
        }
    }

    /// <summary>Displays create person form.</summary>
    public IActionResult Create()
    {
        return View(new CreatePersonViewModel());
    }

    /// <summary>Handles person creation with optional photo upload.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreatePersonViewModel model, IFormFile? photoFile, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            byte[]? photoContent = null;
            string? photoFileName = null;
            string? photoContentType = null;

            // Handle photo upload if provided
            if (photoFile != null && photoFile.Length > 0)
            {
                using var memoryStream = new MemoryStream();
                await photoFile.CopyToAsync(memoryStream, cancellationToken);

                photoContent = memoryStream.ToArray();
                photoFileName = photoFile.FileName;
                photoContentType = NormalizePhotoContentType(photoFile.FileName, photoFile.ContentType);
            }

            var input = new CreatePersonInput
            {
                FullName = model.FullName,
                Email = model.Email,
                Phone = model.Phone,
                PhotoContent = photoContent,
                PhotoFileName = photoFileName,
                PhotoContentType = photoContentType
            };

            var personId = await _peopleService.CreateAsync(input, cancellationToken);

            TempData["Success"] = "האדם נוסף בהצלחה.";
            return RedirectToAction(nameof(Index));
        }
        catch (FluentValidation.ValidationException ex)
        {
            foreach (var error in ex.Errors)
            {
                ModelState.AddModelError(string.IsNullOrEmpty(error.PropertyName) ? "Create" : error.PropertyName, error.ErrorMessage);
            }
            return View(model);
        }
        catch (DbUpdateException ex) when (IsDuplicateEmailViolation(ex))
        {
            _logger.LogWarning(ex, "Attempted to create a person with a duplicate email");
            ModelState.AddModelError(nameof(model.Email), "כתובת הדוא\"ל כבר קיימת במערכת.");
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating person");
            ModelState.AddModelError("Create", "אירעה שגיאה בזמן הוספת האדם. נסו שוב.");
            return View(model);
        }
    }

    /// <summary>Exports selected people as PDF (POST from list checkboxes).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExportPdf(int[]? selectedIds, CancellationToken cancellationToken)
    {
        var validIds = selectedIds?.Where(id => id > 0).Distinct().ToArray() ?? Array.Empty<int>();
        if (validIds.Length == 0)
        {
            TempData["Error"] = "סמנו לפחות אדם אחד לייצוא ל-PDF.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var pdfContent = await _peopleService.ExportPeopleListByIdsAsync(validIds, cancellationToken);
            return File(pdfContent, "application/pdf", $"people-list-{DateTime.UtcNow:yyyyMMdd-HHmmss}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting PDF");
            TempData["Error"] = _environment.IsDevelopment()
                ? $"יצוא PDF: {ex.GetType().Name}: {ex.Message}"
                : "אירעה שגיאה בזמן יצוא הקובץ ל-PDF. נסו שוב.";
            return RedirectToAction(nameof(Index));
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private static bool IsDuplicateEmailViolation(DbUpdateException exception)
    {
        var message = exception.InnerException?.Message ?? exception.Message;

        return message.Contains("UX_People_Email", StringComparison.OrdinalIgnoreCase)
            || message.Contains("duplicate", StringComparison.OrdinalIgnoreCase);
    }

    private static string? NormalizePhotoContentType(string? fileName, string? contentType)
    {
        var extension = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();

        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => string.IsNullOrWhiteSpace(contentType) ? null : contentType.Trim()
        };
    }
}
