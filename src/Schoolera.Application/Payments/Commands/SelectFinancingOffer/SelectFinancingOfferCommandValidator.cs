using FluentValidation;

namespace Schoolera.Application.Payments.Commands.SelectFinancingOffer;

public sealed class SelectFinancingOfferCommandValidator : AbstractValidator<SelectFinancingOfferCommand>
{
    public SelectFinancingOfferCommandValidator()
    {
    }
}
