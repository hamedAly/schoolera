using System.Globalization;
using System.Text;
using Schoolera.Application.Admissions.Common;
using Schoolera.Application.Admissions.Dtos;

namespace Schoolera.Application.Admissions.Common;

/// <summary>Builds UTF-8 CSV exports for Platform Admin admission monitoring.</summary>
public static class AdminAdmissionCsvExporter
{
    /// <summary>Hard cap on exported rows (documented constant).</summary>
    public const int MaxExportRows = 5000;

    /// <summary>Hard cap on dynamic answer columns in CSV exports.</summary>
    public const int MaxAnswerColumns = 30;

    private static readonly string[] BaseHeaders =
    [
        "ApplicationNumber",
        "Status",
        "SchoolName",
        "CityName",
        "BranchName",
        "StudentName",
        "ParentName",
        "GradeName",
        "AcademicYearName",
        "SubmittedAtUtc",
        "ReviewStartedAtUtc",
        "DecisionAtUtc",
    ];

    public static (byte[] Content, string FileName) Build(
        IReadOnlyList<AdminAdmissionExportRowDto> rows,
        bool includeAnswers = false,
        IReadOnlyList<string>? answerColumns = null,
        DateTimeOffset? nowUtc = null)
    {
        var stamp = (nowUtc ?? DateTimeOffset.UtcNow).UtcDateTime.ToString(
            "yyyyMMddHHmmss",
            CultureInfo.InvariantCulture);
        var fileName = $"admission-applications-{stamp}.csv";

        var columns = includeAnswers
            ? (answerColumns ?? [])
                .Take(MaxAnswerColumns)
                .Select(code => $"Answer:{code}")
                .ToArray()
            : [];
        var headers = BaseHeaders.Concat(columns).ToArray();

        var builder = new StringBuilder();
        builder.Append(string.Join(',', headers));
        builder.Append("\r\n");

        foreach (var row in rows)
        {
            builder.Append(Escape(row.ApplicationNumber));
            builder.Append(',');
            builder.Append(Escape(row.Status));
            builder.Append(',');
            builder.Append(Escape(row.SchoolName));
            builder.Append(',');
            builder.Append(Escape(row.CityName));
            builder.Append(',');
            builder.Append(Escape(row.BranchName));
            builder.Append(',');
            builder.Append(Escape(row.StudentName));
            builder.Append(',');
            builder.Append(Escape(row.ParentName));
            builder.Append(',');
            builder.Append(Escape(row.GradeName));
            builder.Append(',');
            builder.Append(Escape(row.AcademicYearName));
            builder.Append(',');
            builder.Append(Escape(row.SubmittedAtUtc));
            builder.Append(',');
            builder.Append(Escape(row.ReviewStartedAtUtc));
            builder.Append(',');
            builder.Append(Escape(row.DecisionAtUtc));

            if (includeAnswers)
            {
                var values = row.AnswerValues;
                for (var index = 0; index < columns.Length; index++)
                {
                    builder.Append(',');
                    builder.Append(Escape(index < values.Count ? values[index] : string.Empty));
                }
            }

            builder.Append("\r\n");
        }

        var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        var body = utf8.GetBytes(builder.ToString());
        var bom = Encoding.UTF8.GetPreamble();
        var content = new byte[bom.Length + body.Length];
        Buffer.BlockCopy(bom, 0, content, 0, bom.Length);
        Buffer.BlockCopy(body, 0, content, bom.Length, body.Length);
        return (content, fileName);
    }

    /// <summary>
    /// Escapes CSV cells and mitigates formula injection by prefixing
    /// leading <c>=</c>, <c>+</c>, <c>-</c>, <c>@</c>, tab, or CR with a single quote.
    /// </summary>
    public static string Escape(string? value)
    {
        var text = value ?? string.Empty;
        if (text.Length > 0 && text[0] is '=' or '+' or '-' or '@' or '\t' or '\r')
        {
            text = "'" + text;
        }

        if (text.Contains('"') || text.Contains(',') || text.Contains('\n') || text.Contains('\r'))
        {
            return "\"" + text.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
        }

        return text;
    }
}
