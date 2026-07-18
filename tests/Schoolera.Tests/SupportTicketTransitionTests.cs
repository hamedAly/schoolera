using Schoolera.Application.SupportTickets.Common;
using Schoolera.Application.SupportTickets.Options;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Tests;

public sealed class SupportTicketTransitionTests
{
    [Fact]
    public void CanParentReopen_ShouldAllowWithinConfiguredWindow()
    {
        var resolvedAt = DateTimeOffset.UtcNow.AddHours(-24);
        var ok = SupportTicketTransitionPolicy.CanParentReopen(
            SupportTicketStatus.Resolved,
            resolvedAt,
            DateTimeOffset.UtcNow,
            reopenWindowHours: 72);

        Assert.True(ok);
    }

    [Fact]
    public void CanParentReopen_ShouldRejectOutsideWindowAndClosed()
    {
        var resolvedAt = DateTimeOffset.UtcNow.AddHours(-100);
        Assert.False(SupportTicketTransitionPolicy.CanParentReopen(
            SupportTicketStatus.Resolved,
            resolvedAt,
            DateTimeOffset.UtcNow,
            reopenWindowHours: 72));

        Assert.False(SupportTicketTransitionPolicy.CanParentReopen(
            SupportTicketStatus.Closed,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            reopenWindowHours: 72));
    }

    [Fact]
    public void IsFirstResponseOverdue_ShouldOnlyApplyWhenNoResponseYet()
    {
        var due = DateTimeOffset.UtcNow.AddHours(-1);
        Assert.True(SupportTicketTransitionPolicy.IsFirstResponseOverdue(null, due, DateTimeOffset.UtcNow));
        Assert.False(SupportTicketTransitionPolicy.IsFirstResponseOverdue(
            DateTimeOffset.UtcNow.AddMinutes(-10),
            due,
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void IsResolutionOverdue_ShouldIgnoreResolvedAndClosed_AndNotPauseOnWaiting()
    {
        var due = DateTimeOffset.UtcNow.AddHours(-2);
        Assert.True(SupportTicketTransitionPolicy.IsResolutionOverdue(
            SupportTicketStatus.WaitingForCustomer,
            due,
            DateTimeOffset.UtcNow));
        Assert.False(SupportTicketTransitionPolicy.IsResolutionOverdue(
            SupportTicketStatus.Resolved,
            due,
            DateTimeOffset.UtcNow));
        Assert.False(SupportTicketTransitionPolicy.IsResolutionOverdue(
            SupportTicketStatus.Closed,
            due,
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void InternalSupportNotes_ShouldBeIsolatedFromParentVisibleHistory()
    {
        var ticket = CreateOpenTicket();
        ticket.AddHistory(
            SupportTicketHistoryAction.MessageAdded,
            Guid.NewGuid(),
            fromValue: null,
            toValue: nameof(SupportTicketMessageVisibility.InternalSupportNote),
            summary: "Internal note");
        ticket.AddHistory(
            SupportTicketHistoryAction.MessageAdded,
            Guid.NewGuid(),
            fromValue: null,
            toValue: nameof(SupportTicketMessageVisibility.CustomerVisible),
            summary: "Customer reply");

        var parentVisible = ticket.History.Where(SupportTicketMapping.IsParentVisibleHistory).ToArray();
        Assert.Single(parentVisible);
        Assert.Equal(
            nameof(SupportTicketMessageVisibility.CustomerVisible),
            parentVisible[0].ToValue);
    }

    [Fact]
    public void AddMessage_InternalNote_ShouldNotBeCustomerVisible()
    {
        var ticket = CreateOpenTicket();
        var agentId = Guid.NewGuid();
        ticket.AddMessage(
            agentId,
            SupportTicketAuthorType.SupportAgent,
            SupportTicketMessageVisibility.InternalSupportNote,
            "Internal only");
        ticket.AddMessage(
            agentId,
            SupportTicketAuthorType.SupportAgent,
            SupportTicketMessageVisibility.CustomerVisible,
            "Customer visible reply");

        var parentDetail = SupportTicketMapping.ToParentDetail(ticket);
        Assert.Single(parentDetail.Messages);
        Assert.Equal((int)SupportTicketMessageVisibility.CustomerVisible, parentDetail.Messages.First().Visibility);
        Assert.DoesNotContain(
            parentDetail.Messages,
            message => message.Visibility == (int)SupportTicketMessageVisibility.InternalSupportNote);
    }

    [Fact]
    public void StatusTransitions_ShouldBeIdempotentOnlyWhenDifferent()
    {
        Assert.False(SupportTicketTransitionPolicy.CanTransition(
            SupportTicketStatus.InProgress,
            SupportTicketStatus.InProgress));
        Assert.True(SupportTicketTransitionPolicy.CanTransition(
            SupportTicketStatus.InProgress,
            SupportTicketStatus.Resolved));
    }

    [Fact]
    public void SlaOptions_ShouldComputeDueDatesFromPriority()
    {
        var options = new SupportTicketSlaOptions
        {
            FirstResponseHoursUrgent = 2,
            ResolutionHoursUrgent = 24,
        };
        var created = new DateTimeOffset(2026, 7, 17, 10, 0, 0, TimeSpan.Zero);
        var (first, resolution) = options.ComputeDueDates((int)SupportTicketPriority.Urgent, created);

        Assert.Equal(created.AddHours(2), first);
        Assert.Equal(created.AddHours(24), resolution);
    }

    [Fact]
    public void CsvExporter_ShouldFormulaEscapeLeadingEquals()
    {
        Assert.Equal("'=cmd", SupportTicketCsvExporter.Escape("=cmd"));
        Assert.Equal("safe", SupportTicketCsvExporter.Escape("safe"));
    }

    private static SupportTicket CreateOpenTicket()
    {
        var now = DateTimeOffset.UtcNow;
        return new SupportTicket(
            "ST-20260717-00001",
            Guid.NewGuid(),
            SupportTicketCategory.GeneralSupport,
            SupportTicketPriority.Normal,
            "Test subject",
            admissionApplicationId: null,
            sourceContactRequestId: null,
            firstResponseDueAtUtc: now.AddHours(24),
            resolutionDueAtUtc: now.AddHours(120));
    }
}
