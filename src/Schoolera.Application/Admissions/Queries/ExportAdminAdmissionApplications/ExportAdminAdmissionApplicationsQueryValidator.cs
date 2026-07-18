using FluentValidation;

namespace Schoolera.Application.Admissions.Queries.ExportAdminAdmissionApplications;

public sealed class ExportAdminAdmissionApplicationsQueryValidator
    : AbstractValidator<ExportAdminAdmissionApplicationsQuery>
{
    private static readonly HashSet<string> AllowedSorts = new(StringComparer.OrdinalIgnoreCase)
    {
        "newest",
        "oldest",
        "submitted-newest",
        "submitted-oldest",
        "application-number",
        "status",
    };

    public ExportAdminAdmissionApplicationsQueryValidator()
    {
        RuleFor(query => query.Filters).NotNull();
        RuleFor(query => query.Filters.Status).IsInEnum().When(query => query.Filters.Status is not null);
        RuleFor(query => query.Filters.Search)
            .MaximumLength(200)
            .When(query => !string.IsNullOrWhiteSpace(query.Filters.Search));
        RuleFor(query => query.Filters.Sort)
            .Must(sort => string.IsNullOrWhiteSpace(sort) || AllowedSorts.Contains(sort.Trim()))
            .WithMessage(
                "Sort must be one of: newest, oldest, submitted-newest, submitted-oldest, application-number, status.");
    }
}
