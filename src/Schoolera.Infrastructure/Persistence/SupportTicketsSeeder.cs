using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.SupportTickets.Options;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;
using Schoolera.Infrastructure.Identity;

namespace Schoolera.Infrastructure.Persistence;

/// <summary>Idempotent demo support tickets for seeded Parent / SupportAgent accounts.</summary>
public sealed class SupportTicketsSeeder(
    SchooleraDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    ISupportTicketReferenceGenerator referenceGenerator,
    IOptions<SupportTicketSlaOptions> slaOptions,
    ILogger<SupportTicketsSeeder> logger)
{
    private const string ParentEmail = "parent@schoolera.local";
    private const string SupportEmail = "support@schoolera.local";
    private const string OpenSeedMarker = "SEED-OPEN-GENERAL";
    private const string WaitingSeedMarker = "SEED-WAITING-ACCOUNT";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Running support tickets seed...");

        var parent = await userManager.FindByEmailAsync(ParentEmail);
        if (parent is null)
        {
            logger.LogInformation("Support tickets seed skipped; parent {Email} not found.", ParentEmail);
            return;
        }

        var support = await userManager.FindByEmailAsync(SupportEmail);
        var createdAt = DateTimeOffset.UtcNow;
        var sla = slaOptions.Value;

        await EnsureTicketAsync(
            OpenSeedMarker,
            parent.Id,
            support?.Id,
            SupportTicketCategory.GeneralSupport,
            SupportTicketPriority.Normal,
            "Demo open support ticket",
            "This is a fictional seeded Open ticket for local development.",
            SupportTicketStatus.Open,
            assign: false,
            createdAt,
            sla,
            cancellationToken);

        await EnsureTicketAsync(
            WaitingSeedMarker,
            parent.Id,
            support?.Id,
            SupportTicketCategory.Account,
            SupportTicketPriority.High,
            "Demo waiting-for-customer ticket",
            "This is a fictional seeded WaitingForCustomer ticket for local development.",
            SupportTicketStatus.WaitingForCustomer,
            assign: true,
            createdAt,
            sla,
            cancellationToken);

        logger.LogInformation("Support tickets seed completed.");
    }

    private async Task EnsureTicketAsync(
        string marker,
        Guid parentUserId,
        Guid? supportUserId,
        SupportTicketCategory category,
        SupportTicketPriority priority,
        string subject,
        string body,
        SupportTicketStatus targetStatus,
        bool assign,
        DateTimeOffset createdAt,
        SupportTicketSlaOptions sla,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.SupportTickets.AnyAsync(
            ticket => ticket.ParentUserId == parentUserId && ticket.Subject == subject,
            cancellationToken);
        if (exists)
        {
            return;
        }

        var (firstDue, resolutionDue) = sla.ComputeDueDates((int)priority, createdAt);
        var reference = await referenceGenerator.GenerateAsync(cancellationToken);
        var ticket = new SupportTicket(
            reference,
            parentUserId,
            category,
            priority,
            subject,
            admissionApplicationId: null,
            sourceContactRequestId: null,
            firstDue,
            resolutionDue);

        ticket.AddMessage(
            parentUserId,
            SupportTicketAuthorType.Parent,
            SupportTicketMessageVisibility.CustomerVisible,
            $"{body} [{marker}]");
        ticket.AddHistory(
            SupportTicketHistoryAction.Created,
            parentUserId,
            fromValue: null,
            toValue: SupportTicketStatus.Open.ToString(),
            summary: "Seeded ticket created");

        if (assign && supportUserId is { } agentId)
        {
            ticket.Assign(agentId);
            ticket.AddHistory(
                SupportTicketHistoryAction.Assigned,
                agentId,
                fromValue: null,
                toValue: agentId.ToString("D"),
                summary: "Seeded assignment");
        }

        if (targetStatus == SupportTicketStatus.WaitingForCustomer && supportUserId is { } responderId)
        {
            ticket.AddMessage(
                responderId,
                SupportTicketAuthorType.SupportAgent,
                SupportTicketMessageVisibility.CustomerVisible,
                "Seeded support reply requesting more information.");
            ticket.RecordFirstResponse(createdAt);
            ticket.ChangeStatus(SupportTicketStatus.WaitingForCustomer, createdAt);
            ticket.AddHistory(
                SupportTicketHistoryAction.StatusChanged,
                responderId,
                SupportTicketStatus.Open.ToString(),
                SupportTicketStatus.WaitingForCustomer.ToString(),
                "Seeded status change");
        }

        dbContext.SupportTickets.Add(ticket);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
