using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Schoolera.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChildAgeEligibilityRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdmissionApplicationChildAgeEligibilitySnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdmissionApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RuleVersion = table.Column<int>(type: "int", nullable: false),
                    SchoolBranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EducationalStageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GradeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MinAgeCompletedMonths = table.Column<int>(type: "int", nullable: false),
                    MaxAgeCompletedMonths = table.Column<int>(type: "int", nullable: false),
                    ReferenceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ReferenceDateMode = table.Column<int>(type: "int", nullable: false),
                    CalculatedAgeCompletedMonths = table.Column<int>(type: "int", nullable: true),
                    ResultCode = table.Column<int>(type: "int", nullable: false),
                    CalculatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ManualExceptionAllowedAtEvaluation = table.Column<bool>(type: "bit", nullable: false),
                    ManualExceptionIsApproved = table.Column<bool>(type: "bit", nullable: false),
                    ManualExceptionApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ManualExceptionReasonCode = table.Column<int>(type: "int", nullable: true),
                    ManualExceptionReasonNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ManualExceptionApprovedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    BirthDateSnapshot = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionApplicationChildAgeEligibilitySnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdmissionApplicationChildAgeEligibilitySnapshots_AdmissionApplications_AdmissionApplicationId",
                        column: x => x.AdmissionApplicationId,
                        principalTable: "AdmissionApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SchoolChildAgeEligibilityRuleAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolChildAgeEligibilityRuleAudits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SchoolChildAgeEligibilityRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolBranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EducationalStageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GradeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScopeKey = table.Column<string>(type: "nvarchar(140)", maxLength: 140, nullable: false),
                    MinAgeCompletedMonths = table.Column<int>(type: "int", nullable: false),
                    MaxAgeCompletedMonths = table.Column<int>(type: "int", nullable: false),
                    ReferenceDateMode = table.Column<int>(type: "int", nullable: false),
                    ExplanationAr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ExplanationEn = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ManualExceptionAllowed = table.Column<bool>(type: "bit", nullable: false),
                    PublicationStatus = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RuleVersion = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PublishedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolChildAgeEligibilityRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchoolChildAgeEligibilityRules_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplicationChildAgeEligibilitySnapshots_AdmissionApplicationId",
                table: "AdmissionApplicationChildAgeEligibilitySnapshots",
                column: "AdmissionApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplicationChildAgeEligibilitySnapshots_SourceRuleId",
                table: "AdmissionApplicationChildAgeEligibilitySnapshots",
                column: "SourceRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolChildAgeEligibilityRuleAudits_SchoolId_CreatedAtUtc",
                table: "SchoolChildAgeEligibilityRuleAudits",
                columns: new[] { "SchoolId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SchoolChildAgeEligibilityRules_SchoolId_PublicationStatus_IsActive",
                table: "SchoolChildAgeEligibilityRules",
                columns: new[] { "SchoolId", "PublicationStatus", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_SchoolChildAgeEligibilityRules_SchoolId_ScopeKey",
                table: "SchoolChildAgeEligibilityRules",
                columns: new[] { "SchoolId", "ScopeKey" },
                unique: true,
                filter: "[IsActive] = 1 AND [PublicationStatus] = 2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdmissionApplicationChildAgeEligibilitySnapshots");

            migrationBuilder.DropTable(
                name: "SchoolChildAgeEligibilityRuleAudits");

            migrationBuilder.DropTable(
                name: "SchoolChildAgeEligibilityRules");
        }
    }
}
