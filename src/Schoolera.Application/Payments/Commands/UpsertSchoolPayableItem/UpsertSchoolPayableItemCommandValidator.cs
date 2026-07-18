using FluentValidation;

namespace Schoolera.Application.Payments.Commands.UpsertSchoolPayableItem;

public sealed class UpsertSchoolPayableItemCommandValidator : AbstractValidator<UpsertSchoolPayableItemCommand>
{
    public UpsertSchoolPayableItemCommandValidator()
    {
    }
}
