using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Schoolera.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInterviewAssessmentPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdmissionApplicationInterviewAssessmentPolicySnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdmissionApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourcePolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyVersion = table.Column<int>(type: "int", nullable: false),
                    SchoolBranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EducationalStageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GradeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequirementMode = table.Column<int>(type: "int", nullable: false),
                    DeliveryMode = table.Column<int>(type: "int", nullable: true),
                    RequiredParticipants = table.Column<int>(type: "int", nullable: true),
                    ExpectedDurationMinutes = table.Column<int>(type: "int", nullable: true),
                    BookingWindowOpensDaysBefore = table.Column<int>(type: "int", nullable: true),
                    BookingWindowClosesDaysBefore = table.Column<int>(type: "int", nullable: true),
                    MinimumSchedulingLeadTimeHours = table.Column<int>(type: "int", nullable: true),
                    ParentReschedulingAllowed = table.Column<bool>(type: "bit", nullable: false),
                    MaxParentRescheduleAttempts = table.Column<int>(type: "int", nullable: false),
                    ParentCancellationAllowed = table.Column<bool>(type: "bit", nullable: false),
                    PreparationNotesAr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PreparationNotesEn = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OnSiteInstructionsAr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OnSiteInstructionsEn = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OnlineInstructionsAr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OnlineInstructionsEn = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MeetingProviderCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    HybridSelectionAuthority = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionApplicationInterviewAssessmentPolicySnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdmissionApplicationInterviewAssessmentPolicySnapshots_AdmissionApplications_AdmissionApplicationId",
                        column: x => x.AdmissionApplicationId,
                        principalTable: "AdmissionApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SchoolInterviewAssessmentPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolBranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EducationalStageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GradeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ScopeKey = table.Column<string>(type: "nvarchar(140)", maxLength: 140, nullable: false),
                    RequirementMode = table.Column<int>(type: "int", nullable: false),
                    DeliveryMode = table.Column<int>(type: "int", nullable: true),
                    RequiredParticipants = table.Column<int>(type: "int", nullable: true),
                    ExpectedDurationMinutes = table.Column<int>(type: "int", nullable: true),
                    BookingWindowOpensDaysBefore = table.Column<int>(type: "int", nullable: true),
                    BookingWindowClosesDaysBefore = table.Column<int>(type: "int", nullable: true),
                    MinimumSchedulingLeadTimeHours = table.Column<int>(type: "int", nullable: true),
                    ParentReschedulingAllowed = table.Column<bool>(type: "bit", nullable: false),
                    MaxParentRescheduleAttempts = table.Column<int>(type: "int", nullable: false),
                    ParentCancellationAllowed = table.Column<bool>(type: "bit", nullable: false),
                    PreparationNotesAr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PreparationNotesEn = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OnSiteInstructionsAr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OnSiteInstructionsEn = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OnlineInstructionsAr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OnlineInstructionsEn = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MeetingProviderCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    HybridSelectionAuthority = table.Column<int>(type: "int", nullable: true),
                    PublicationStatus = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    PolicyVersion = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PublishedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolInterviewAssessmentPolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchoolInterviewAssessmentPolicies_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SchoolInterviewAssessmentPolicyAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolInterviewAssessmentPolicyAudits", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplicationInterviewAssessmentPolicySnapshots_AdmissionApplicationId",
                table: "AdmissionApplicationInterviewAssessmentPolicySnapshots",
                column: "AdmissionApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplicationInterviewAssessmentPolicySnapshots_SourcePolicyId",
                table: "AdmissionApplicationInterviewAssessmentPolicySnapshots",
                column: "SourcePolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolInterviewAssessmentPolicies_SchoolId_PublicationStatus_IsActive",
                table: "SchoolInterviewAssessmentPolicies",
                columns: new[] { "SchoolId", "PublicationStatus", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_SchoolInterviewAssessmentPolicies_SchoolId_ScopeKey",
                table: "SchoolInterviewAssessmentPolicies",
                columns: new[] { "SchoolId", "ScopeKey" },
                unique: true,
                filter: "[IsActive] = 1 AND [PublicationStatus] = 2");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolInterviewAssessmentPolicyAudits_SchoolId_CreatedAtUtc",
                table: "SchoolInterviewAssessmentPolicyAudits",
                columns: new[] { "SchoolId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdmissionApplicationInterviewAssessmentPolicySnapshots");

            migrationBuilder.DropTable(
                name: "SchoolInterviewAssessmentPolicies");

            migrationBuilder.DropTable(
                name: "SchoolInterviewAssessmentPolicyAudits");
        }
    }
}
