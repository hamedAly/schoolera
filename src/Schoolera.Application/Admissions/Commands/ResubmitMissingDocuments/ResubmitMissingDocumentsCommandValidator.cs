using FluentValidation;

namespace Schoolera.Application.Admissions.Commands.ResubmitMissingDocuments;

public sealed class ResubmitMissingDocumentsCommandValidator
    : AbstractValidator<ResubmitMissingDocumentsCommand>
{
    public ResubmitMissingDocumentsCommandValidator()
    {
        RuleFor(command => command.ApplicationId).NotEmpty();
        RuleFor(command => command.Body).NotNull();
    }
}
