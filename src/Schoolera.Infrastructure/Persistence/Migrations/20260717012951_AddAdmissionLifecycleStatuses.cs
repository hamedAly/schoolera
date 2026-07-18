using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Schoolera.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdmissionLifecycleStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AdmissionApplications_ActiveDuplicate",
                table: "AdmissionApplications");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RegisteredAtUtc",
                table: "AdmissionApplications",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "WaitingListEnteredAtUtc",
                table: "AdmissionApplications",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WaitingListPosition",
                table: "AdmissionApplications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WaitingListReason",
                table: "AdmissionApplications",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "WaitingListReviewDate",
                table: "AdmissionApplications",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AdmissionAssessmentAppointments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdmissionApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduledAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TimeZoneId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Mode = table.Column<int>(type: "int", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OnlineInstructions = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ParentVisibleNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PreparationInstructions = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Lifecycle = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CancelledAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    OutcomeNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionAssessmentAppointments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdmissionAssessmentAppointments_AdmissionApplications_AdmissionApplicationId",
                        column: x => x.AdmissionApplicationId,
                        principalTable: "AdmissionApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdmissionInterviewAppointments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdmissionApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduledAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TimeZoneId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Mode = table.Column<int>(type: "int", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OnlineInstructions = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ParentVisibleNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PreparationInstructions = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Lifecycle = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CancelledAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    OutcomeNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionInterviewAppointments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdmissionInterviewAppointments_AdmissionApplications_AdmissionApplicationId",
                        column: x => x.AdmissionApplicationId,
                        principalTable: "AdmissionApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdmissionMissingItemsRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdmissionApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentVisibleReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ResponseDeadlineUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ClearedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClearedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionMissingItemsRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdmissionMissingItemsRequests_AdmissionApplications_AdmissionApplicationId",
                        column: x => x.AdmissionApplicationId,
                        principalTable: "AdmissionApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdmissionMissingItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MissingItemsRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    LabelAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LabelEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RequirementSnapshotId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    QuestionSnapshotId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ParentSnapshotField = table.Column<int>(type: "int", nullable: true),
                    ChildSnapshotField = table.Column<int>(type: "int", nullable: true),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionMissingItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdmissionMissingItems_AdmissionMissingItemsRequests_MissingItemsRequestId",
                        column: x => x.MissingItemsRequestId,
                        principalTable: "AdmissionMissingItemsRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplications_ActiveDuplicate",
                table: "AdmissionApplications",
                columns: new[] { "ChildProfileId", "SchoolId", "SchoolBranchId", "GradeId", "AcademicYearId" },
                unique: true,
                filter: "[Status] IN (1, 2, 3, 4, 7, 8, 9, 10, 11)");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionAssessmentAppointments_AdmissionApplicationId_Lifecycle",
                table: "AdmissionAssessmentAppointments",
                columns: new[] { "AdmissionApplicationId", "Lifecycle" });

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionInterviewAppointments_AdmissionApplicationId_Lifecycle",
                table: "AdmissionInterviewAppointments",
                columns: new[] { "AdmissionApplicationId", "Lifecycle" });

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionMissingItems_MissingItemsRequestId",
                table: "AdmissionMissingItems",
                column: "MissingItemsRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionMissingItems_QuestionSnapshotId",
                table: "AdmissionMissingItems",
                column: "QuestionSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionMissingItems_RequirementSnapshotId",
                table: "AdmissionMissingItems",
                column: "RequirementSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionMissingItemsRequests_AdmissionApplicationId_ClearedAtUtc",
                table: "AdmissionMissingItemsRequests",
                columns: new[] { "AdmissionApplicationId", "ClearedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdmissionAssessmentAppointments");

            migrationBuilder.DropTable(
                name: "AdmissionInterviewAppointments");

            migrationBuilder.DropTable(
                name: "AdmissionMissingItems");

            migrationBuilder.DropTable(
                name: "AdmissionMissingItemsRequests");

            migrationBuilder.DropIndex(
                name: "IX_AdmissionApplications_ActiveDuplicate",
                table: "AdmissionApplications");

            migrationBuilder.DropColumn(
                name: "RegisteredAtUtc",
                table: "AdmissionApplications");

            migrationBuilder.DropColumn(
                name: "WaitingListEnteredAtUtc",
                table: "AdmissionApplications");

            migrationBuilder.DropColumn(
                name: "WaitingListPosition",
                table: "AdmissionApplications");

            migrationBuilder.DropColumn(
                name: "WaitingListReason",
                table: "AdmissionApplications");

            migrationBuilder.DropColumn(
                name: "WaitingListReviewDate",
                table: "AdmissionApplications");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplications_ActiveDuplicate",
                table: "AdmissionApplications",
                columns: new[] { "ChildProfileId", "SchoolId", "SchoolBranchId", "GradeId", "AcademicYearId" },
                unique: true,
                filter: "[Status] IN (1, 2, 3, 4)");
        }
    }
}
