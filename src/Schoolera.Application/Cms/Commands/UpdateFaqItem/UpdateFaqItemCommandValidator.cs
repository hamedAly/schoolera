using FluentValidation;
using Schoolera.Domain.Common;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Cms.Commands.UpdateFaqItem;

public sealed class UpdateFaqItemCommandValidator : AbstractValidator<UpdateFaqItemCommand>
{
    public UpdateFaqItemCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.CategoryId).NotEmpty();
        RuleFor(command => command.QuestionAr).NotEmpty().MaximumLength(FieldLengthLimits.FaqQuestion);
        RuleFor(command => command.QuestionEn).NotEmpty().MaximumLength(FieldLengthLimits.FaqQuestion);
        RuleFor(command => command.AnswerAr).NotEmpty().MaximumLength(FieldLengthLimits.FaqAnswer);
        RuleFor(command => command.AnswerEn).NotEmpty().MaximumLength(FieldLengthLimits.FaqAnswer);
        RuleFor(command => command.InterviewCategory)
            .IsInEnum()
            .When(command => command.InterviewCategory.HasValue);
    }
}
