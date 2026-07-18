using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Schoolera.Infrastructure.Persistence.Migrations;

public partial class AddSecureMeetingDelivery : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AdmissionMeetingSessions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AdmissionApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AppointmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Kind = table.Column<int>(type: "int", nullable: false),
                SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SchoolBranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                IntegrationConfigurationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ProviderCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                ProviderEnvironment = table.Column<int>(type: "int", nullable: false),
                SettingsSchemaVersion = table.Column<int>(type: "int", nullable: false),
                ConfigurationRowVersion = table.Column<byte[]>(type: "varbinary(32)", maxLength: 32, nullable: false),
                Generation = table.Column<int>(type: "int", nullable: false),
                ProvisioningIdempotencyKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                ScheduledStartAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ScheduledEndAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                ProviderMeetingReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                ProvisioningAttemptCount = table.Column<int>(type: "int", nullable: false),
                LastAttemptAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                NextRetryAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                ProvisionedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                CancellationRequestedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                CancellationAttemptCount = table.Column<int>(type: "int", nullable: false),
                CancelledAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                ExpirationAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                LastSafeProviderStatusCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                LastSafeFailureCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AdmissionMeetingSessions", x => x.Id);
                table.ForeignKey(
                    name: "FK_AdmissionMeetingSessions_AdmissionApplications_AdmissionApplicationId",
                    column: x => x.AdmissionApplicationId,
                    principalTable: "AdmissionApplications",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_AdmissionMeetingSessions_PlatformIntegrationConfigurations_IntegrationConfigurationId",
                    column: x => x.IntegrationConfigurationId,
                    principalTable: "PlatformIntegrationConfigurations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "AdmissionMeetingSessionHistory",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AdmissionMeetingSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Action = table.Column<int>(type: "int", nullable: false),
                PreviousStatus = table.Column<int>(type: "int", nullable: false),
                NewStatus = table.Column<int>(type: "int", nullable: false),
                SafeCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                IdempotencyKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AdmissionMeetingSessionHistory", x => x.Id);
                table.ForeignKey(
                    name: "FK_AdmissionMeetingSessionHistory_AdmissionMeetingSessions_AdmissionMeetingSessionId",
                    column: x => x.AdmissionMeetingSessionId,
                    principalTable: "AdmissionMeetingSessions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AdmissionMeetingSessionHistory_AdmissionMeetingSessionId_CreatedAtUtc",
            table: "AdmissionMeetingSessionHistory",
            columns: new[] { "AdmissionMeetingSessionId", "CreatedAtUtc" });
        migrationBuilder.CreateIndex(
            name: "IX_AdmissionMeetingSessionHistory_AdmissionMeetingSessionId_IdempotencyKey",
            table: "AdmissionMeetingSessionHistory",
            columns: new[] { "AdmissionMeetingSessionId", "IdempotencyKey" },
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_AdmissionMeetingSessions_AdmissionApplicationId",
            table: "AdmissionMeetingSessions",
            column: "AdmissionApplicationId");
        migrationBuilder.CreateIndex(
            name: "IX_AdmissionMeetingSessions_AppointmentId_Kind_Generation",
            table: "AdmissionMeetingSessions",
            columns: new[] { "AppointmentId", "Kind", "Generation" },
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_AdmissionMeetingSessions_IntegrationConfigurationId",
            table: "AdmissionMeetingSessions",
            column: "IntegrationConfigurationId");
        migrationBuilder.CreateIndex(
            name: "IX_AdmissionMeetingSessions_ProvisioningIdempotencyKey",
            table: "AdmissionMeetingSessions",
            column: "ProvisioningIdempotencyKey",
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_AdmissionMeetingSessions_Status_NextRetryAtUtc",
            table: "AdmissionMeetingSessions",
            columns: new[] { "Status", "NextRetryAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AdmissionMeetingSessionHistory");
        migrationBuilder.DropTable(name: "AdmissionMeetingSessions");
    }
}
