using FluentValidation;

namespace Schoolera.Application.Cms.Commands.ReorderFaqCategories;

public sealed class ReorderFaqCategoriesCommandValidator : AbstractValidator<ReorderFaqCategoriesCommand>
{
    public ReorderFaqCategoriesCommandValidator()
    {
        RuleFor(command => command.OrderedIds).NotNull().NotEmpty();
        RuleFor(command => command.OrderedIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Duplicate category IDs are not allowed.");
        RuleForEach(command => command.OrderedIds).NotEmpty();
    }
}
