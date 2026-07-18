using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Schoolera.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddParentFamilyLegalConsent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CountryId",
                table: "ParentProfiles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FatherEmail",
                table: "ParentProfiles",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FatherFullName",
                table: "ParentProfiles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FatherIdentityLastFour",
                table: "ParentProfiles",
                type: "nvarchar(4)",
                maxLength: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FatherIdentityLookupHash",
                table: "ParentProfiles",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FatherIdentityType",
                table: "ParentProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FatherOccupation",
                table: "ParentProfiles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FatherPhone",
                table: "ParentProfiles",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FatherProtectedIdentityValue",
                table: "ParentProfiles",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FatherQualification",
                table: "ParentProfiles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GovernorateId",
                table: "ParentProfiles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotherEmail",
                table: "ParentProfiles",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotherFullName",
                table: "ParentProfiles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotherIdentityLastFour",
                table: "ParentProfiles",
                type: "nvarchar(4)",
                maxLength: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotherIdentityLookupHash",
                table: "ParentProfiles",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MotherIdentityType",
                table: "ParentProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotherOccupation",
                table: "ParentProfiles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotherPhone",
                table: "ParentProfiles",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotherProtectedIdentityValue",
                table: "ParentProfiles",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotherQualification",
                table: "ParentProfiles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Occupation",
                table: "ParentProfiles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Qualification",
                table: "ParentProfiles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmittedFatherEmail",
                table: "AdmissionApplications",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmittedFatherFullName",
                table: "AdmissionApplications",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmittedFatherMaskedIdentity",
                table: "AdmissionApplications",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmittedFatherOccupation",
                table: "AdmissionApplications",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmittedFatherPhone",
                table: "AdmissionApplications",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmittedFatherQualification",
                table: "AdmissionApplications",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmittedMotherEmail",
                table: "AdmissionApplications",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmittedMotherFullName",
                table: "AdmissionApplications",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmittedMotherMaskedIdentity",
                table: "AdmissionApplications",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmittedMotherOccupation",
                table: "AdmissionApplications",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmittedMotherPhone",
                table: "AdmissionApplications",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmittedMotherQualification",
                table: "AdmissionApplications",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmittedParentAlternatePhone",
                table: "AdmissionApplications",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmittedParentDisplayName",
                table: "AdmissionApplications",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmittedParentEmail",
                table: "AdmissionApplications",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmittedParentPhone",
                table: "AdmissionApplications",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LegalDocumentVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<int>(type: "int", nullable: false),
                    Culture = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", maxLength: 50000, nullable: false),
                    PublishedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsCurrentMandatory = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalDocumentVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LegalAcceptances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LegalDocumentVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Purpose = table.Column<int>(type: "int", nullable: false),
                    AcceptedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalAcceptances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LegalAcceptances_LegalDocumentVersions_LegalDocumentVersionId",
                        column: x => x.LegalDocumentVersionId,
                        principalTable: "LegalDocumentVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParentProfiles_CountryId",
                table: "ParentProfiles",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_ParentProfiles_GovernorateId",
                table: "ParentProfiles",
                column: "GovernorateId");

            migrationBuilder.CreateIndex(
                name: "IX_LegalAcceptances_LegalDocumentVersionId",
                table: "LegalAcceptances",
                column: "LegalDocumentVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_LegalAcceptances_UserId_LegalDocumentVersionId_Purpose",
                table: "LegalAcceptances",
                columns: new[] { "UserId", "LegalDocumentVersionId", "Purpose" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegalAcceptances_UserId_Purpose",
                table: "LegalAcceptances",
                columns: new[] { "UserId", "Purpose" });

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocumentVersions_DocumentType_Culture_IsCurrentMandatory",
                table: "LegalDocumentVersions",
                columns: new[] { "DocumentType", "Culture", "IsCurrentMandatory" });

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocumentVersions_DocumentType_Culture_VersionNumber",
                table: "LegalDocumentVersions",
                columns: new[] { "DocumentType", "Culture", "VersionNumber" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ParentProfiles_Countries_CountryId",
                table: "ParentProfiles",
                column: "CountryId",
                principalTable: "Countries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ParentProfiles_Governorates_GovernorateId",
                table: "ParentProfiles",
                column: "GovernorateId",
                principalTable: "Governorates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParentProfiles_Countries_CountryId",
                table: "ParentProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_ParentProfiles_Governorates_GovernorateId",
                table: "ParentProfiles");

            migrationBuilder.DropTable(
                name: "LegalAcceptances");

            migrationBuilder.DropTable(
                name: "LegalDocumentVersions");

            migrationBuilder.DropIndex(
                name: "IX_ParentProfiles_CountryId",
                table: "ParentProfiles");

            migrationBuilder.DropIndex(
                name: "IX_ParentProfiles_GovernorateId",
                table: "ParentProfiles");

            migrationBuilder.DropColumn(
                name: "CountryId",
                table: "ParentProfiles");

            migrationBuilder.DropColumn(
                name: "FatherEmail",
                table: "ParentProfiles");

            migrationBuilder.DropColumn(
                name: "FatherFullName",
                table: "ParentProfiles");

            migrationBuilder.DropColumn(
                name: "FatherIdentityLastFour",
                table: "ParentProfiles");

            migrationBuilder.DropColumn(
                name: "FatherIdentityLookupHash",
                table: "ParentProfiles");

            migrationBuilder.DropColumn(
                name: "FatherIdentityType",
                table: "ParentProfiles");

            migrationBuilder.DropColumn(
                name: "FatherOccupation",
                table: "ParentProfiles");

            migrationBuilder.DropColumn(
                name: "FatherPhone",
                table: "ParentProfiles");

            migrationBuilder.DropColumn(
                name: "FatherProtectedIdentityValue",
                table: "ParentProfiles");

            migrationBuilder.DropColumn(
                name: "FatherQualification",
                table: "ParentProfiles");

            migrationBuilder.DropColumn(
                name: "GovernorateId",
                table: "ParentProfiles");

            migrationBuilder.DropColumn(
                name: "MotherEmail",
                table: "ParentProfiles");

            migrationBuilder.DropColumn(
                name: "MotherFullName",
                table: "ParentProfiles");

            migrationBuilder.DropColumn(
                name: "MotherIdentityLastFour",
                table: "ParentProfiles");

            migrationBuilder.DropColumn(
                name: "MotherIdentityLookupHash",
                table: "ParentProfiles");

            migrationBuilder.DropColumn(
                name: "MotherIdentityType",
                table: "ParentProfiles");

            migrationBuilder.DropColumn(
                name: "MotherOccupation",
                table: "ParentProfiles");

            migrationBuilder.DropColumn(
                name: "MotherPhone",
                table: "ParentProfiles");

            migrationBuilder.DropColumn(
                name: "MotherProtectedIdentityValue",
                table: "ParentProfiles");

            migrationBuilder.DropColumn(
                name: "MotherQualification",
                table: "ParentProfiles");

            migrationBuilder.DropColumn(
                name: "Occupation",
                table: "ParentProfiles");

            migrationBuilder.DropColumn(
                name: "Qualification",
                table: "ParentProfiles");

            migrationBuilder.DropColumn(
                name: "SubmittedFatherEmail",
                table: "AdmissionApplications");

            migrationBuilder.DropColumn(
                name: "SubmittedFatherFullName",
                table: "AdmissionApplications");

            migrationBuilder.DropColumn(
                name: "SubmittedFatherMaskedIdentity",
                table: "AdmissionApplications");

            migrationBuilder.DropColumn(
                name: "SubmittedFatherOccupation",
                table: "AdmissionApplications");

            migrationBuilder.DropColumn(
                name: "SubmittedFatherPhone",
                table: "AdmissionApplications");

            migrationBuilder.DropColumn(
                name: "SubmittedFatherQualification",
                table: "AdmissionApplications");

            migrationBuilder.DropColumn(
                name: "SubmittedMotherEmail",
                table: "AdmissionApplications");

            migrationBuilder.DropColumn(
                name: "SubmittedMotherFullName",
                table: "AdmissionApplications");

            migrationBuilder.DropColumn(
                name: "SubmittedMotherMaskedIdentity",
                table: "AdmissionApplications");

            migrationBuilder.DropColumn(
                name: "SubmittedMotherOccupation",
                table: "AdmissionApplications");

            migrationBuilder.DropColumn(
                name: "SubmittedMotherPhone",
                table: "AdmissionApplications");

            migrationBuilder.DropColumn(
                name: "SubmittedMotherQualification",
                table: "AdmissionApplications");

            migrationBuilder.DropColumn(
                name: "SubmittedParentAlternatePhone",
                table: "AdmissionApplications");

            migrationBuilder.DropColumn(
                name: "SubmittedParentDisplayName",
                table: "AdmissionApplications");

            migrationBuilder.DropColumn(
                name: "SubmittedParentEmail",
                table: "AdmissionApplications");

            migrationBuilder.DropColumn(
                name: "SubmittedParentPhone",
                table: "AdmissionApplications");
        }
    }
}
