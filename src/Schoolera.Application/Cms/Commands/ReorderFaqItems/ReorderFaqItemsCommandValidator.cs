using FluentValidation;

namespace Schoolera.Application.Cms.Commands.ReorderFaqItems;

public sealed class ReorderFaqItemsCommandValidator : AbstractValidator<ReorderFaqItemsCommand>
{
    public ReorderFaqItemsCommandValidator()
    {
        RuleFor(command => command.CategoryId).NotEmpty();
        RuleFor(command => command.OrderedIds).NotNull().NotEmpty();
        RuleFor(command => command.OrderedIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Duplicate item IDs are not allowed.");
        RuleForEach(command => command.OrderedIds).NotEmpty();
    }
}
