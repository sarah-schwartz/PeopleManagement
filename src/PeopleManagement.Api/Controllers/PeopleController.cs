using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using PeopleManagement.Application.People;
using PeopleManagement.Domain.People;

namespace PeopleManagement.Api.Controllers;

/// <summary>
/// Manages people operations: creation, retrieval, searching, and PDF exports.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class PeopleController : ControllerBase
{
    private readonly IPeopleService _peopleService;
    private readonly ILogger<PeopleController> _logger;

    public PeopleController(IPeopleService peopleService, ILogger<PeopleController> logger)
    {
        _peopleService = peopleService ?? throw new ArgumentNullException(nameof(peopleService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new person. Accepts multipart/form-data so the profile photo can be
    /// uploaded as a real file field rather than a Base64-encoded JSON property.
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CreatePersonResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAsync([FromForm] CreatePersonFormInput form, CancellationToken cancellationToken)
    {
        byte[]? photoContent = null;
        string? photoFileName = null;
        string? photoContentType = null;

        if (form.Photo is { Length: > 0 })
        {
            using var ms = new MemoryStream();
            await form.Photo.CopyToAsync(ms, cancellationToken);
            photoContent = ms.ToArray();
            photoFileName = form.Photo.FileName;
            photoContentType = form.Photo.ContentType;
        }

        var input = new CreatePersonInput
        {
            FirstName = form.FirstName,
            LastName = form.LastName,
            Email = form.Email,
            Phone = form.Phone,
            Status = form.Status,
            PhotoContent = photoContent,
            PhotoFileName = photoFileName,
            PhotoContentType = photoContentType
        };

        try
        {
            var personId = await _peopleService.CreateAsync(input, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = personId }, new CreatePersonResponse(personId));
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning("Validation error during person creation: {Errors}", ex.Message);
            var errors = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return BadRequest(new ValidationErrorResponse(errors));
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<Person>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync([FromQuery] PersonStatus? status, CancellationToken cancellationToken)
    {
        var people = await _peopleService.ListAsync(status, cancellationToken);
        return Ok(people);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Person), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var person = await _peopleService.GetByIdAsync(id, cancellationToken);
        if (person == null)
            return NotFound();

        return Ok(person);
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(IReadOnlyList<Person>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchAsync([FromQuery] string? query, CancellationToken cancellationToken)
    {
        var results = await _peopleService.SearchAsync(query ?? string.Empty, cancellationToken);
        return Ok(results);
    }

    [HttpGet("export/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportPdfAsync(CancellationToken cancellationToken)
    {
        var pdfContent = await _peopleService.ExportPeopleListAsync(cancellationToken);
        return File(pdfContent, "application/pdf", $"people-list-{DateTime.UtcNow:yyyyMMdd-HHmmss}.pdf");
    }
}

/// <summary>Form model for POST /api/People (multipart/form-data).</summary>
public sealed class CreatePersonFormInput
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public PersonStatus Status { get; init; } = PersonStatus.Active;
    public IFormFile? Photo { get; init; }
}

public sealed record CreatePersonResponse(int Id);

public sealed record ValidationErrorResponse(Dictionary<string, string[]> Errors);
