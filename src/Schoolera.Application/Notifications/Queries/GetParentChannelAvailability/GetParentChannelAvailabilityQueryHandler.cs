using MediatR;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Notifications.Dtos;
using Schoolera.Application.Parent.Constants;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Notifications.Queries.GetParentChannelAvailability;

public sealed record GetParentChannelAvailabilityQuery : IRequest<Result<ChannelAvailabilityDto>>;

public sealed class GetParentChannelAvailabilityQueryHandler(
    ICurrentUser currentUser,
    IPlatformIntegrationConfigurationAccessor configurationAccessor)
    : IRequestHandler<GetParentChannelAvailabilityQuery, Result<ChannelAvailabilityDto>>
{
    public async Task<Result<ChannelAvailabilityDto>> Handle(
        GetParentChannelAvailabilityQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null || !currentUser.IsInRole(SchooleraRoles.Parent))
        {
            return Result<ChannelAvailabilityDto>.Failure(
                ["Forbidden."],
                [ParentErrorCodes.Forbidden]);
        }

        var email = await configurationAccessor.GetActiveDefaultAsync(
            IntegrationType.Email,
            cancellationToken);
        var sms = await configurationAccessor.GetActiveDefaultAsync(
            IntegrationType.Sms,
            cancellationToken);
        var whatsApp = await configurationAccessor.GetActiveDefaultAsync(
            IntegrationType.WhatsApp,
            cancellationToken);

        return Result<ChannelAvailabilityDto>.Success(
            new ChannelAvailabilityDto(
                InAppAvailable: true,
                EmailConfigured: email?.IsConfigured == true,
                SmsConfigured: sms?.IsConfigured == true,
                WhatsAppConfigured: whatsApp?.IsConfigured == true));
    }
}
