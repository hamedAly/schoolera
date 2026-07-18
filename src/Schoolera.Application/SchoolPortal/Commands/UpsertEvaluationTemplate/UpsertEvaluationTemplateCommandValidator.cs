using FluentValidation;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SchoolPortal.Commands.UpsertEvaluationTemplate;

public sealed class UpsertEvaluationTemplateCommandValidator
    : AbstractValidator<UpsertEvaluationTemplateCommand>
{
    public UpsertEvaluationTemplateCommandValidator()
    {
        RuleFor(x => x.SchoolId).NotEmpty();
        RuleFor(x => x.Body.NameAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Body.NameEn).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Body.NameAr).Must(IsPlainText);
        RuleFor(x => x.Body.NameEn).Must(IsPlainText);
        RuleFor(x => x.Body.Kind).IsInEnum();
        RuleFor(x => x.Body.IdempotencyKey).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Body.Criteria).NotNull().Must(x => x.Count is > 0 and <= 100);
        RuleForEach(x => x.Body.Criteria).ChildRules(criterion =>
        {
            criterion.RuleFor(x => x.Type).IsInEnum();
            criterion.RuleFor(x => x.LabelAr).NotEmpty().MaximumLength(300);
            criterion.RuleFor(x => x.LabelEn).NotEmpty().MaximumLength(300);
            criterion.RuleFor(x => x.HelpTextAr).MaximumLength(1000);
            criterion.RuleFor(x => x.HelpTextEn).MaximumLength(1000);
            criterion.RuleFor(x => x.LabelAr).Must(IsPlainText);
            criterion.RuleFor(x => x.LabelEn).Must(IsPlainText);
            criterion.RuleFor(x => x.HelpTextAr).Must(IsPlainText);
            criterion.RuleFor(x => x.HelpTextEn).Must(IsPlainText);
            criterion.RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
            criterion.RuleFor(x => x.ShortTextMaxLength)
                .InclusiveBetween(1, 2000)
                .When(x => x.Type == EvaluationCriterionType.ShortText);
            criterion.RuleFor(x => x.Options)
                .Must(x => x.Count is >= 2 and <= 20)
                .When(x => x.Type == EvaluationCriterionType.SingleChoice);
            criterion.RuleForEach(x => x.Options).ChildRules(option =>
            {
                option.RuleFor(x => x.Value).NotEmpty().MaximumLength(100).Must(IsPlainText);
                option.RuleFor(x => x.LabelAr).NotEmpty().MaximumLength(200).Must(IsPlainText);
                option.RuleFor(x => x.LabelEn).NotEmpty().MaximumLength(200).Must(IsPlainText);
            });
        });
    }

    private static bool IsPlainText(string? value) =>
        string.IsNullOrEmpty(value) || (!value.Contains('<') && !value.Contains('>'));
}
