using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Schoolera.Api.Controllers;
using Schoolera.Application.Admissions.Common;
using Schoolera.Application.Admissions.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;
using Schoolera.Infrastructure.Persistence;

namespace Schoolera.Tests;

[Collection(WebApplicationFactoryCollection.Name)]
public sealed class ParentAppointmentJourneyTests(SchooleraWebApplicationFactory factory)
{
    private static AdmissionInterviewAppointment Proposed(Guid? slotId = null)
    {
        var appointment = new AdmissionInterviewAppointment(
            Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(2), "UTC",
            AdmissionAppointmentMode.Online, null, "Instructions", null, null, Guid.NewGuid());
        appointment.LinkToSlot(slotId ?? Guid.NewGuid());
        return appointment;
    }

    [Fact]
    public void Proposal_confirmation_consumes_no_attempt()
    {
        var appointment = Proposed();
        appointment.ConfirmCurrent();
        Assert.Equal(AdmissionAppointmentLifecycle.Confirmed, appointment.Lifecycle);
        Assert.Equal(0, appointment.ParentRescheduleAttemptCount);
        Assert.NotNull(appointment.ConfirmedAtUtc);
    }

    [Fact]
    public void Alternate_from_active_proposal_consumes_one_attempt()
    {
        var appointment = Proposed();
        var alternate = Guid.NewGuid();
        appointment.SelectAlternate(
            DateTimeOffset.UtcNow.AddDays(3), "UTC", AdmissionAppointmentMode.Online,
            null, "Instructions", null, null, alternate);
        Assert.Equal(AdmissionAppointmentLifecycle.Confirmed, appointment.Lifecycle);
        Assert.Equal(alternate, appointment.InterviewAssessmentSlotId);
        Assert.Equal(1, appointment.ParentRescheduleAttemptCount);
    }

    [Fact]
    public void Parent_request_consumes_once_and_school_recovery_consumes_zero()
    {
        var parent = Proposed();
        parent.RequestReschedule("<b>Need another day</b>", AppointmentRescheduleInitiator.Parent);
        parent.RequestReschedule("ignored retry", AppointmentRescheduleInitiator.Parent);
        Assert.Equal(1, parent.ParentRescheduleAttemptCount);
        Assert.DoesNotContain("<", parent.LastParentVisibleRescheduleReason);

        var school = Proposed();
        school.RequestReschedule("Slot cancelled", AppointmentRescheduleInitiator.School);
        school.SelectAlternate(
            DateTimeOffset.UtcNow.AddDays(3), "UTC", AdmissionAppointmentMode.Online,
            null, "Instructions", null, null, Guid.NewGuid());
        Assert.Equal(0, school.ParentRescheduleAttemptCount);
        Assert.Equal(AdmissionAppointmentLifecycle.Confirmed, school.Lifecycle);
    }

    [Fact]
    public void Terminal_states_reject_parent_actions()
    {
        var appointment = Proposed();
        appointment.CancelByParent("No longer needed");
        Assert.Equal(AdmissionAppointmentLifecycle.Cancelled, appointment.Lifecycle);
        Assert.Throws<InvalidOperationException>(appointment.ConfirmCurrent);
        Assert.Throws<InvalidOperationException>(() =>
            appointment.RequestReschedule("retry", AppointmentRescheduleInitiator.Parent));
    }

    [Fact]
    public void Lifecycle_contract_has_no_legacy_aliases()
    {
        var names = Enum.GetNames<AdmissionAppointmentLifecycle>();
        Assert.DoesNotContain("Scheduled", names);
        Assert.DoesNotContain("RescheduleRequired", names);
        Assert.Equal(5, (int)AdmissionAppointmentLifecycle.Confirmed);
        Assert.Equal(6, (int)AdmissionAppointmentLifecycle.NoShow);
    }

    [Fact]
    public void Every_parent_appointment_write_requires_csrf()
    {
        var actions = typeof(ParentAdmissionAppointmentsController).GetMethods()
            .Where(x => x.DeclaringType == typeof(ParentAdmissionAppointmentsController) &&
                        x.GetCustomAttribute<HttpPostAttribute>() is not null)
            .ToArray();
        Assert.Equal(5, actions.Length);
        Assert.All(actions, action => Assert.Contains(
            action.GetCustomAttributes(inherit: true),
            attribute => attribute is ValidateAntiForgeryTokenAttribute));
    }

