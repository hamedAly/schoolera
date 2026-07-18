namespace Schoolera.Application.Common.Interfaces;

public interface IContentSanitizer
{
    string SanitizeHtml(string? html);

    string StripToPlainText(string? html);
}
