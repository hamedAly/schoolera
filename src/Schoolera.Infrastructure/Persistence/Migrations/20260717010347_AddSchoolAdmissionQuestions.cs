using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Schoolera.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSchoolAdmissionQuestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "QuestionSnapshotId",
                table: "AdmissionApplicationAttachments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AdmissionApplicationQuestionSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdmissionApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceQuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    QuestionCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    QuestionType = table.Column<int>(type: "int", nullable: false),
                    LabelAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LabelEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HelpAr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    HelpEn = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    SchoolBranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EducationalStageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GradeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MinLength = table.Column<int>(type: "int", nullable: true),
                    MaxLength = table.Column<int>(type: "int", nullable: true),
                    MinSelectedOptions = table.Column<int>(type: "int", nullable: true),
                    MaxSelectedOptions = table.Column<int>(type: "int", nullable: true),
                    MinDate = table.Column<DateOnly>(type: "date", nullable: true),
                    MaxDate = table.Column<DateOnly>(type: "date", nullable: true),
                    AllowedFileExtensions = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    MaxFileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    AllowChildVaultCopy = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionApplicationQuestionSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdmissionApplicationQuestionSnapshots_AdmissionApplications_AdmissionApplicationId",
                        column: x => x.AdmissionApplicationId,
                        principalTable: "AdmissionApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SchoolAdmissionQuestionAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolAdmissionQuestionAudits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SchoolAdmissionQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    QuestionType = table.Column<int>(type: "int", nullable: false),
                    LabelAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LabelEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HelpAr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    HelpEn = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    PublicationStatus = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SchoolBranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EducationalStageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GradeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ScopeKey = table.Column<string>(type: "nvarchar(140)", maxLength: 140, nullable: false),
                    MinLength = table.Column<int>(type: "int", nullable: true),
                    MaxLength = table.Column<int>(type: "int", nullable: true),
                    MinSelectedOptions = table.Column<int>(type: "int", nullable: true),
                    MaxSelectedOptions = table.Column<int>(type: "int", nullable: true),
                    MinDate = table.Column<DateOnly>(type: "date", nullable: true),
                    MaxDate = table.Column<DateOnly>(type: "date", nullable: true),
                    AllowedFileExtensions = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    MaxFileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    AllowChildVaultCopy = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PublishedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolAdmissionQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchoolAdmissionQuestions_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdmissionApplicationAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdmissionApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionSnapshotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TextValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SelectedOptionCodes = table.Column<string>(type: "nvarchar(640)", maxLength: 640, nullable: true),
                    DateValue = table.Column<DateOnly>(type: "date", nullable: true),
                    BooleanValue = table.Column<bool>(type: "bit", nullable: true),
                    AttachmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionApplicationAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdmissionApplicationAnswers_AdmissionApplicationAttachments_AttachmentId",
                        column: x => x.AttachmentId,
                        principalTable: "AdmissionApplicationAttachments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdmissionApplicationAnswers_AdmissionApplicationQuestionSnapshots_QuestionSnapshotId",
                        column: x => x.QuestionSnapshotId,
                        principalTable: "AdmissionApplicationQuestionSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdmissionApplicationAnswers_AdmissionApplications_AdmissionApplicationId",
                        column: x => x.AdmissionApplicationId,
                        principalTable: "AdmissionApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdmissionApplicationQuestionSnapshotOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionSnapshotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OptionCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    LabelAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LabelEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionApplicationQuestionSnapshotOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdmissionApplicationQuestionSnapshotOptions_AdmissionApplicationQuestionSnapshots_QuestionSnapshotId",
                        column: x => x.QuestionSnapshotId,
                        principalTable: "AdmissionApplicationQuestionSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SchoolAdmissionQuestionOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolAdmissionQuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OptionCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    LabelAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LabelEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolAdmissionQuestionOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchoolAdmissionQuestionOptions_SchoolAdmissionQuestions_SchoolAdmissionQuestionId",
                        column: x => x.SchoolAdmissionQuestionId,
                        principalTable: "SchoolAdmissionQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplicationAttachments_AdmissionApplicationId_QuestionSnapshotId",
                table: "AdmissionApplicationAttachments",
                columns: new[] { "AdmissionApplicationId", "QuestionSnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplicationAttachments_QuestionSnapshotId",
                table: "AdmissionApplicationAttachments",
                column: "QuestionSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplicationAnswers_AdmissionApplicationId_QuestionSnapshotId",
                table: "AdmissionApplicationAnswers",
                columns: new[] { "AdmissionApplicationId", "QuestionSnapshotId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplicationAnswers_AttachmentId",
                table: "AdmissionApplicationAnswers",
                column: "AttachmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplicationAnswers_QuestionSnapshotId",
                table: "AdmissionApplicationAnswers",
                column: "QuestionSnapshotId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplicationQuestionSnapshotOptions_QuestionSnapshotId_OptionCode",
                table: "AdmissionApplicationQuestionSnapshotOptions",
                columns: new[] { "QuestionSnapshotId", "OptionCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplicationQuestionSnapshots_AdmissionApplicationId_QuestionCode",
                table: "AdmissionApplicationQuestionSnapshots",
                columns: new[] { "AdmissionApplicationId", "QuestionCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplicationQuestionSnapshots_AdmissionApplicationId_SortOrder",
                table: "AdmissionApplicationQuestionSnapshots",
                columns: new[] { "AdmissionApplicationId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_SchoolAdmissionQuestionAudits_SchoolId_CreatedAtUtc",
                table: "SchoolAdmissionQuestionAudits",
                columns: new[] { "SchoolId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SchoolAdmissionQuestionOptions_SchoolAdmissionQuestionId_OptionCode",
                table: "SchoolAdmissionQuestionOptions",
                columns: new[] { "SchoolAdmissionQuestionId", "OptionCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SchoolAdmissionQuestions_SchoolId_QuestionCode_ScopeKey",
                table: "SchoolAdmissionQuestions",
                columns: new[] { "SchoolId", "QuestionCode", "ScopeKey" },
                unique: true,
                filter: "[IsActive] = 1 AND [PublicationStatus] = 2");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolAdmissionQuestions_SchoolId_SortOrder",
                table: "SchoolAdmissionQuestions",
                columns: new[] { "SchoolId", "SortOrder" });

            migrationBuilder.AddForeignKey(
                name: "FK_AdmissionApplicationAttachments_AdmissionApplicationQuestionSnapshots_QuestionSnapshotId",
                table: "AdmissionApplicationAttachments",
                column: "QuestionSnapshotId",
                principalTable: "AdmissionApplicationQuestionSnapshots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdmissionApplicationAttachments_AdmissionApplicationQuestionSnapshots_QuestionSnapshotId",
                table: "AdmissionApplicationAttachments");

            migrationBuilder.DropTable(
                name: "AdmissionApplicationAnswers");

            migrationBuilder.DropTable(
                name: "AdmissionApplicationQuestionSnapshotOptions");

            migrationBuilder.DropTable(
                name: "SchoolAdmissionQuestionAudits");

            migrationBuilder.DropTable(
                name: "SchoolAdmissionQuestionOptions");

            migrationBuilder.DropTable(
                name: "AdmissionApplicationQuestionSnapshots");

            migrationBuilder.DropTable(
                name: "SchoolAdmissionQuestions");

            migrationBuilder.DropIndex(
                name: "IX_AdmissionApplicationAttachments_AdmissionApplicationId_QuestionSnapshotId",
                table: "AdmissionApplicationAttachments");

            migrationBuilder.DropIndex(
                name: "IX_AdmissionApplicationAttachments_QuestionSnapshotId",
                table: "AdmissionApplicationAttachments");

            migrationBuilder.DropColumn(
                name: "QuestionSnapshotId",
                table: "AdmissionApplicationAttachments");
        }
    }
}
