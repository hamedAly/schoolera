using Schoolera.Domain.Enums;

namespace Schoolera.Application.SupportTickets.Common;

/// <summary>Centralized support ticket status transition rules for Phase 1.</summary>
public static class SupportTicketTransitionPolicy
{
    public static bool CanTransition(SupportTicketStatus from, SupportTicketStatus to)
    {
        if (from == to)
        {
            return false;
        }

        return (from, to) switch
        {
            (SupportTicketStatus.Open, SupportTicketStatus.InProgress) => true,
            (SupportTicketStatus.Open, SupportTicketStatus.WaitingForCustomer) => true,
            (SupportTicketStatus.Open, SupportTicketStatus.Resolved) => true,
            (SupportTicketStatus.Open, SupportTicketStatus.Closed) => true,

            (SupportTicketStatus.InProgress, SupportTicketStatus.WaitingForCustomer) => true,
            (SupportTicketStatus.InProgress, SupportTicketStatus.Resolved) => true,
            (SupportTicketStatus.InProgress, SupportTicketStatus.Closed) => true,
            (SupportTicketStatus.InProgress, SupportTicketStatus.Open) => true,

            (SupportTicketStatus.WaitingForCustomer, SupportTicketStatus.InProgress) => true,
            (SupportTicketStatus.WaitingForCustomer, SupportTicketStatus.Open) => true,
            (SupportTicketStatus.WaitingForCustomer, SupportTicketStatus.Resolved) => true,
            (SupportTicketStatus.WaitingForCustomer, SupportTicketStatus.Closed) => true,

            (SupportTicketStatus.Resolved, SupportTicketStatus.Open) => true,
            (SupportTicketStatus.Resolved, SupportTicketStatus.InProgress) => true,
            (SupportTicketStatus.Resolved, SupportTicketStatus.Closed) => true,

            (SupportTicketStatus.Closed, _) => false,
            _ => false,
        };
    }

    public static bool CanParentReopen(
        SupportTicketStatus status,
        DateTimeOffset? resolvedAtUtc,
        DateTimeOffset utcNow,
        int reopenWindowHours)
    {
        if (status != SupportTicketStatus.Resolved || resolvedAtUtc is null)
        {
            return false;
        }

        if (reopenWindowHours < 0)
        {
            return false;
        }

        return utcNow - resolvedAtUtc.Value <= TimeSpan.FromHours(reopenWindowHours);
    }

    public static bool IsFirstResponseOverdue(
        DateTimeOffset? firstResponseAtUtc,
        DateTimeOffset firstResponseDueAtUtc,
        DateTimeOffset utcNow) =>
        firstResponseAtUtc is null && utcNow > firstResponseDueAtUtc;

    /// <summary>
    /// Resolved/Closed tickets are never resolution-overdue.
    /// WaitingForCustomer does not pause the SLA clock in Phase 1.
    /// </summary>
    public static bool IsResolutionOverdue(
        SupportTicketStatus status,
        DateTimeOffset resolutionDueAtUtc,
        DateTimeOffset utcNow) =>
        status is not (SupportTicketStatus.Resolved or SupportTicketStatus.Closed) &&
        utcNow > resolutionDueAtUtc;
}
