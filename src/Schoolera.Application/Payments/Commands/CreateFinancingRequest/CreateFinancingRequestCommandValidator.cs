using FluentValidation;

namespace Schoolera.Application.Payments.Commands.CreateFinancingRequest;

public sealed class CreateFinancingRequestCommandValidator : AbstractValidator<CreateFinancingRequestCommand>
{
    public CreateFinancingRequestCommandValidator()
    {
    }
}
