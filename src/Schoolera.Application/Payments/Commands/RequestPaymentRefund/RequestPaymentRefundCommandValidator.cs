using FluentValidation;

namespace Schoolera.Application.Payments.Commands.RequestPaymentRefund;

public sealed class RequestPaymentRefundCommandValidator : AbstractValidator<RequestPaymentRefundCommand>
{
    public RequestPaymentRefundCommandValidator()
    {
    }
}