    [Fact]
    public void Parent_contract_exposes_no_provider_secret_or_url()
    {
        var names = typeof(AdmissionAppointmentDto).GetProperties()
            .Select(x => x.Name).ToArray();
        Assert.DoesNotContain(names, x =>
            x.Contains("Provider", StringComparison.OrdinalIgnoreCase) ||
            x.Contains("Url", StringComparison.OrdinalIgnoreCase) ||
            x.Contains("Token", StringComparison.OrdinalIgnoreCase) ||
            x.Contains("Credential", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("admission.appointment.joinProviderUnavailable",
            AdmissionErrorCodes.AppointmentJoinProviderUnavailable);
    }

    [Fact]
    public void Interview_capabilities_are_false_in_assessment_required_status()
    {
        var application = CreateApplication(AdmissionApplicationStatus.AssessmentRequired);
        var appointment = Proposed();

        var dto = AdmissionLifecycleMapping.ToAppointment(appointment, application)!;

        Assert.False(dto.Capabilities.CanConfirmAppointment);
        Assert.False(dto.Capabilities.CanSelectAlternateSlot);
        Assert.False(dto.Capabilities.CanRequestReschedule);
        Assert.False(dto.Capabilities.CanCancelAppointment);
        Assert.False(dto.Capabilities.CanViewAvailableSlots);
    }

    [Fact]
    public void Past_proposal_has_no_mutation_capabilities_but_preserves_online_window()
    {
        var application = CreateApplication(AdmissionApplicationStatus.InterviewRequired);
        var appointment = new AdmissionInterviewAppointment(
            application.Id, DateTimeOffset.UtcNow.AddMinutes(-1), "UTC",
            AdmissionAppointmentMode.Online, null, "Instructions", null, null, Guid.NewGuid());
        appointment.LinkToSlot(Guid.NewGuid());

        var dto = AdmissionLifecycleMapping.ToAppointment(appointment, application)!;

        Assert.False(dto.Capabilities.CanConfirmAppointment);
        Assert.False(dto.Capabilities.CanSelectAlternateSlot);
        Assert.False(dto.Capabilities.CanRequestReschedule);
        Assert.False(dto.Capabilities.CanCancelAppointment);
        Assert.False(dto.Capabilities.CanViewAvailableSlots);
        Assert.False(dto.Capabilities.CanJoinOnlineAppointment);
        Assert.NotNull(dto.Capabilities.JoinWindowStartsAtUtc);
        Assert.NotNull(dto.Capabilities.JoinWindowEndsAtUtc);
    }

    [Fact]
    public async Task Confirm_proposal_commits_history_and_outbox_without_consuming_attempt()
    {
        var fixture = await CreateSqlFixtureAsync();
        await using var scope = factory.Services.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IInterviewAssessmentSlotRepository>();

        var outcome = await repository.ExecuteParentAppointmentActionAsync(new(
            fixture.ParentUserId, fixture.ApplicationId, SlotKind.Interview,
            ParentAppointmentActionKind.Confirm, null, fixture.RowVersion,
            $"confirm-{Guid.NewGuid():N}", null));

        Assert.Equal(ParentAppointmentActionResult.Succeeded, outcome.Result);
        await using var verifyScope = factory.Services.CreateAsyncScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
        var application = await db.AdmissionApplications.AsNoTracking()
            .Include(x => x.InterviewAppointments)
            .SingleAsync(x => x.Id == fixture.ApplicationId);
        var appointment = Assert.Single(application.InterviewAppointments);
        Assert.Equal(AdmissionApplicationStatus.InterviewRequired, application.Status);
        Assert.Equal(AdmissionAppointmentLifecycle.Confirmed, appointment.Lifecycle);
        Assert.Equal(0, appointment.ParentRescheduleAttemptCount);
        Assert.Single(await db.AdmissionAppointmentActionHistory.AsNoTracking()
            .Where(x => x.AdmissionApplicationId == fixture.ApplicationId).ToListAsync());
        var outbox = await db.NotificationOutboxMessages.AsNoTracking()
            .Where(x => x.RelatedEntityId == fixture.ApplicationId).ToListAsync();
        Assert.NotEmpty(outbox);
        Assert.Equal(outbox.Count, outbox.Select(x => x.DeduplicationKey).Distinct().Count());
    }

    [Fact]
    public async Task Full_alternate_slot_rolls_back_all_mutations_and_messages()
    {
        var source = await CreateSqlFixtureAsync();
        var filler = await CreateSqlFixtureAsync(capacity: 1);
        await using (var setupScope = factory.Services.CreateAsyncScope())
        {
            var db = setupScope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
            var destination = await db.InterviewAssessmentSlots.SingleAsync(x => x.Id == filler.SlotId);
            var application = await db.AdmissionApplications
                .Include(x => x.InterviewAppointments)
                .SingleAsync(x => x.Id == source.ApplicationId);
            var appointment = Assert.Single(application.InterviewAppointments);
            typeof(InterviewAssessmentSlot).GetProperty(nameof(InterviewAssessmentSlot.SchoolId))!
                .SetValue(destination, application.SchoolId);
            typeof(InterviewAssessmentSlot).GetProperty(nameof(InterviewAssessmentSlot.SchoolBranchId))!
                .SetValue(destination, application.SchoolBranchId);
            typeof(InterviewAssessmentSlot).GetProperty(nameof(InterviewAssessmentSlot.EducationalStageId))!
                .SetValue(destination, application.EducationalStageId);
            typeof(InterviewAssessmentSlot).GetProperty(nameof(InterviewAssessmentSlot.GradeId))!
                .SetValue(destination, application.GradeId);
            typeof(InterviewAssessmentSlot).GetProperty(nameof(InterviewAssessmentSlot.AcademicYearId))!
                .SetValue(destination, application.AcademicYearId);
            await db.SaveChangesAsync();
        }
        var baseline = await CountsAsync(source.ApplicationId);
        await using var actionScope = factory.Services.CreateAsyncScope();
        var repository = actionScope.ServiceProvider.GetRequiredService<IInterviewAssessmentSlotRepository>();

        var outcome = await repository.ExecuteParentAppointmentActionAsync(new(
            source.ParentUserId, source.ApplicationId, SlotKind.Interview,
            ParentAppointmentActionKind.SelectSlot, filler.SlotId, source.RowVersion,
            $"full-{Guid.NewGuid():N}", null));

        Assert.Equal(ParentAppointmentActionResult.SlotFull, outcome.Result);
        await using var verifyScope = factory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
        var unchanged = await verifyDb.AdmissionInterviewAppointments.AsNoTracking()
            .SingleAsync(x => x.AdmissionApplicationId == source.ApplicationId);
        Assert.Equal(source.SlotId, unchanged.InterviewAssessmentSlotId);
        Assert.Equal(AdmissionAppointmentLifecycle.Proposed, unchanged.Lifecycle);
        Assert.Equal(0, unchanged.ParentRescheduleAttemptCount);
        Assert.Equal(baseline, await CountsAsync(source.ApplicationId));
    }

    [Fact]
    public async Task Parent_reschedule_is_idempotent_and_releases_capacity()
    {
        var fixture = await CreateSqlFixtureAsync();
        var key = $"reschedule-{Guid.NewGuid():N}";
        await using var scope = factory.Services.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IInterviewAssessmentSlotRepository>();
        var request = new ParentAppointmentActionRequest(
            fixture.ParentUserId, fixture.ApplicationId, SlotKind.Interview,
            ParentAppointmentActionKind.RequestReschedule, null, fixture.RowVersion, key, "New time");

        var first = await repository.ExecuteParentAppointmentActionAsync(request);
        var retry = await repository.ExecuteParentAppointmentActionAsync(request);

        Assert.Equal(ParentAppointmentActionResult.Succeeded, first.Result);
        Assert.Equal(ParentAppointmentActionResult.Existing, retry.Result);
        await using var verifyScope = factory.Services.CreateAsyncScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
        var appointment = await db.AdmissionInterviewAppointments.AsNoTracking()
            .SingleAsync(x => x.Id == fixture.AppointmentId);
        Assert.Equal(AdmissionAppointmentLifecycle.RescheduleRequested, appointment.Lifecycle);
        Assert.Equal(1, appointment.ParentRescheduleAttemptCount);
        Assert.Equal(0, await repository.CountActiveAppointmentsAsync(fixture.SlotId));
        Assert.Equal(1, await db.AdmissionAppointmentActionHistory.CountAsync(
            x => x.AdmissionApplicationId == fixture.ApplicationId));
    }

    [Fact]
    public async Task Parent_cancellation_preserves_required_status_and_releases_capacity()
    {
        var fixture = await CreateSqlFixtureAsync();
        await using var scope = factory.Services.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IInterviewAssessmentSlotRepository>();

        var outcome = await repository.ExecuteParentAppointmentActionAsync(new(
            fixture.ParentUserId, fixture.ApplicationId, SlotKind.Interview,
            ParentAppointmentActionKind.Cancel, null, fixture.RowVersion,
            $"cancel-{Guid.NewGuid():N}", "No longer available"));

        Assert.Equal(ParentAppointmentActionResult.Succeeded, outcome.Result);
        await using var verifyScope = factory.Services.CreateAsyncScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
        var application = await db.AdmissionApplications.AsNoTracking()
            .SingleAsync(x => x.Id == fixture.ApplicationId);
        Assert.Equal(AdmissionApplicationStatus.InterviewRequired, application.Status);
        Assert.Equal(0, await repository.CountActiveAppointmentsAsync(fixture.SlotId));
    }

    [Fact]
    public async Task Cross_parent_is_not_found_without_mutation()
    {
        var fixture = await CreateSqlFixtureAsync();
        var baseline = await CountsAsync(fixture.ApplicationId);
        await using var scope = factory.Services.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IInterviewAssessmentSlotRepository>();

        var outcome = await repository.ExecuteParentAppointmentActionAsync(new(
            Guid.NewGuid(), fixture.ApplicationId, SlotKind.Interview,
            ParentAppointmentActionKind.Confirm, null, fixture.RowVersion,
            $"foreign-{Guid.NewGuid():N}", null));

        Assert.Equal(ParentAppointmentActionResult.NotFound, outcome.Result);
        Assert.Equal(baseline, await CountsAsync(fixture.ApplicationId));
    }

    [Fact]
    public async Task Join_without_adapter_returns_stable_unavailable_without_mutation()
    {
        var fixture = await CreateSqlFixtureAsync(online: true);
        await using (var confirmScope = factory.Services.CreateAsyncScope())
        {
            var repository = confirmScope.ServiceProvider
                .GetRequiredService<IInterviewAssessmentSlotRepository>();
            var confirmed = await repository.ExecuteParentAppointmentActionAsync(new(
                fixture.ParentUserId, fixture.ApplicationId, SlotKind.Interview,
                ParentAppointmentActionKind.Confirm, null, fixture.RowVersion,
                $"confirm-online-{Guid.NewGuid():N}", null));
            Assert.Equal(ParentAppointmentActionResult.Succeeded, confirmed.Result);
        }
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
        var rowVersion = await db.AdmissionInterviewAppointments.AsNoTracking()
            .Where(x => x.Id == fixture.AppointmentId).Select(x => x.RowVersion).SingleAsync();
        var repository2 = scope.ServiceProvider.GetRequiredService<IInterviewAssessmentSlotRepository>();
        var baseline = await CountsAsync(fixture.ApplicationId);

        var outcome = await repository2.ExecuteParentAppointmentActionAsync(new(
            fixture.ParentUserId, fixture.ApplicationId, SlotKind.Interview,
            ParentAppointmentActionKind.Join, null, rowVersion, null, null));

        Assert.Equal(ParentAppointmentActionResult.JoinProviderUnavailable, outcome.Result);
        Assert.Equal(baseline, await CountsAsync(fixture.ApplicationId));
    }

    private static AdmissionApplication CreateApplication(AdmissionApplicationStatus status)
    {
        var application = new AdmissionApplication(
            $"TEST-{Guid.NewGuid():N}", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null);
        typeof(AdmissionApplication).GetProperty(nameof(AdmissionApplication.Status))!
            .SetValue(application, status);
        return application;
    }

    private async Task<SqlFixture> CreateSqlFixtureAsync(int capacity = 2, bool online = false)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
        var template = await db.AdmissionApplications.AsNoTracking().FirstAsync();
        var branchTemplate = await db.SchoolBranches.AsNoTracking()
            .FirstAsync(x => x.SchoolId == template.SchoolId);
        var branch = new SchoolBranch(
            template.SchoolId, "فرع اختبار", "Journey test branch",
            $"journey-{Guid.NewGuid():N}", branchTemplate.CityId, branchTemplate.DistrictId, false);
        branch.UpdateAddress("عنوان اختبار", "Test address", null, null, null, null,
            $"REF-{Guid.NewGuid():N}", null, null);
        db.SchoolBranches.Add(branch);
        var child = new ChildProfile(
            template.ParentProfileId, template.ParentUserId, $"Journey {Guid.NewGuid():N}",
            ChildIdentityType.NationalId, $"protected-{Guid.NewGuid():N}",
            $"hash-{Guid.NewGuid():N}", "1234", DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-8)),
            ChildGender.Male, template.GradeId, false, null);
        db.ChildProfiles.Add(child);
        var application = new AdmissionApplication(
            $"PJ-{Guid.NewGuid():N}"[..24], template.ParentUserId, template.ParentProfileId, child.Id,
            template.SchoolId, branch.Id, template.EducationalStageId, template.GradeId,
            template.AcademicYearId, null);
        application.MoveToInterviewRequired();
        var policy = new SchoolInterviewAssessmentPolicy(
            application.SchoolId, InterviewAssessmentRequirementMode.InterviewOnly,
            online ? InterviewAssessmentDeliveryMode.Online : InterviewAssessmentDeliveryMode.OnSite,
            InterviewAssessmentRequiredParticipants.Child,
            30, null, null, 0, true, 2, true, null, null,
            online ? null : "تعليمات", online ? null : "Instructions",
            online ? "تعليمات" : null, online ? "Instructions" : null,
            online ? "missing-adapter" : null, null,
            null, null, null, null, template.ParentUserId);
        application.AddPolicySnapshot(
            AdmissionApplicationInterviewAssessmentPolicySnapshot.FromPolicy(
                application.Id, policy, application.SchoolBranchId, application.EducationalStageId,
                application.GradeId, application.AcademicYearId));
        var start = DateTimeOffset.UtcNow.AddDays(3);
        var slot = new InterviewAssessmentSlot(
            application.SchoolId, application.SchoolBranchId, application.EducationalStageId,
            application.GradeId, application.AcademicYearId, SlotKind.Interview,
            online ? SlotDeliveryMode.Online : SlotDeliveryMode.OnSite,
            start, start.AddMinutes(30), "UTC", capacity,
            null, null, "تعليمات", "Instructions",
            online ? "missing-adapter" : null, null, template.ParentUserId);
        slot.Open(template.ParentUserId);
        var appointment = new AdmissionInterviewAppointment(
            application.Id, start, "UTC",
            online ? AdmissionAppointmentMode.Online : AdmissionAppointmentMode.InPerson,
            online ? null : slot.InstructionsAr, online ? slot.InstructionsAr : null,
            null, null, template.ParentUserId);
        appointment.LinkToSlot(slot.Id);
        application.AddInterviewAppointment(appointment);
        db.AdmissionApplications.Add(application);
        db.InterviewAssessmentSlots.Add(slot);
        await db.SaveChangesAsync();
        await db.Entry(appointment).ReloadAsync();
        return new(application.Id, template.ParentUserId, appointment.Id, slot.Id,
            appointment.RowVersion.ToArray());
    }

    private async Task<(int History, int Outbox)> CountsAsync(Guid applicationId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
        return (
            await db.AdmissionAppointmentActionHistory.CountAsync(
                x => x.AdmissionApplicationId == applicationId),
            await db.NotificationOutboxMessages.CountAsync(
                x => x.RelatedEntityId == applicationId));
    }

    private sealed record SqlFixture(
        Guid ApplicationId, Guid ParentUserId, Guid AppointmentId, Guid SlotId, byte[] RowVersion);
}
