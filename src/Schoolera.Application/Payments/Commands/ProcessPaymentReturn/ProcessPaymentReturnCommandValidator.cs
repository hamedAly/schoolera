using FluentValidation;

namespace Schoolera.Application.Payments.Commands.ProcessPaymentReturn;

public sealed class ProcessPaymentReturnCommandValidator : AbstractValidator<ProcessPaymentReturnCommand>
{
    public ProcessPaymentReturnCommandValidator()
    {
    }
}
