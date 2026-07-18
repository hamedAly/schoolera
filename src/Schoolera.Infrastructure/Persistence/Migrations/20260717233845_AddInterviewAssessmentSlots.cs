using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Schoolera.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInterviewAssessmentSlots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "InterviewAssessmentSlotId",
                table: "AdmissionInterviewAppointments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InterviewAssessmentSlotId",
                table: "AdmissionAssessmentAppointments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InterviewAssessmentSlotGenerationBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RequestFingerprint = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    BatchReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewAssessmentSlotGenerationBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InterviewAssessmentSlots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolBranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EducationalStageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GradeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    DeliveryMode = table.Column<int>(type: "int", nullable: false),
                    StartAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TimeZoneId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    ResourceKind = table.Column<int>(type: "int", nullable: true),
                    ResourceReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InstructionsAr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    InstructionsEn = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CancellationReasonAr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CancellationReasonEn = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MeetingProviderCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    GenerationBatchReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    OpenedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CancelledAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewAssessmentSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewAssessmentSlots_AcademicYears_AcademicYearId",
                        column: x => x.AcademicYearId,
                        principalTable: "AcademicYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InterviewAssessmentSlots_EducationalStages_EducationalStageId",
                        column: x => x.EducationalStageId,
                        principalTable: "EducationalStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InterviewAssessmentSlots_Grades_GradeId",
                        column: x => x.GradeId,
                        principalTable: "Grades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InterviewAssessmentSlots_SchoolBranches_SchoolBranchId",
                        column: x => x.SchoolBranchId,
                        principalTable: "SchoolBranches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InterviewAssessmentSlots_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InterviewAssessmentSlotAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InterviewAssessmentSlotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewAssessmentSlotAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewAssessmentSlotAudits_InterviewAssessmentSlots_InterviewAssessmentSlotId",
                        column: x => x.InterviewAssessmentSlotId,
                        principalTable: "InterviewAssessmentSlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionInterviewAppointments_InterviewAssessmentSlotId_Lifecycle",
                table: "AdmissionInterviewAppointments",
                columns: new[] { "InterviewAssessmentSlotId", "Lifecycle" });

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionAssessmentAppointments_InterviewAssessmentSlotId_Lifecycle",
                table: "AdmissionAssessmentAppointments",
                columns: new[] { "InterviewAssessmentSlotId", "Lifecycle" });

            migrationBuilder.CreateIndex(
                name: "IX_InterviewAssessmentSlotAudits_InterviewAssessmentSlotId_CreatedAtUtc",
                table: "InterviewAssessmentSlotAudits",
                columns: new[] { "InterviewAssessmentSlotId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_InterviewAssessmentSlotGenerationBatches_BatchReference",
                table: "InterviewAssessmentSlotGenerationBatches",
                column: "BatchReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InterviewAssessmentSlotGenerationBatches_SchoolId_RequestKey",
                table: "InterviewAssessmentSlotGenerationBatches",
                columns: new[] { "SchoolId", "RequestKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InterviewAssessmentSlots_AcademicYearId",
                table: "InterviewAssessmentSlots",
                column: "AcademicYearId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewAssessmentSlots_EducationalStageId",
                table: "InterviewAssessmentSlots",
                column: "EducationalStageId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewAssessmentSlots_GenerationBatchReference",
                table: "InterviewAssessmentSlots",
                column: "GenerationBatchReference");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewAssessmentSlots_GradeId",
                table: "InterviewAssessmentSlots",
                column: "GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewAssessmentSlots_ResourceKind_ResourceReferenceId_StartAtUtc_EndAtUtc",
                table: "InterviewAssessmentSlots",
                columns: new[] { "ResourceKind", "ResourceReferenceId", "StartAtUtc", "EndAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_InterviewAssessmentSlots_SchoolBranchId",
                table: "InterviewAssessmentSlots",
                column: "SchoolBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewAssessmentSlots_SchoolId_SchoolBranchId_StartAtUtc_EndAtUtc",
                table: "InterviewAssessmentSlots",
                columns: new[] { "SchoolId", "SchoolBranchId", "StartAtUtc", "EndAtUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_AdmissionAssessmentAppointments_InterviewAssessmentSlots_InterviewAssessmentSlotId",
                table: "AdmissionAssessmentAppointments",
                column: "InterviewAssessmentSlotId",
                principalTable: "InterviewAssessmentSlots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AdmissionInterviewAppointments_InterviewAssessmentSlots_InterviewAssessmentSlotId",
                table: "AdmissionInterviewAppointments",
                column: "InterviewAssessmentSlotId",
                principalTable: "InterviewAssessmentSlots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdmissionAssessmentAppointments_InterviewAssessmentSlots_InterviewAssessmentSlotId",
                table: "AdmissionAssessmentAppointments");

            migrationBuilder.DropForeignKey(
                name: "FK_AdmissionInterviewAppointments_InterviewAssessmentSlots_InterviewAssessmentSlotId",
                table: "AdmissionInterviewAppointments");

            migrationBuilder.DropTable(
                name: "InterviewAssessmentSlotAudits");

            migrationBuilder.DropTable(
                name: "InterviewAssessmentSlotGenerationBatches");

            migrationBuilder.DropTable(
                name: "InterviewAssessmentSlots");

            migrationBuilder.DropIndex(
                name: "IX_AdmissionInterviewAppointments_InterviewAssessmentSlotId_Lifecycle",
                table: "AdmissionInterviewAppointments");

            migrationBuilder.DropIndex(
                name: "IX_AdmissionAssessmentAppointments_InterviewAssessmentSlotId_Lifecycle",
                table: "AdmissionAssessmentAppointments");

            migrationBuilder.DropColumn(
                name: "InterviewAssessmentSlotId",
                table: "AdmissionInterviewAppointments");

            migrationBuilder.DropColumn(
                name: "InterviewAssessmentSlotId",
                table: "AdmissionAssessmentAppointments");
        }
    }
}
