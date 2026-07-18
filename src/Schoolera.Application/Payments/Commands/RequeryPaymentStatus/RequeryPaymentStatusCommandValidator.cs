using FluentValidation;

namespace Schoolera.Application.Payments.Commands.RequeryPaymentStatus;

public sealed class RequeryPaymentStatusCommandValidator : AbstractValidator<RequeryPaymentStatusCommand>
{
    public RequeryPaymentStatusCommandValidator()
    {
    }
}
