using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Schoolera.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdmissionApplications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdmissionApplicationNumberSequences",
                columns: table => new
                {
                    Year = table.Column<int>(type: "int", nullable: false),
                    LastValue = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionApplicationNumberSequences", x => x.Year);
                });

            migrationBuilder.CreateTable(
                name: "AdmissionApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ParentUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChildProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolBranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EducationalStageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GradeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ParentNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SchoolNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SubmittedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReviewStartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AcceptedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RejectedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CancelledAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SubmittedChildFullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SubmittedSchoolNameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SubmittedSchoolNameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SubmittedBranchNameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SubmittedBranchNameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SubmittedStageNameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SubmittedStageNameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SubmittedGradeNameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SubmittedGradeNameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SubmittedAcademicYearNameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SubmittedAcademicYearNameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdmissionApplications_AcademicYears_AcademicYearId",
                        column: x => x.AcademicYearId,
                        principalTable: "AcademicYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdmissionApplications_ChildProfiles_ChildProfileId",
                        column: x => x.ChildProfileId,
                        principalTable: "ChildProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdmissionApplications_EducationalStages_EducationalStageId",
                        column: x => x.EducationalStageId,
                        principalTable: "EducationalStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdmissionApplications_Grades_GradeId",
                        column: x => x.GradeId,
                        principalTable: "Grades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdmissionApplications_ParentProfiles_ParentProfileId",
                        column: x => x.ParentProfileId,
                        principalTable: "ParentProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdmissionApplications_SchoolBranches_SchoolBranchId",
                        column: x => x.SchoolBranchId,
                        principalTable: "SchoolBranches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdmissionApplications_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdmissionApplicationAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdmissionApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttachmentType = table.Column<int>(type: "int", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionApplicationAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdmissionApplicationAttachments_AdmissionApplications_AdmissionApplicationId",
                        column: x => x.AdmissionApplicationId,
                        principalTable: "AdmissionApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdmissionApplicationHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdmissionApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStatus = table.Column<int>(type: "int", nullable: true),
                    ToStatus = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorRole = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ParentVisible = table.Column<bool>(type: "bit", nullable: false),
                    ParentVisibleNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    InternalNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionApplicationHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdmissionApplicationHistory_AdmissionApplications_AdmissionApplicationId",
                        column: x => x.AdmissionApplicationId,
                        principalTable: "AdmissionApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplicationAttachments_AdmissionApplicationId",
                table: "AdmissionApplicationAttachments",
                column: "AdmissionApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplicationHistory_AdmissionApplicationId_CreatedAtUtc",
                table: "AdmissionApplicationHistory",
                columns: new[] { "AdmissionApplicationId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplications_AcademicYearId",
                table: "AdmissionApplications",
                column: "AcademicYearId");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplications_ActiveDuplicate",
                table: "AdmissionApplications",
                columns: new[] { "ChildProfileId", "SchoolId", "SchoolBranchId", "GradeId", "AcademicYearId" },
                unique: true,
                filter: "[Status] IN (1, 2, 3, 4)");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplications_ApplicationNumber",
                table: "AdmissionApplications",
                column: "ApplicationNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplications_ChildProfileId",
                table: "AdmissionApplications",
                column: "ChildProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplications_CreatedAtUtc",
                table: "AdmissionApplications",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplications_EducationalStageId",
                table: "AdmissionApplications",
                column: "EducationalStageId");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplications_GradeId_AcademicYearId",
                table: "AdmissionApplications",
                columns: new[] { "GradeId", "AcademicYearId" });

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplications_ParentProfileId",
                table: "AdmissionApplications",
                column: "ParentProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplications_ParentUserId",
                table: "AdmissionApplications",
                column: "ParentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplications_SchoolBranchId",
                table: "AdmissionApplications",
                column: "SchoolBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplications_SchoolId_SchoolBranchId",
                table: "AdmissionApplications",
                columns: new[] { "SchoolId", "SchoolBranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplications_Status",
                table: "AdmissionApplications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplications_SubmittedAtUtc",
                table: "AdmissionApplications",
                column: "SubmittedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdmissionApplicationAttachments");

            migrationBuilder.DropTable(
                name: "AdmissionApplicationHistory");

            migrationBuilder.DropTable(
                name: "AdmissionApplicationNumberSequences");

            migrationBuilder.DropTable(
                name: "AdmissionApplications");
        }
    }
}
