using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;
using Schoolera.Infrastructure.Persistence;
using System.Data;

namespace Schoolera.Infrastructure.Notifications;

/// <summary>Idempotent seed for simulated integrations and notification templates. Never seeds real secrets.</summary>
public sealed class NotificationSeeder(
    SchooleraDbContext dbContext,
    IHostEnvironment environment,
    ILogger<NotificationSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Running notification / integration seed...");
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable, cancellationToken);
            await dbContext.Database.ExecuteSqlRawAsync(
                "EXEC sp_getapplock @Resource = {0}, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 30000",
                ["Schoolera:NotificationSeeder"],
                cancellationToken);
            if (environment.IsDevelopment() || environment.IsEnvironment("Test"))
            {
                await SeedSimulatedIntegrationsAsync(cancellationToken);
            }
            await SeedTemplatesAsync(cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
        logger.LogInformation("Notification / integration seed completed.");
    }

    private async Task SeedSimulatedIntegrationsAsync(CancellationToken cancellationToken)
    {
        await EnsureSimulatedIntegrationAsync(
            IntegrationType.Email,
            "تكامل البريد المحاكى",
            "Simulated Email",
            """{"mode":"simulated","senderEmail":"noreply@schoolera.local"}""",
            cancellationToken);

        await EnsureSimulatedIntegrationAsync(
            IntegrationType.Sms,
            "تكامل الرسائل المحاكى",
            "Simulated SMS",
            """{"mode":"simulated","senderId":"SCHOOLERA"}""",
            cancellationToken);

        await EnsureSimulatedIntegrationAsync(
            IntegrationType.WhatsApp,
            "تكامل واتساب المحاكى",
            "Simulated WhatsApp",
            """{"mode":"simulated","phoneNumberId":"simulated-local"}""",
            cancellationToken);

        await EnsureMapIntegrationAsync(cancellationToken);
        if (environment.IsDevelopment() || environment.IsEnvironment("Test"))
        {
            await EnsureMeetingIntegrationAsync(cancellationToken);
        }
    }

    private async Task EnsureMeetingIntegrationAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.PlatformIntegrationConfigurations.AnyAsync(
                x => x.IntegrationType == IntegrationType.Meeting, cancellationToken))
            return;
        const string settingsJson =
            """
            {
              "environment": "Development",
              "publicNameAr": "اجتماع تجريبي",
              "publicNameEn": "Simulated meeting",
              "maximumDurationMinutes": 180,
              "joinBeforeMinutes": 15,
              "joinAfterMinutes": 0,
              "hostBeforeMinutes": 30,
              "requestTimeoutSeconds": 30,
              "maxRetryAttempts": 3,
              "retryDelaySeconds": 30,
              "supportsMeetingCreation": true,
              "supportsParentAccess": true,
              "supportsHostAccess": true,
              "supportsCancellation": true,
              "supportsStatusQuery": true,
              "simulatedScenario": "Success"
            }
            """;
        var entity = new PlatformIntegrationConfiguration(
            IntegrationType.Meeting,
            IntegrationProviderCodes.Simulated,
            "اجتماع تجريبي (للتطوير فقط)",
            "Simulated Meeting (Development only)",
            settingsJson,
            settingsSchemaVersion: 1,
            sortOrder: 20);
        entity.Activate();
        entity.SetAsDefault();
        await dbContext.PlatformIntegrationConfigurations.AddAsync(entity, cancellationToken);
    }

    private async Task EnsureMapIntegrationAsync(CancellationToken cancellationToken)
    {
        var exists = await dbContext.PlatformIntegrationConfigurations
            .AnyAsync(item => item.IntegrationType == IntegrationType.Map, cancellationToken);
        if (exists)
        {
            return;
        }

        // Development foundation only — Product must approve Production tile licensing.
        // Tile URL is stored in DB SettingsJson (not appsettings / Angular environment).
        const string settingsJson =
            """
            {
              "providerCode": "LeafletOsm",
              "tileUrlTemplate": "https://tile.openstreetmap.org/{z}/{x}/{y}.png",
              "attributionText": "© OpenStreetMap contributors",
              "defaultLatitude": 30.0444,
              "defaultLongitude": 31.2357,
              "defaultZoom": 11,
              "minZoom": 5,
              "maxZoom": 18,
              "requestTimeoutSeconds": 30
            }
            """;

        var entity = new PlatformIntegrationConfiguration(
            IntegrationType.Map,
            IntegrationProviderCodes.LeafletOsm,
            "خريطة البحث (تطوير)",
            "School Search Map (Development)",
            settingsJson,
            settingsSchemaVersion: 1,
            sortOrder: 10);
        entity.Activate();
        entity.SetAsDefault();
        await dbContext.PlatformIntegrationConfigurations.AddAsync(entity, cancellationToken);
    }

    private async Task EnsureSimulatedIntegrationAsync(
        IntegrationType type,
        string displayNameAr,
        string displayNameEn,
        string settingsJson,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.PlatformIntegrationConfigurations
            .AnyAsync(item => item.IntegrationType == type, cancellationToken);
        if (exists)
        {
            return;
        }

        var entity = new PlatformIntegrationConfiguration(
            type,
            IntegrationProviderCodes.Simulated,
            displayNameAr,
            displayNameEn,
            settingsJson,
            settingsSchemaVersion: 1,
            sortOrder: 0);
        entity.Activate();
        entity.SetAsDefault();
        await dbContext.PlatformIntegrationConfigurations.AddAsync(entity, cancellationToken);
    }

    private async Task SeedTemplatesAsync(CancellationToken cancellationToken)
    {
        await EnsureTemplateAsync(
            NotificationEventType.AccountVerification,
            NotificationChannel.InApp,
            "ar",
            "account_verification.inapp.ar",
            null,
            "رمز التحقق الخاص بك جاهز في Schoolera.",
            "displayName",
            cancellationToken);

        await EnsureTemplateAsync(
            NotificationEventType.AccountVerification,
            NotificationChannel.InApp,
            "en",
            "account_verification.inapp.en",
            null,
            "Your Schoolera verification code is ready.",
            "displayName",
            cancellationToken);

        await EnsureTemplateAsync(
            NotificationEventType.AccountVerification,
            NotificationChannel.Email,
            "ar",
            "account_verification.email.ar",
            "تأكيد البريد الإلكتروني",
            "مرحباً {{displayName}}، رمز التحقق الخاص بك جاهز. أدخل الرمز من صفحة التحقق خلال المدة المحددة.",
            "displayName",
            cancellationToken);

        await EnsureTemplateAsync(
            NotificationEventType.AccountVerification,
            NotificationChannel.Email,
            "en",
            "account_verification.email.en",
            "Verify your email",
            "Hello {{displayName}}, your verification code is ready. Enter it on the verification page before it expires.",
            "displayName",
            cancellationToken);

        await EnsureTemplateAsync(
            NotificationEventType.AdmissionApplicationSubmitted,
            NotificationChannel.InApp,
            "ar",
            "admission_submitted.inapp.ar",
            null,
            "تم استلام طلب القبول {{applicationNumber}} لمدرسة {{schoolName}}.",
            "applicationNumber,schoolName",
            cancellationToken);

        await EnsureTemplateAsync(
            NotificationEventType.AdmissionApplicationSubmitted,
            NotificationChannel.InApp,
            "en",
            "admission_submitted.inapp.en",
            null,
            "Admission application {{applicationNumber}} for {{schoolName}} was received.",
            "applicationNumber,schoolName",
            cancellationToken);

        await EnsureTemplateAsync(
            NotificationEventType.AdmissionApplicationSubmitted,
            NotificationChannel.Email,
            "ar",
            "admission_submitted.email.ar",
            "تم استلام طلب القبول",
            "تم استلام طلب القبول رقم {{applicationNumber}} لمدرسة {{schoolName}}.",
            "applicationNumber,schoolName",
            cancellationToken);

        await EnsureTemplateAsync(
            NotificationEventType.AdmissionApplicationSubmitted,
            NotificationChannel.Email,
            "en",
            "admission_submitted.email.en",
            "Admission application received",
            "We received admission application {{applicationNumber}} for {{schoolName}}.",
            "applicationNumber,schoolName",
            cancellationToken);

        await EnsureTemplateAsync(
            NotificationEventType.AdmissionsOpened,
            NotificationChannel.InApp,
            "ar",
            "admissions_opened.inapp.ar",
            null,
            "فُتحت طلبات القبول في {{schoolName}} للمرحلة {{stageName}}.",
            "schoolName,stageName",
            cancellationToken);

        await EnsureTemplateAsync(
            NotificationEventType.AdmissionsOpened,
            NotificationChannel.InApp,
            "en",
            "admissions_opened.inapp.en",
            null,
            "Admissions are now open at {{schoolName}} for {{stageName}}.",
            "schoolName,stageName",
            cancellationToken);

        await EnsureTemplateAsync(
            NotificationEventType.AdmissionsOpened,
            NotificationChannel.Email,
            "ar",
            "admissions_opened.email.ar",
            "فتح باب القبول",
            "فُتحت طلبات القبول في {{schoolName}} للمرحلة {{stageName}}.",
            "schoolName,stageName",
            cancellationToken);

        await EnsureTemplateAsync(
            NotificationEventType.AdmissionsOpened,
            NotificationChannel.Email,
            "en",
            "admissions_opened.email.en",
            "Admissions are open",
            "Admissions are now open at {{schoolName}} for {{stageName}}.",
            "schoolName,stageName",
            cancellationToken);

        await SeedAppointmentTemplatesAsync(cancellationToken);
        await SeedSupportTicketTemplatesAsync(cancellationToken);
        await SeedCourierOperationalTemplatesAsync(cancellationToken);
    }

    private async Task SeedCourierOperationalTemplatesAsync(CancellationToken cancellationToken)
    {
        const string variables = "providerName,providerCode,status,safeCode";
        await EnsureTemplateAsync(
            NotificationEventType.CourierHealthIncident,
            NotificationChannel.InApp,
            "ar",
            "courier_health_incident.inapp.ar",
            null,
            "تنبيه تشغيلي: تكامل الشحن {{providerName}} ({{providerCode}}) بحالة {{status}}. الرمز الآمن: {{safeCode}}.",
            variables,
            cancellationToken);
        await EnsureTemplateAsync(
            NotificationEventType.CourierHealthIncident,
            NotificationChannel.InApp,
            "en",
            "courier_health_incident.inapp.en",
            null,
            "Operational alert: courier integration {{providerName}} ({{providerCode}}) is {{status}}. Safe code: {{safeCode}}.",
            variables,
            cancellationToken);
    }

    private async Task SeedAppointmentTemplatesAsync(CancellationToken cancellationToken)
    {
        var templates = new[]
        {
            (NotificationEventType.AppointmentProposed, "appointment_proposed",
                "تم اقتراح موعد جديد لطلب القبول {{applicationNumber}}.",
                "A new appointment was proposed for admission application {{applicationNumber}}."),
            (NotificationEventType.AppointmentConfirmed, "appointment_confirmed",
                "تم تأكيد موعد طلب القبول {{applicationNumber}}.",
                "The appointment for admission application {{applicationNumber}} was confirmed."),
            (NotificationEventType.AppointmentChanged, "appointment_changed",
                "تم تحديث موعد طلب القبول {{applicationNumber}}.",
                "The appointment for admission application {{applicationNumber}} was updated."),
            (NotificationEventType.AppointmentCancelled, "appointment_cancelled",
                "تم إلغاء موعد طلب القبول {{applicationNumber}}.",
                "The appointment for admission application {{applicationNumber}} was cancelled."),
            (NotificationEventType.ParentAppointmentConfirmed, "parent_appointment_confirmed",
                "أكد ولي الأمر موعد طلب القبول {{applicationNumber}}.",
                "The parent confirmed the appointment for application {{applicationNumber}}."),
            (NotificationEventType.ParentAlternateSlotSelected, "parent_alternate_selected",
                "اختار ولي الأمر موعداً بديلاً لطلب القبول {{applicationNumber}}.",
                "The parent selected an alternate slot for application {{applicationNumber}}."),
            (NotificationEventType.ParentAppointmentRescheduleRequested, "parent_reschedule_requested",
                "طلب ولي الأمر إعادة جدولة موعد طلب القبول {{applicationNumber}}.",
                "The parent requested rescheduling for application {{applicationNumber}}."),
            (NotificationEventType.ParentAppointmentCancelled, "parent_appointment_cancelled",
                "ألغى ولي الأمر موعد طلب القبول {{applicationNumber}}.",
                "The parent cancelled the appointment for application {{applicationNumber}}."),
            (NotificationEventType.AppointmentCompleted, "appointment_completed",
                "تم إكمال موعد طلب القبول {{applicationNumber}}.",
                "The appointment for admission application {{applicationNumber}} was completed."),
            (NotificationEventType.AppointmentNoShow, "appointment_no_show",
                "تم تسجيل عدم الحضور لموعد طلب القبول {{applicationNumber}}.",
                "A no-show was recorded for admission application {{applicationNumber}}."),
            (NotificationEventType.OnlineMeetingReady, "online_meeting_ready",
                "أصبح الاجتماع الإلكتروني لطلب القبول {{applicationNumber}} جاهزاً.",
                "The online meeting for admission application {{applicationNumber}} is ready."),
            (NotificationEventType.OnlineMeetingProvisioningFailed, "online_meeting_failed",
                "تعذر تجهيز الاجتماع الإلكتروني لطلب القبول {{applicationNumber}}. يرجى التواصل مع المدرسة.",
                "The online meeting for admission application {{applicationNumber}} could not be prepared. Please contact the school."),
            (NotificationEventType.OnlineMeetingCancelled, "online_meeting_cancelled",
                "لم يعد الاجتماع الإلكتروني لطلب القبول {{applicationNumber}} متاحاً.",
                "The online meeting for admission application {{applicationNumber}} is no longer available."),
        };
        foreach (var (eventType, code, ar, en) in templates)
        {
            await EnsureTemplateAsync(eventType, NotificationChannel.InApp, "ar",
                $"{code}.inapp.ar", null, ar, "applicationNumber", cancellationToken);
            await EnsureTemplateAsync(eventType, NotificationChannel.InApp, "en",
                $"{code}.inapp.en", null, en, "applicationNumber", cancellationToken);
        }
    }

    private async Task SeedSupportTicketTemplatesAsync(CancellationToken cancellationToken)
    {
        var vars = "ticketReference,subject,status";

        await EnsureTemplateAsync(
            NotificationEventType.SupportTicketCreated,
            NotificationChannel.InApp,
            "ar",
            "support_ticket_created.inapp.ar",
            null,
            "تم إنشاء تذكرة الدعم {{ticketReference}}.",
            vars,
            cancellationToken);
        await EnsureTemplateAsync(
            NotificationEventType.SupportTicketCreated,
            NotificationChannel.InApp,
            "en",
            "support_ticket_created.inapp.en",
            null,
            "Support ticket {{ticketReference}} was created.",
            vars,
            cancellationToken);
        await EnsureTemplateAsync(
            NotificationEventType.SupportTicketCreated,
            NotificationChannel.Email,
            "ar",
            "support_ticket_created.email.ar",
            "تذكرة دعم جديدة",
            "تم إنشاء تذكرة الدعم {{ticketReference}}: {{subject}}.",
            vars,
            cancellationToken);
        await EnsureTemplateAsync(
            NotificationEventType.SupportTicketCreated,
            NotificationChannel.Email,
            "en",
            "support_ticket_created.email.en",
            "New support ticket",
            "Support ticket {{ticketReference}} was created: {{subject}}.",
            vars,
            cancellationToken);

        await EnsureTemplateAsync(
            NotificationEventType.SupportTicketReply,
            NotificationChannel.InApp,
            "ar",
            "support_ticket_reply.inapp.ar",
            null,
            "رد جديد على تذكرة الدعم {{ticketReference}}.",
            vars,
            cancellationToken);
        await EnsureTemplateAsync(
            NotificationEventType.SupportTicketReply,
            NotificationChannel.InApp,
            "en",
            "support_ticket_reply.inapp.en",
            null,
            "New reply on support ticket {{ticketReference}}.",
            vars,
            cancellationToken);
        await EnsureTemplateAsync(
            NotificationEventType.SupportTicketReply,
            NotificationChannel.Email,
            "ar",
            "support_ticket_reply.email.ar",
            "رد على تذكرة الدعم",
            "يوجد رد جديد على تذكرة الدعم {{ticketReference}}.",
            vars,
            cancellationToken);
        await EnsureTemplateAsync(
            NotificationEventType.SupportTicketReply,
            NotificationChannel.Email,
            "en",
            "support_ticket_reply.email.en",
            "Support ticket reply",
            "There is a new reply on support ticket {{ticketReference}}.",
            vars,
            cancellationToken);

        await EnsureTemplateAsync(
            NotificationEventType.SupportTicketWaitingForCustomer,
            NotificationChannel.InApp,
            "ar",
            "support_ticket_waiting.inapp.ar",
            null,
            "تذكرة الدعم {{ticketReference}} بانتظار ردك.",
            vars,
            cancellationToken);
        await EnsureTemplateAsync(
            NotificationEventType.SupportTicketWaitingForCustomer,
            NotificationChannel.InApp,
            "en",
            "support_ticket_waiting.inapp.en",
            null,
            "Support ticket {{ticketReference}} is waiting for your reply.",
            vars,
            cancellationToken);

        await EnsureTemplateAsync(
            NotificationEventType.SupportTicketResolved,
            NotificationChannel.InApp,
            "ar",
            "support_ticket_resolved.inapp.ar",
            null,
            "تم حل تذكرة الدعم {{ticketReference}}.",
            vars,
            cancellationToken);
        await EnsureTemplateAsync(
            NotificationEventType.SupportTicketResolved,
            NotificationChannel.InApp,
            "en",
            "support_ticket_resolved.inapp.en",
            null,
            "Support ticket {{ticketReference}} was resolved.",
            vars,
            cancellationToken);

        await EnsureTemplateAsync(
            NotificationEventType.SupportTicketClosed,
            NotificationChannel.InApp,
            "ar",
            "support_ticket_closed.inapp.ar",
            null,
            "تم إغلاق تذكرة الدعم {{ticketReference}}.",
            vars,
            cancellationToken);
        await EnsureTemplateAsync(
            NotificationEventType.SupportTicketClosed,
            NotificationChannel.InApp,
            "en",
            "support_ticket_closed.inapp.en",
            null,
            "Support ticket {{ticketReference}} was closed.",
            vars,
            cancellationToken);

        await EnsureTemplateAsync(
            NotificationEventType.SupportTicketReopened,
            NotificationChannel.InApp,
            "ar",
            "support_ticket_reopened.inapp.ar",
            null,
            "تمت إعادة فتح تذكرة الدعم {{ticketReference}}.",
            vars,
            cancellationToken);
        await EnsureTemplateAsync(
            NotificationEventType.SupportTicketReopened,
            NotificationChannel.InApp,
            "en",
            "support_ticket_reopened.inapp.en",
            null,
            "Support ticket {{ticketReference}} was reopened.",
            vars,
            cancellationToken);
    }

    private async Task EnsureTemplateAsync(
        NotificationEventType eventType,
        NotificationChannel channel,
        string culture,
        string code,
        string? subject,
        string body,
        string allowedVariablesCsv,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.NotificationTemplates
            .AnyAsync(item => item.Code == code, cancellationToken);
        if (exists)
        {
            return;
        }

        var template = new NotificationTemplate(eventType, channel, culture, code);
        var version = template.AddVersion(subject, body, allowedVariablesCsv, null, null);
        version.Publish();
        await dbContext.NotificationTemplates.AddAsync(template, cancellationToken);
    }
}
