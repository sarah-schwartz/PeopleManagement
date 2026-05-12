using PeopleManagement.Application.Export;
using PeopleManagement.Domain.People;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace PeopleManagement.Infrastructure.Export;

/// <summary>
/// Generates PDF exports using QuestPDF with simple table layouts (text only; no embedded photos).
/// </summary>
public sealed class QuestPdfPeopleExportService : IPdfExportService
{
    private const string HebrewFontFamily = "Segoe UI";

    private const string TitleColor = "#0D47A1";
    private const string HeaderBackgroundColor = "#1E88E5";
    private const string WhiteColor = "#FFFFFF";
    private const string BorderColor = "#E0E0E0";
    private const string LabelBackgroundColor = "#F5F5F5";

    public Task<byte[]> ExportPeopleListAsync(IReadOnlyList<Person> people, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (people.Count == 0)
            return Task.FromResult(GenerateEmptyListPdf());

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.DefaultTextStyle(t => t.FontFamily(HebrewFontFamily));

                page.Content()
                    .Column(column =>
                    {
                        column.Item().Text("רשימת אנשים")
                            .FontSize(24)
                            .Bold()
                            .FontColor(TitleColor);

                        column.Item().PaddingTop(12);

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(HeaderBackgroundColor).Padding(10).Text("שם מלא").FontColor(WhiteColor).Bold();
                                header.Cell().Background(HeaderBackgroundColor).Padding(10).Text("דוא\"ל").FontColor(WhiteColor).Bold();
                                header.Cell().Background(HeaderBackgroundColor).Padding(10).Text("טלפון").FontColor(WhiteColor).Bold();
                                header.Cell().Background(HeaderBackgroundColor).Padding(10).Text("סטטוס").FontColor(WhiteColor).Bold();
                            });

                            foreach (var person in people)
                            {
                                table.Cell().Border(1).BorderColor(BorderColor).Padding(8).Text(person.FullName);
                                table.Cell().Border(1).BorderColor(BorderColor).Padding(8).Text(person.Email);
                                table.Cell().Border(1).BorderColor(BorderColor).Padding(8).Text(string.IsNullOrEmpty(person.Phone) ? "" : person.Phone);
                                table.Cell().Border(1).BorderColor(BorderColor).Padding(8).Text(
                                    person.Status == Domain.People.PersonStatus.Active ? "פעיל" : "לא פעיל");
                            }
                        });
                    });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("עמוד ");
                    text.CurrentPageNumber();
                });
            });
        });

        return Task.FromResult(document.GeneratePdf());
    }

    private static byte[] GenerateEmptyListPdf()
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(t => t.FontFamily(HebrewFontFamily));
                page.Content().Column(c =>
                {
                    c.Item().Text("רשימת אנשים").FontSize(22).Bold().FontColor(TitleColor);
                    c.Item().PaddingTop(16).Text("אין אנשים לייצוא.").FontSize(12);
                });
            });
        }).GeneratePdf();
    }

    public Task<byte[]> ExportPersonDetailsAsync(Person person, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(person);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.DefaultTextStyle(t => t.FontFamily(HebrewFontFamily));

                page.Content()
                    .Column(column =>
                    {
                        column.Item().Text("פרטי אדם")
                            .FontSize(24)
                            .Bold()
                            .FontColor(TitleColor);

                        column.Item().PaddingTop(16);

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(150f);
                                columns.RelativeColumn(1f);
                            });

                            AddRow(table, "שם מלא:", person.FullName);
                            AddRow(table, "דוא\"ל:", person.Email);
                            AddRow(table, "טלפון:", string.IsNullOrEmpty(person.Phone) ? "" : person.Phone);
                            AddRow(table, "סטטוס:", person.Status == Domain.People.PersonStatus.Active ? "פעיל" : "לא פעיל");
                        });
                    });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("הופק בתאריך ");
                    text.Span(DateTime.UtcNow.ToString("g"));
                });
            });
        });

        return Task.FromResult(document.GeneratePdf());
    }

    private static void AddRow(TableDescriptor table, string label, string value)
    {
        table.Cell().Background(LabelBackgroundColor).Padding(6).AlignRight().Text(label).Bold().FontSize(10f);
        table.Cell().Padding(6).AlignRight().Text(value).FontSize(10f);
    }
}
