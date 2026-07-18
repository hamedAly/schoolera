using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Schoolera.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChildProfileExtensionsAndDocumentVault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrentSchoolName",
                table: "ChildProfiles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HealthNotes",
                table: "ChildProfiles",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Hobbies",
                table: "ChildProfiles",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImprovementAreas",
                table: "ChildProfiles",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PreferredStudyLanguage",
                table: "ChildProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Skills",
                table: "ChildProfiles",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Strengths",
                table: "ChildProfiles",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmittedChildCurrentSchoolName",
                table: "AdmissionApplications",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SubmittedChildHasSpecialNeeds",
                table: "AdmissionApplications",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmittedChildHobbies",
                table: "AdmissionApplications",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmittedChildImprovementAreas",
                table: "AdmissionApplications",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubmittedChildPreferredStudyLanguage",
                table: "AdmissionApplications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmittedChildSkills",
                table: "AdmissionApplications",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmittedChildSpecialNeedsNotes",
                table: "AdmissionApplications",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmittedChildStrengths",
                table: "AdmissionApplications",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceVaultDocumentId",
                table: "AdmissionApplicationAttachments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ChildDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChildProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<int>(type: "int", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChildDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChildDocuments_ChildProfiles_ChildProfileId",
                        column: x => x.ChildProfileId,
                        principalTable: "ChildProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplicationAttachments_AdmissionApplicationId_SourceVaultDocumentId",
                table: "AdmissionApplicationAttachments",
                columns: new[] { "AdmissionApplicationId", "SourceVaultDocumentId" });

            migrationBuilder.CreateIndex(
                name: "IX_ChildDocuments_ChildProfileId_DocumentType",
                table: "ChildDocuments",
                columns: new[] { "ChildProfileId", "DocumentType" });

            migrationBuilder.CreateIndex(
                name: "IX_ChildDocuments_ParentUserId_ChildProfileId",
                table: "ChildDocuments",
                columns: new[] { "ParentUserId", "ChildProfileId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChildDocuments");

            migrationBuilder.DropIndex(
                name: "IX_AdmissionApplicationAttachments_AdmissionApplicationId_SourceVaultDocumentId",
                table: "AdmissionApplicationAttachments");

            migrationBuilder.DropColumn(
                name: "CurrentSchoolName",
                table: "ChildProfiles");

            migrationBuilder.DropColumn(
                name: "HealthNotes",
                table: "ChildProfiles");

            migrationBuilder.DropColumn(
                name: "Hobbies",
                table: "ChildProfiles");

            migrationBuilder.DropColumn(
                name: "ImprovementAreas",
                table: "ChildProfiles");

            migrationBuilder.DropColumn(
                name: "PreferredStudyLanguage",
                table: "ChildProfiles");

            migrationBuilder.DropColumn(
                name: "Skills",
                table: "ChildProfiles");

            migrationBuilder.DropColumn(
                name: "Strengths",
                table: "ChildProfiles");

            migrationBuilder.DropColumn(
                name: "SubmittedChildCurrentSchoolName",
                table: "AdmissionApplications");

            migrationBuilder.DropColumn(
                name: "SubmittedChildHasSpecialNeeds",
                table: "AdmissionApplications");

            migrationBuilder.DropColumn(
                name: "SubmittedChildHobbies",
                table: "AdmissionApplications");

            migrationBuilder.DropColumn(
                name: "SubmittedChildImprovementAreas",
                table: "AdmissionApplications");

            migrationBuilder.DropColumn(
                name: "SubmittedChildPreferredStudyLanguage",
                table: "AdmissionApplications");

            migrationBuilder.DropColumn(
                name: "SubmittedChildSkills",
                table: "AdmissionApplications");

            migrationBuilder.DropColumn(
                name: "SubmittedChildSpecialNeedsNotes",
                table: "AdmissionApplications");

            migrationBuilder.DropColumn(
                name: "SubmittedChildStrengths",
                table: "AdmissionApplications");

            migrationBuilder.DropColumn(
                name: "SourceVaultDocumentId",
                table: "AdmissionApplicationAttachments");
        }
    }
}
