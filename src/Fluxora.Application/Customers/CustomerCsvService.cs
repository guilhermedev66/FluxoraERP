using System.Net.Mail;
using System.Text;
using System.Text.Json;
using Fluxora.Application.Common;
using Fluxora.Domain.Customers;
using Microsoft.VisualBasic.FileIO;

namespace Fluxora.Application.Customers;

public sealed record CsvImportError(long Line, string Reason);

public sealed record CustomerCsvImportResult(
    int Total,
    int Imported,
    int Rejected,
    IReadOnlyList<CsvImportError> Errors);

public sealed record CustomerCsvExport(byte[] Content, string FileName, string ContentType);

public class CustomerCsvService(
    ICustomerRepository repository,
    IUnitOfWork unitOfWork,
    IAuditWriter auditWriter,
    ICurrentUser currentUser)
{
    private static readonly string[] ExpectedHeaders = ["name", "document", "email", "phone"];

    public async Task<CustomerCsvImportResult> ImportAsync(
        Stream stream, CancellationToken cancellationToken = default)
    {
        var importId = Guid.NewGuid();
        var parsedRows = Parse(stream, out var parseErrors);
        var existingDocuments = await repository.GetExistingDocumentsAsync(
            parsedRows.Select(row => row.Document), cancellationToken);
        var seenDocuments = new HashSet<string>(StringComparer.Ordinal);
        var errors = new List<CsvImportError>(parseErrors);
        var imported = 0;

        foreach (var row in parsedRows)
        {
            if (!seenDocuments.Add(row.Document))
            {
                errors.Add(new CsvImportError(row.Line, $"Duplicate document '{row.Document}' in this file."));
                continue;
            }

            if (existingDocuments.Contains(row.Document))
            {
                errors.Add(new CsvImportError(row.Line, $"A customer with document '{row.Document}' already exists."));
                continue;
            }

            var customer = Customer.Create(row.Name, row.Document, row.Email, row.Phone);
            repository.Add(customer);
            auditWriter.Record(
                "CustomerImported",
                nameof(Customer),
                customer.Id,
                afterValues: JsonSerializer.Serialize(new { customer.Name, customer.Document, ImportId = importId }),
                actorId: currentUser.UserId,
                correlationId: importId);
            imported++;
        }

        var total = parsedRows.Count + parseErrors.Count;
        auditWriter.Record(
            "CustomerCsvImportCompleted",
            "CustomerImport",
            importId,
            afterValues: JsonSerializer.Serialize(new { Total = total, Imported = imported, Rejected = errors.Count }),
            actorId: currentUser.UserId,
            correlationId: importId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new CustomerCsvImportResult(total, imported, errors.Count, errors.OrderBy(error => error.Line).ToList());
    }

    public async Task<CustomerCsvExport> ExportAsync(
        string? search, bool? isActive, CancellationToken cancellationToken = default)
    {
        var customers = await repository.ListForExportAsync(search, isActive, cancellationToken);
        var csv = new StringBuilder();
        csv.AppendLine("name,document,email,phone,is_active,created_at_utc");

        foreach (var customer in customers)
        {
            csv.Append(Escape(customer.Name)).Append(',')
                .Append(Escape(customer.Document)).Append(',')
                .Append(Escape(customer.Email)).Append(',')
                .Append(Escape(customer.Phone)).Append(',')
                .Append(customer.IsActive ? "true" : "false").Append(',')
                .Append(customer.CreatedAtUtc.ToString("O"))
                .AppendLine();
        }

        var content = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(csv.ToString());
        return new CustomerCsvExport(
            content,
            $"fluxora-customers-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv",
            "text/csv; charset=utf-8");
    }

    private static List<ParsedCustomerRow> Parse(Stream stream, out List<CsvImportError> errors)
    {
        errors = [];
        var rows = new List<ParsedCustomerRow>();

        try
        {
            using var parser = new TextFieldParser(
                stream,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true),
                detectEncoding: false,
                leaveOpen: true)
            {
                TextFieldType = FieldType.Delimited,
                HasFieldsEnclosedInQuotes = true,
                TrimWhiteSpace = true,
            };
            parser.SetDelimiters(",");

            string[]? headers;
            try
            {
                headers = parser.ReadFields();
            }
            catch (MalformedLineException)
            {
                throw new InvalidCsvException("The CSV header row is malformed.");
            }
            if (headers is null)
            {
                throw new InvalidCsvException("The CSV file is empty.");
            }

            var normalizedHeaders = headers
                .Select((header, index) => index == 0 ? header.TrimStart('\uFEFF').ToLowerInvariant() : header.ToLowerInvariant())
                .ToArray();
            if (!normalizedHeaders.SequenceEqual(ExpectedHeaders))
            {
                throw new InvalidCsvException(
                    $"Invalid CSV headers. Expected exactly: {string.Join(',', ExpectedHeaders)}.");
            }

            while (!parser.EndOfData)
            {
                var line = parser.LineNumber;
                string[]? fields;
                try
                {
                    fields = parser.ReadFields();
                }
                catch (MalformedLineException exception)
                {
                    errors.Add(new CsvImportError(exception.LineNumber, "Malformed CSV row."));
                    continue;
                }

                if (fields is null)
                {
                    continue;
                }

                var validationError = Validate(fields);
                if (validationError is not null)
                {
                    errors.Add(new CsvImportError(line, validationError));
                    continue;
                }

                rows.Add(new ParsedCustomerRow(
                    line,
                    fields[0].Trim(),
                    fields[1].Trim(),
                    NullIfWhiteSpace(fields[2]),
                    NullIfWhiteSpace(fields[3])));
            }
        }
        catch (DecoderFallbackException)
        {
            throw new InvalidCsvException("The CSV file must use valid UTF-8 encoding.");
        }

        return rows;
    }

    private static string? Validate(string[] fields)
    {
        if (fields.Length != ExpectedHeaders.Length)
        {
            return $"Expected {ExpectedHeaders.Length} columns, but found {fields.Length}.";
        }

        var name = fields[0].Trim();
        var document = fields[1].Trim();
        var email = NullIfWhiteSpace(fields[2]);
        var phone = NullIfWhiteSpace(fields[3]);

        if (name.Length == 0) return "Name is required.";
        if (name.Length > 200) return "Name cannot exceed 200 characters.";
        if (document.Length == 0) return "Document is required.";
        if (document.Length > 20) return "Document cannot exceed 20 characters.";
        if (email?.Length > 200) return "Email cannot exceed 200 characters.";
        if (email is not null && !MailAddress.TryCreate(email, out _)) return "Email format is invalid.";
        if (phone?.Length > 30) return "Phone cannot exceed 30 characters.";
        return null;
    }

    private static string? NullIfWhiteSpace(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.IndexOfAny([',', '"', '\r', '\n']) >= 0
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }

    private sealed record ParsedCustomerRow(long Line, string Name, string Document, string? Email, string? Phone);
}
