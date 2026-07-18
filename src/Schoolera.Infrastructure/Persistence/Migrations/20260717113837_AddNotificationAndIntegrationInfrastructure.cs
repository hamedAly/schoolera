using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Schoolera.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationAndIntegrationInfrastructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotificationOutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    Culture = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    TemplateVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TemplateCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TemplateVersionNumber = table.Column<int>(type: "int", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BodyOrPayload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    NextAttemptAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ProcessingStartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SentAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeliveredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FailedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReadAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeadLetteredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IntegrationConfigurationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProviderMessageId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DeduplicationKey = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    SafeMetadataJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    LastSafeFailureCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RelatedSchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RelatedEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActionPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationOutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    Culture = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ParentAdmissionOpenSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolBranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EducationalStageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GradeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PreferredChannel = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UnsubscribedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParentAdmissionOpenSubscriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ParentNotificationPreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InAppEnabled = table.Column<bool>(type: "bit", nullable: false),
                    EmailEnabled = table.Column<bool>(type: "bit", nullable: false),
                    SmsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    WhatsAppEnabled = table.Column<bool>(type: "bit", nullable: false),
                    EmailConsentAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SmsConsentAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    WhatsAppConsentAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EmailConsentSource = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SmsConsentSource = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    WhatsAppConsentSource = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OptionalAdmissionsOpenEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParentNotificationPreferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlatformIntegrationConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntegrationType = table.Column<int>(type: "int", nullable: false),
                    ProviderCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DisplayNameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DisplayNameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SettingsJson = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: false),
                    SettingsSchemaVersion = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    HealthStatus = table.Column<int>(type: "int", nullable: false),
                    LastHealthCheckAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastSuccessfulUseAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastSafeFailureCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformIntegrationConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationTemplateVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AllowedVariablesCsv = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ProviderTemplateId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    PublishedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationTemplateVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationTemplateVersions_NotificationTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "NotificationTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationOutboxMessages_DeduplicationKey",
                table: "NotificationOutboxMessages",
                column: "DeduplicationKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationOutboxMessages_EventType",
                table: "NotificationOutboxMessages",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationOutboxMessages_RecipientUserId_Channel_CreatedAtUtc",
                table: "NotificationOutboxMessages",
                columns: new[] { "RecipientUserId", "Channel", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationOutboxMessages_RelatedSchoolId",
                table: "NotificationOutboxMessages",
                column: "RelatedSchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationOutboxMessages_Status_NextAttemptAtUtc",
                table: "NotificationOutboxMessages",
                columns: new[] { "Status", "NextAttemptAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplates_Code",
                table: "NotificationTemplates",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplates_EventChannelCulture",
                table: "NotificationTemplates",
                columns: new[] { "EventType", "Channel", "Culture" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplateVersions_TemplateId_IsPublished_VersionNumber",
                table: "NotificationTemplateVersions",
                columns: new[] { "TemplateId", "IsPublished", "VersionNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplateVersions_TemplateId_VersionNumber",
                table: "NotificationTemplateVersions",
                columns: new[] { "TemplateId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParentAdmissionOpenSubscriptions_ActiveScope",
                table: "ParentAdmissionOpenSubscriptions",
                columns: new[] { "ParentUserId", "SchoolId", "SchoolBranchId", "EducationalStageId", "GradeId", "AcademicYearId" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ParentAdmissionOpenSubscriptions_ParentUserId",
                table: "ParentAdmissionOpenSubscriptions",
                column: "ParentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ParentAdmissionOpenSubscriptions_SchoolId_IsActive",
                table: "ParentAdmissionOpenSubscriptions",
                columns: new[] { "SchoolId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ParentNotificationPreferences_ParentUserId",
                table: "ParentNotificationPreferences",
                column: "ParentUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformIntegrationConfigurations_DefaultPerType",
                table: "PlatformIntegrationConfigurations",
                column: "IntegrationType",
                unique: true,
                filter: "[IsDefault] = 1 AND [IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformIntegrationConfigurations_IntegrationType_ProviderCode",
                table: "PlatformIntegrationConfigurations",
                columns: new[] { "IntegrationType", "ProviderCode" });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformIntegrationConfigurations_IsActive",
                table: "PlatformIntegrationConfigurations",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationOutboxMessages");

            migrationBuilder.DropTable(
                name: "NotificationTemplateVersions");

            migrationBuilder.DropTable(
                name: "ParentAdmissionOpenSubscriptions");

            migrationBuilder.DropTable(
                name: "ParentNotificationPreferences");

            migrationBuilder.DropTable(
                name: "PlatformIntegrationConfigurations");

            migrationBuilder.DropTable(
                name: "NotificationTemplates");
        }
    }
}
