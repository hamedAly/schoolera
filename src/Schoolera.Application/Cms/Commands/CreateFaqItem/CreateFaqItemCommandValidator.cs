using FluentValidation;
using Schoolera.Domain.Common;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Cms.Commands.CreateFaqItem;

public sealed class CreateFaqItemCommandValidator : AbstractValidator<CreateFaqItemCommand>
{
    public CreateFaqItemCommandValidator()
    {
        RuleFor(command => command.CategoryId).NotEmpty();
        RuleFor(command => command.QuestionAr).NotEmpty().MaximumLength(FieldLengthLimits.FaqQuestion);
        RuleFor(command => command.QuestionEn).NotEmpty().MaximumLength(FieldLengthLimits.FaqQuestion);
        RuleFor(command => command.AnswerAr).NotEmpty().MaximumLength(FieldLengthLimits.FaqAnswer);
        RuleFor(command => command.AnswerEn).NotEmpty().MaximumLength(FieldLengthLimits.FaqAnswer);
        RuleFor(command => command.OwnershipScope)
            .Must(scope => scope is null or FaqOwnershipScope.Platform)
            .WithMessage("Platform Admin may only create Platform-owned FAQ items.");
        RuleFor(command => command.InterviewCategory)
            .IsInEnum()
            .When(command => command.InterviewCategory.HasValue);
    }
}
