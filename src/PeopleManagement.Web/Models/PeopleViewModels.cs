using System.ComponentModel.DataAnnotations;
using PeopleManagement.Domain.People;

namespace PeopleManagement.Web.Models;


/// <summary>View model for people index page.</summary>
public sealed class PeopleIndexViewModel
{
    public IReadOnlyList<Person> People { get; set; } = new List<Person>();

    public string? SearchQuery { get; set; }

    public PersonStatus? StatusFilter { get; set; }
}

/// <summary>View model for creating a person.</summary>
public sealed class CreatePersonViewModel
{
    [Display(Name = "שם פרטי")]
    [Required(ErrorMessage = "יש להזין שם פרטי.")]
    [StringLength(100, ErrorMessage = "אורך השם הפרטי יכול להיות עד 100 תווים.")]
    public string FirstName { get; set; } = string.Empty;

    [Display(Name = "שם משפחה")]
    [StringLength(100, ErrorMessage = "אורך שם המשפחה יכול להיות עד 100 תווים.")]
    public string? LastName { get; set; }

    [Display(Name = "דוא\"ל")]
    [Required(ErrorMessage = "יש להזין כתובת דוא\"ל.")]
    [EmailAddress(ErrorMessage = "כתובת הדוא\"ל אינה תקינה.")]
    [StringLength(320, ErrorMessage = "כתובת הדוא\"ל יכולה להכיל עד 320 תווים.")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "טלפון")]
    [StringLength(20, ErrorMessage = "מספר הטלפון יכול להכיל עד 20 תווים.")]
    public string? Phone { get; set; }

    [Display(Name = "סטטוס")]
    public PersonStatus Status { get; set; } = PersonStatus.Active;
}
