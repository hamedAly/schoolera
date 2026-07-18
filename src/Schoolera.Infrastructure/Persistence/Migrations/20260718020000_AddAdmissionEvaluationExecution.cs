using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Schoolera.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SchooleraDbContext))]
[Migration("20260718020000_AddAdmissionEvaluationExecution")]
public sealed class AddAdmissionEvaluationExecution : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_AdmissionInterviewAppointments_AdmissionApplicationId",
            table: "AdmissionInterviewAppointments");
        migrationBuilder.DropIndex(
            name: "IX_AdmissionAssessmentAppointments_AdmissionApplicationId",
            table: "AdmissionAssessmentAppointments");

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "StartedAtUtc",
            table: "AdmissionInterviewAppointments",
            type: "datetimeoffset",
            nullable: true);
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "StartedAtUtc",
            table: "AdmissionAssessmentAppointments",
            type: "datetimeoffset",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "SchoolAdmissionEvaluationTemplates",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SchoolBranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                EducationalStageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                GradeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                ScopeKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Kind = table.Column<int>(type: "int", nullable: false),
                PublicationStatus = table.Column<int>(type: "int", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                Version = table.Column<int>(type: "int", nullable: false),
                PublishedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
            },
            constraints: table => table.PrimaryKey(
                "PK_SchoolAdmissionEvaluationTemplates", x => x.Id));

        migrationBuilder.CreateTable(
            name: "AdmissionEvaluationResults",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AdmissionApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AppointmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SchoolBranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Kind = table.Column<int>(type: "int", nullable: false),
                TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TemplateVersion = table.Column<int>(type: "int", nullable: false),
                TemplateSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                PolicySnapshotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PolicyVersion = table.Column<int>(type: "int", nullable: false),
                AppointmentSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                StartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                EvaluatorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                State = table.Column<int>(type: "int", nullable: false),
                CurrentFinalizedVersion = table.Column<int>(type: "int", nullable: false),
                DraftAnswersJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                ChildAttendance = table.Column<int>(type: "int", nullable: false),
                ParentAttendance = table.Column<int>(type: "int", nullable: false),
                Recommendation = table.Column<int>(type: "int", nullable: false),
                SuggestedParentReasonAr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                SuggestedParentReasonEn = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                InternalNotes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                PendingCorrectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                CorrectsVersionNumber = table.Column<int>(type: "int", nullable: true),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AdmissionEvaluationResults", x => x.Id);
                table.ForeignKey(
                    name: "FK_AdmissionEvaluationResults_AdmissionApplications_AdmissionApplicationId",
                    column: x => x.AdmissionApplicationId,
                    principalTable: "AdmissionApplications",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "SchoolAdmissionEvaluationTemplateAudits",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SchoolAdmissionEvaluationTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Action = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Version = table.Column<int>(type: "int", nullable: false),
                IdempotencyKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
            },
            constraints: table => table.PrimaryKey(
                "PK_SchoolAdmissionEvaluationTemplateAudits", x => x.Id));

        migrationBuilder.CreateTable(
            name: "SchoolAdmissionEvaluationCriteria",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SchoolAdmissionEvaluationTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Type = table.Column<int>(type: "int", nullable: false),
                LabelAr = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                LabelEn = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                HelpTextAr = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                HelpTextEn = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                IsRequired = table.Column<bool>(type: "bit", nullable: false),
                SortOrder = table.Column<int>(type: "int", nullable: false),
                ShortTextMaxLength = table.Column<int>(type: "int", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SchoolAdmissionEvaluationCriteria", x => x.Id);
                table.ForeignKey(
                    name: "FK_SchoolAdmissionEvaluationCriteria_SchoolAdmissionEvaluationTemplates_SchoolAdmissionEvaluationTemplateId",
                    column: x => x.SchoolAdmissionEvaluationTemplateId,
                    principalTable: "SchoolAdmissionEvaluationTemplates",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AdmissionEvaluationResultVersions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AdmissionEvaluationResultId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                VersionNumber = table.Column<int>(type: "int", nullable: false),
                TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TemplateVersion = table.Column<int>(type: "int", nullable: false),
                TemplateSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                PolicySnapshotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PolicyVersion = table.Column<int>(type: "int", nullable: false),
                AppointmentSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                StartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                EndedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ChildAttendance = table.Column<int>(type: "int", nullable: false),
                ParentAttendance = table.Column<int>(type: "int", nullable: false),
                EvaluatorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AnswersJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Recommendation = table.Column<int>(type: "int", nullable: false),
                SuggestedParentReasonAr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                SuggestedParentReasonEn = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                InternalNotes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                FinalizedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                FinalizedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CorrectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                PreviousVersionNumber = table.Column<int>(type: "int", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AdmissionEvaluationResultVersions", x => x.Id);
                table.ForeignKey(
                    name: "FK_AdmissionEvaluationResultVersions_AdmissionEvaluationResults_AdmissionEvaluationResultId",
                    column: x => x.AdmissionEvaluationResultId,
                    principalTable: "AdmissionEvaluationResults",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "AdmissionEvaluationHistory",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AdmissionEvaluationResultId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AdmissionApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AppointmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Kind = table.Column<int>(type: "int", nullable: false),
                Action = table.Column<int>(type: "int", nullable: false),
                VersionNumber = table.Column<int>(type: "int", nullable: true),
                ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                IdempotencyKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                RequestFingerprint = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AdmissionEvaluationHistory", x => x.Id);
                table.ForeignKey(
                    name: "FK_AdmissionEvaluationHistory_AdmissionEvaluationResults_AdmissionEvaluationResultId",
                    column: x => x.AdmissionEvaluationResultId,
                    principalTable: "AdmissionEvaluationResults",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "SchoolAdmissionEvaluationCriterionOptions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SchoolAdmissionEvaluationCriterionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Value = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                LabelAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                LabelEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                SortOrder = table.Column<int>(type: "int", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SchoolAdmissionEvaluationCriterionOptions", x => x.Id);
                table.ForeignKey(
                    name: "FK_SchoolAdmissionEvaluationCriterionOptions_SchoolAdmissionEvaluationCriteria_SchoolAdmissionEvaluationCriterionId",
                    column: x => x.SchoolAdmissionEvaluationCriterionId,
                    principalTable: "SchoolAdmissionEvaluationCriteria",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AdmissionInterviewAppointments_AdmissionApplicationId",
            table: "AdmissionInterviewAppointments",
            column: "AdmissionApplicationId",
            unique: true,
            filter: "[Lifecycle] IN (1,5,7)");
        migrationBuilder.CreateIndex(
            name: "IX_AdmissionAssessmentAppointments_AdmissionApplicationId",
            table: "AdmissionAssessmentAppointments",
            column: "AdmissionApplicationId",
            unique: true,
            filter: "[Lifecycle] IN (1,5,7)");
        migrationBuilder.CreateIndex(
            name: "IX_SchoolAdmissionEvaluationTemplates_SchoolId_PublicationStatus_IsActive",
            table: "SchoolAdmissionEvaluationTemplates",
            columns: new[] { "SchoolId", "PublicationStatus", "IsActive" });
        migrationBuilder.CreateIndex(
            name: "IX_SchoolAdmissionEvaluationTemplates_SchoolId_ScopeKey_Kind_PublicationStatus",
            table: "SchoolAdmissionEvaluationTemplates",
            columns: new[] { "SchoolId", "ScopeKey", "Kind", "PublicationStatus" },
            filter: "[PublicationStatus] = 2 AND [IsActive] = 1");
        migrationBuilder.CreateIndex(
            name: "IX_SchoolAdmissionEvaluationCriteria_SchoolAdmissionEvaluationTemplateId_SortOrder",
            table: "SchoolAdmissionEvaluationCriteria",
            columns: new[] { "SchoolAdmissionEvaluationTemplateId", "SortOrder" });
        migrationBuilder.CreateIndex(
            name: "IX_SchoolAdmissionEvaluationTemplateAudits_SchoolAdmissionEvaluationTemplateId_CreatedAtUtc",
            table: "SchoolAdmissionEvaluationTemplateAudits",
            columns: new[] { "SchoolAdmissionEvaluationTemplateId", "CreatedAtUtc" });
        migrationBuilder.CreateIndex(
            name: "IX_SchoolAdmissionEvaluationTemplateAudits_SchoolId_IdempotencyKey",
            table: "SchoolAdmissionEvaluationTemplateAudits",
            columns: new[] { "SchoolId", "IdempotencyKey" },
            unique: true,
            filter: "[IdempotencyKey] IS NOT NULL");
        migrationBuilder.CreateIndex(
            name: "IX_SchoolAdmissionEvaluationCriterionOptions_SchoolAdmissionEvaluationCriterionId_Value",
            table: "SchoolAdmissionEvaluationCriterionOptions",
            columns: new[] { "SchoolAdmissionEvaluationCriterionId", "Value" },
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_AdmissionEvaluationResults_AdmissionApplicationId_Kind",
            table: "AdmissionEvaluationResults",
            columns: new[] { "AdmissionApplicationId", "Kind" });
        migrationBuilder.CreateIndex(
            name: "IX_AdmissionEvaluationResults_AppointmentId_Kind",
            table: "AdmissionEvaluationResults",
            columns: new[] { "AppointmentId", "Kind" },
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_AdmissionEvaluationResultVersions_AdmissionEvaluationResultId_VersionNumber",
            table: "AdmissionEvaluationResultVersions",
            columns: new[] { "AdmissionEvaluationResultId", "VersionNumber" },
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_AdmissionEvaluationHistory_AdmissionEvaluationResultId_CreatedAtUtc",
            table: "AdmissionEvaluationHistory",
            columns: new[] { "AdmissionEvaluationResultId", "CreatedAtUtc" });
        migrationBuilder.CreateIndex(
            name: "IX_AdmissionEvaluationHistory_AdmissionEvaluationResultId_IdempotencyKey",
            table: "AdmissionEvaluationHistory",
            columns: new[] { "AdmissionEvaluationResultId", "IdempotencyKey" },
            unique: true,
            filter: "[IdempotencyKey] IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AdmissionEvaluationHistory");
        migrationBuilder.DropTable(name: "AdmissionEvaluationResultVersions");
        migrationBuilder.DropTable(name: "SchoolAdmissionEvaluationCriterionOptions");
        migrationBuilder.DropTable(name: "SchoolAdmissionEvaluationTemplateAudits");
        migrationBuilder.DropTable(name: "AdmissionEvaluationResults");
        migrationBuilder.DropTable(name: "SchoolAdmissionEvaluationCriteria");
        migrationBuilder.DropTable(name: "SchoolAdmissionEvaluationTemplates");
        migrationBuilder.DropColumn(name: "StartedAtUtc", table: "AdmissionInterviewAppointments");
        migrationBuilder.DropColumn(name: "StartedAtUtc", table: "AdmissionAssessmentAppointments");
        migrationBuilder.DropIndex(
            name: "IX_AdmissionInterviewAppointments_AdmissionApplicationId",
            table: "AdmissionInterviewAppointments");
        migrationBuilder.DropIndex(
            name: "IX_AdmissionAssessmentAppointments_AdmissionApplicationId",
            table: "AdmissionAssessmentAppointments");
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
    }
}
