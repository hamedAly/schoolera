using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Schoolera.Api.Controllers;
using Schoolera.Application.Admissions.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Infrastructure.Persistence;
using System.Reflection;

namespace Schoolera.Tests;

[Collection(WebApplicationFactoryCollection.Name)]
public sealed class InterviewAssessmentSlotTests(SchooleraWebApplicationFactory factory)
{
    private static InterviewAssessmentSlot Create(
        DateTimeOffset? start = null, DateTimeOffset? end = null, int capacity = 10) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            SlotKind.Interview, SlotDeliveryMode.Online,
            start ?? DateTimeOffset.UtcNow.AddDays(2),
            end ?? DateTimeOffset.UtcNow.AddDays(2).AddMinutes(30),
            "UTC", capacity, null, null, "تعليمات", "Instructions", "teams", null, Guid.NewGuid());

    [Fact]
    public void Slot_enums_have_stable_contract_values()
    {
        Assert.Equal(1, (int)SlotKind.Interview);
        Assert.Equal(2, (int)SlotKind.Assessment);
        Assert.Equal(1, (int)SlotDeliveryMode.Online);
        Assert.Equal(2, (int)SlotDeliveryMode.OnSite);
        Assert.Equal(4, (int)SlotStatus.Cancelled);
        Assert.Equal(1, (int)AdmissionAppointmentLifecycle.Proposed);
        Assert.Equal(4, (int)AdmissionAppointmentLifecycle.RescheduleRequested);
        Assert.Equal(5, (int)AdmissionAppointmentLifecycle.Confirmed);
        Assert.Equal(6, (int)AdmissionAppointmentLifecycle.NoShow);
    }

    [Fact]
    public void Adjacent_half_open_intervals_do_not_overlap()
    {
        var start = DateTimeOffset.UtcNow.AddDays(1);
        var middle = start.AddMinutes(30);
        Assert.False(InterviewAssessmentSlot.Overlaps(start, middle, middle, middle.AddMinutes(30)));
        Assert.True(InterviewAssessmentSlot.Overlaps(start, middle, middle.AddMinutes(-1), middle.AddMinutes(30)));
    }

    [Fact]
    public void Capacity_is_bounded()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(capacity: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(capacity: 101));
        Assert.Equal(100, Create(capacity: 100).Capacity);
    }

    [Fact]
    public void Draft_open_close_reopen_cancel_transitions_are_enforced()
    {
        var actor = Guid.NewGuid();
        var slot = Create();
        slot.Open(actor);
        slot.Close(actor);
        slot.Reopen(actor);
        Assert.True(slot.Cancel("سبب", "Reason", actor));
        Assert.False(slot.Cancel("سبب", "Reason", actor));
        Assert.Throws<InvalidOperationException>(() => slot.Open(actor));
    }

    [Fact]
    public void Open_slot_cannot_be_edited()
    {
        var slot = Create();
        var actor = Guid.NewGuid();
        slot.Open(actor);
        Assert.Throws<InvalidOperationException>(() => slot.UpdateDraftOrUnbooked(
            slot.SchoolBranchId, slot.EducationalStageId, slot.GradeId, slot.AcademicYearId,
            slot.Kind, slot.DeliveryMode, slot.StartAtUtc, slot.EndAtUtc, slot.TimeZoneId,
            slot.Capacity, null, null, slot.InstructionsAr, slot.InstructionsEn,
            slot.MeetingProviderCode, actor));
    }

    [Fact]
    public void Appointment_keeps_slot_link_when_reschedule_is_required()
    {
        var slotId = Guid.NewGuid();
        var appointment = new AdmissionInterviewAppointment(
            Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(1), "UTC",
            AdmissionAppointmentMode.Online, null, "Online", null, null, Guid.NewGuid());
        appointment.LinkToSlot(slotId);
        appointment.RequestReschedule("Slot cancelled", AppointmentRescheduleInitiator.School);
        Assert.Equal(slotId, appointment.InterviewAssessmentSlotId);
        Assert.Equal(AdmissionAppointmentLifecycle.RescheduleRequested, appointment.Lifecycle);
        Assert.False(appointment.IsActiveReservation);
    }

    [Fact]
    public void Invalid_or_unsupported_timezone_is_detectable()
    {
        Assert.Throws<TimeZoneNotFoundException>(() =>
            TimeZoneInfo.FindSystemTimeZoneById("Schoolera/Definitely-Invalid"));
    }

    [Fact]
    public void Ambiguous_time_is_rejected_when_test_zone_exposes_one()
    {
        var zone = TimeZoneInfo.GetSystemTimeZones().FirstOrDefault(z => z.SupportsDaylightSavingTime);
        if (zone is null) return;
        var ambiguous = Enumerable.Range(0, 366)
            .SelectMany(day => Enumerable.Range(0, 24).Select(hour =>
                DateTime.SpecifyKind(DateTime.Today.AddDays(day).AddHours(hour), DateTimeKind.Unspecified)))
            .FirstOrDefault(zone.IsAmbiguousTime);
        if (ambiguous != default) Assert.True(zone.IsAmbiguousTime(ambiguous));
    }

    [Fact]
    public void Every_school_slot_write_endpoint_requires_csrf()
    {
        var writeMethods = typeof(SchoolPortalInterviewAssessmentSlotsController)
            .GetMethods()
            .Where(method => method.GetCustomAttributes(inherit: true).Any(attribute =>
                attribute is HttpPostAttribute or HttpPutAttribute))
            .ToArray();
        Assert.NotEmpty(writeMethods);
        Assert.All(writeMethods, method =>
            Assert.Contains(method.GetCustomAttributes(inherit: true),
                attribute => attribute is ValidateAntiForgeryTokenAttribute));
    }

    [Fact]
    public void Parent_slot_controller_is_read_only()
    {
        var methods = typeof(ParentAvailableAdmissionSlotsController).GetMethods()
            .Where(method => method.DeclaringType == typeof(ParentAvailableAdmissionSlotsController))
            .ToArray();
        Assert.Contains(methods, method => method.GetCustomAttribute<HttpGetAttribute>() is not null);
        Assert.DoesNotContain(methods, method => method.GetCustomAttributes(inherit: true).Any(attribute =>
            attribute is HttpPostAttribute or HttpPutAttribute or HttpDeleteAttribute));
    }

    [Fact]
    public async Task Concurrent_final_seat_assignment_allows_exactly_one_appointment()
    {
        await using var setupScope = factory.Services.CreateAsyncScope();
        var setupDb = setupScope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
        var applications = await setupDb.AdmissionApplications.AsNoTracking()
            .Where(x => !x.InterviewAppointments.Any(a =>
                a.Lifecycle == AdmissionAppointmentLifecycle.Proposed ||
                a.Lifecycle == AdmissionAppointmentLifecycle.Confirmed))
            .OrderBy(x => x.Id).Take(2).ToListAsync();
        Assert.Equal(2, applications.Count);
        var first = applications[0];
        var actor = Guid.NewGuid();
        var slot = new InterviewAssessmentSlot(first.SchoolId, first.SchoolBranchId,
            first.EducationalStageId, first.GradeId, first.AcademicYearId, SlotKind.Interview,
            SlotDeliveryMode.Online, DateTimeOffset.UtcNow.AddDays(5),
            DateTimeOffset.UtcNow.AddDays(5).AddMinutes(30), "UTC", 1, null, null,
            "تعليمات", "Instructions", "development", null, actor);
        slot.Open(actor);
        setupDb.InterviewAssessmentSlots.Add(slot);
        await setupDb.SaveChangesAsync();

        await using var scope1 = factory.Services.CreateAsyncScope();
        await using var scope2 = factory.Services.CreateAsyncScope();
        var repo1 = scope1.ServiceProvider.GetRequiredService<IInterviewAssessmentSlotRepository>();
        var repo2 = scope2.ServiceProvider.GetRequiredService<IInterviewAssessmentSlotRepository>();
        var db1 = scope1.ServiceProvider.GetRequiredService<SchooleraDbContext>();
        var db2 = scope2.ServiceProvider.GetRequiredService<SchooleraDbContext>();

        Task AddAppointment(SchooleraDbContext db, Guid applicationId, CancellationToken _)
        {
            var appointment = new AdmissionInterviewAppointment(applicationId, slot.StartAtUtc,
                "UTC", AdmissionAppointmentMode.Online, null, "Online", null, null, actor);
            appointment.LinkToSlot(slot.Id);
            db.AdmissionInterviewAppointments.Add(appointment);
            return Task.CompletedTask;
        }

        var results = await Task.WhenAll(
            repo1.ExecuteAtomicAssignmentAsync(first.SchoolId, slot.Id, 1, null,
                ct => AddAppointment(db1, applications[0].Id, ct)),
            repo2.ExecuteAtomicAssignmentAsync(first.SchoolId, slot.Id, 1, null,
                ct => AddAppointment(db2, applications[1].Id, ct)));

        Assert.Single(results, x => x == SlotAssignmentResult.Succeeded);
        Assert.Single(results, x => x is SlotAssignmentResult.Full or SlotAssignmentResult.ConcurrencyConflict);
        Assert.Equal("admission.application.slotFull", AdmissionErrorCodes.SlotFull);
        await using var verifyScope = factory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
        Assert.Equal(1, await verifyDb.AdmissionInterviewAppointments.CountAsync(x =>
            x.InterviewAssessmentSlotId == slot.Id &&
            (x.Lifecycle == AdmissionAppointmentLifecycle.Proposed ||
             x.Lifecycle == AdmissionAppointmentLifecycle.Confirmed)));
    }

    [Fact]
    public async Task Atomic_open_enforces_exclusive_resource_but_allows_null_and_adjacent()
    {
        var sameStaff = await RaceOpenAsync(Guid.NewGuid(), overlap: true);
        Assert.Single(sameStaff, x => x == AtomicSlotOpenResult.Succeeded);
        Assert.Single(sameStaff, x => x == AtomicSlotOpenResult.ResourceConflict);

        var noResource = await RaceOpenAsync(null, overlap: true);
        Assert.All(noResource, x => Assert.Equal(AtomicSlotOpenResult.Succeeded, x));

        var adjacent = await RaceOpenAsync(Guid.NewGuid(), overlap: false);
        Assert.All(adjacent, x => Assert.Equal(AtomicSlotOpenResult.Succeeded, x));
    }

    [Fact]
    public async Task Atomic_generation_is_idempotent_for_concurrent_same_key()
    {
        await using var setupScope = factory.Services.CreateAsyncScope();
        var db = setupScope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
        var schoolId = await db.Schools.Select(x => x.Id).FirstAsync();
        var key = $"test-{Guid.NewGuid():N}";
        const string fingerprint = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

        await using var scope1 = factory.Services.CreateAsyncScope();
        await using var scope2 = factory.Services.CreateAsyncScope();
        var repo1 = scope1.ServiceProvider.GetRequiredService<IInterviewAssessmentSlotRepository>();
        var repo2 = scope2.ServiceProvider.GetRequiredService<IInterviewAssessmentSlotRepository>();
        var actor = Guid.NewGuid();

        async Task<InterviewAssessmentSlotGenerationBatch> AddBatch(
            IInterviewAssessmentSlotRepository repo, CancellationToken ct)
        {
            var batch = new InterviewAssessmentSlotGenerationBatch(schoolId, key, fingerprint, actor);
            await repo.AddBatchAsync(batch, ct);
            return batch;
        }

        var outcomes = await Task.WhenAll(
            repo1.ExecuteAtomicGenerationAsync(schoolId, key, fingerprint, null, null, [],
                ct => AddBatch(repo1, ct)),
            repo2.ExecuteAtomicGenerationAsync(schoolId, key, fingerprint, null, null, [],
                ct => AddBatch(repo2, ct)));
        Assert.Single(outcomes, x => x.Result == AtomicSlotGenerationResult.Created);
        Assert.Single(outcomes, x => x.Result == AtomicSlotGenerationResult.Existing);
        Assert.Equal(outcomes[0].BatchReference, outcomes[1].BatchReference);

        var differentPayload = await repo1.ExecuteAtomicGenerationAsync(
            schoolId, key,
            "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
            null, null, [],
            _ => throw new InvalidOperationException("Mutation must not run for an existing key."));
        Assert.Equal(AtomicSlotGenerationResult.IdempotencyConflict, differentPayload.Result);
    }

    private async Task<AtomicSlotOpenResult[]> RaceOpenAsync(Guid? resourceId, bool overlap)
    {
        await using var setupScope = factory.Services.CreateAsyncScope();
        var setupDb = setupScope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
        var app = await setupDb.AdmissionApplications.AsNoTracking().FirstAsync();
        var actor = Guid.NewGuid();
        var start = DateTimeOffset.UtcNow.AddDays(10);
        InterviewAssessmentSlot Build(DateTimeOffset slotStart) => new(
            app.SchoolId, app.SchoolBranchId, app.EducationalStageId, app.GradeId,
            app.AcademicYearId, SlotKind.Interview, SlotDeliveryMode.OnSite,
            slotStart, slotStart.AddMinutes(30), "UTC", 10,
            resourceId.HasValue ? SlotResourceKind.StaffMember : null, resourceId,
            "تعليمات", "Instructions", null, null, actor);
        var first = Build(start);
        var second = Build(overlap ? start.AddMinutes(10) : start.AddMinutes(30));
        setupDb.AddRange(first, second);
        await setupDb.SaveChangesAsync();

        await using var scope1 = factory.Services.CreateAsyncScope();
        await using var scope2 = factory.Services.CreateAsyncScope();
        var db1 = scope1.ServiceProvider.GetRequiredService<SchooleraDbContext>();
        var db2 = scope2.ServiceProvider.GetRequiredService<SchooleraDbContext>();
        var tracked1 = await db1.InterviewAssessmentSlots.SingleAsync(x => x.Id == first.Id);
        var tracked2 = await db2.InterviewAssessmentSlots.SingleAsync(x => x.Id == second.Id);
        var repo1 = scope1.ServiceProvider.GetRequiredService<IInterviewAssessmentSlotRepository>();
        var repo2 = scope2.ServiceProvider.GetRequiredService<IInterviewAssessmentSlotRepository>();

        return await Task.WhenAll(
            repo1.ExecuteAtomicOpenAsync(app.SchoolId, tracked1.Id, tracked1.ResourceKind,
                tracked1.ResourceReferenceId, tracked1.StartAtUtc, tracked1.EndAtUtc,
                _ => { tracked1.Open(actor); return Task.CompletedTask; }),
            repo2.ExecuteAtomicOpenAsync(app.SchoolId, tracked2.Id, tracked2.ResourceKind,
                tracked2.ResourceReferenceId, tracked2.StartAtUtc, tracked2.EndAtUtc,
                _ => { tracked2.Open(actor); return Task.CompletedTask; }));
    }
}
