using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Schoolera.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddParentAppointmentJourney : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AdmissionInterviewAppointments_AdmissionApplicationId",
                table: "AdmissionInterviewAppointments");

            migrationBuilder.DropIndex(
                name: "IX_AdmissionAssessmentAppointments_AdmissionApplicationId",
                table: "AdmissionAssessmentAppointments");

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "AdmissionInterviewAppointments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ConfirmedAtUtc",
                table: "AdmissionInterviewAppointments",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastParentVisibleRescheduleReason",
                table: "AdmissionInterviewAppointments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NoShowAtUtc",
                table: "AdmissionInterviewAppointments",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentRescheduleAttemptCount",
                table: "AdmissionInterviewAppointments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ProposedAtUtc",
                table: "AdmissionInterviewAppointments",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<int>(
                name: "RescheduleInitiator",
                table: "AdmissionInterviewAppointments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RescheduleRequestedAtUtc",
                table: "AdmissionInterviewAppointments",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AdmissionInterviewAppointments",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "AdmissionAssessmentAppointments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ConfirmedAtUtc",
                table: "AdmissionAssessmentAppointments",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastParentVisibleRescheduleReason",
                table: "AdmissionAssessmentAppointments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NoShowAtUtc",
                table: "AdmissionAssessmentAppointments",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentRescheduleAttemptCount",
                table: "AdmissionAssessmentAppointments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ProposedAtUtc",
                table: "AdmissionAssessmentAppointments",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<int>(
                name: "RescheduleInitiator",
                table: "AdmissionAssessmentAppointments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RescheduleRequestedAtUtc",
                table: "AdmissionAssessmentAppointments",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AdmissionAssessmentAppointments",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.Sql("""
                UPDATE [AdmissionInterviewAppointments]
                SET [ProposedAtUtc] = [CreatedAtUtc]
                WHERE [ProposedAtUtc] = '0001-01-01T00:00:00.0000000+00:00';
                UPDATE [AdmissionAssessmentAppointments]
                SET [ProposedAtUtc] = [CreatedAtUtc]
                WHERE [ProposedAtUtc] = '0001-01-01T00:00:00.0000000+00:00';
                """);

            migrationBuilder.CreateTable(
                name: "AdmissionAppointmentActionHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdmissionApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    PreviousLifecycle = table.Column<int>(type: "int", nullable: false),
                    NewLifecycle = table.Column<int>(type: "int", nullable: false),
                    OldSlotId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NewSlotId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActorType = table.Column<int>(type: "int", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SafeReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionAppointmentActionHistory", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionInterviewAppointments_AdmissionApplicationId",
                table: "AdmissionInterviewAppointments",
                column: "AdmissionApplicationId",
                unique: true,
                filter: "[Lifecycle] IN (1,5)");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionAssessmentAppointments_AdmissionApplicationId",
                table: "AdmissionAssessmentAppointments",
                column: "AdmissionApplicationId",
                unique: true,
                filter: "[Lifecycle] IN (1,5)");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionAppointmentActionHistory_AdmissionApplicationId_CreatedAtUtc",
                table: "AdmissionAppointmentActionHistory",
                columns: new[] { "AdmissionApplicationId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionAppointmentActionHistory_AppointmentId_IdempotencyKey",
                table: "AdmissionAppointmentActionHistory",
                columns: new[] { "AppointmentId", "IdempotencyKey" },
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdmissionAppointmentActionHistory");

            migrationBuilder.DropIndex(
                name: "IX_AdmissionInterviewAppointments_AdmissionApplicationId",
                table: "AdmissionInterviewAppointments");

            migrationBuilder.DropIndex(
                name: "IX_AdmissionAssessmentAppointments_AdmissionApplicationId",
                table: "AdmissionAssessmentAppointments");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "AdmissionInterviewAppointments");

            migrationBuilder.DropColumn(
                name: "ConfirmedAtUtc",
                table: "AdmissionInterviewAppointments");

            migrationBuilder.DropColumn(
                name: "LastParentVisibleRescheduleReason",
                table: "AdmissionInterviewAppointments");

            migrationBuilder.DropColumn(
                name: "NoShowAtUtc",
                table: "AdmissionInterviewAppointments");

            migrationBuilder.DropColumn(
                name: "ParentRescheduleAttemptCount",
                table: "AdmissionInterviewAppointments");

            migrationBuilder.DropColumn(
                name: "ProposedAtUtc",
                table: "AdmissionInterviewAppointments");

            migrationBuilder.DropColumn(
                name: "RescheduleInitiator",
                table: "AdmissionInterviewAppointments");

            migrationBuilder.DropColumn(
                name: "RescheduleRequestedAtUtc",
                table: "AdmissionInterviewAppointments");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AdmissionInterviewAppointments");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "AdmissionAssessmentAppointments");

            migrationBuilder.DropColumn(
                name: "ConfirmedAtUtc",
                table: "AdmissionAssessmentAppointments");

            migrationBuilder.DropColumn(
                name: "LastParentVisibleRescheduleReason",
                table: "AdmissionAssessmentAppointments");

            migrationBuilder.DropColumn(
                name: "NoShowAtUtc",
                table: "AdmissionAssessmentAppointments");

            migrationBuilder.DropColumn(
                name: "ParentRescheduleAttemptCount",
                table: "AdmissionAssessmentAppointments");

            migrationBuilder.DropColumn(
                name: "ProposedAtUtc",
                table: "AdmissionAssessmentAppointments");

            migrationBuilder.DropColumn(
                name: "RescheduleInitiator",
                table: "AdmissionAssessmentAppointments");

            migrationBuilder.DropColumn(
                name: "RescheduleRequestedAtUtc",
                table: "AdmissionAssessmentAppointments");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AdmissionAssessmentAppointments");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionInterviewAppointments_AdmissionApplicationId",
                table: "AdmissionInterviewAppointments",
                column: "AdmissionApplicationId",
                unique: true,
                filter: "[Lifecycle] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionAssessmentAppointments_AdmissionApplicationId",
                table: "AdmissionAssessmentAppointments",
                column: "AdmissionApplicationId",
                unique: true,
                filter: "[Lifecycle] = 1");
        }
    }
}
