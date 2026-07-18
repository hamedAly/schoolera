using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Schoolera.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSchoolOnboarding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SchoolOnboardingApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CurrentStep = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SubmittedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReviewStartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ChangesRequestedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastResubmittedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ApprovedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RejectedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedSchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrganizationNameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OrganizationNameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LegalName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CountryCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    RegistrationOrLicenseNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NormalizedRegistrationNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TaxRegistrationNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LegalForm = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OrganizationAddress = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    OrganizationWebsite = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RepresentativeFullNameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RepresentativeFullNameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RepresentativeNationalOrIdentityReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RepresentativeJobTitleAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    RepresentativeJobTitleEn = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    RepresentativeEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RepresentativePhone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    SchoolNameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SchoolNameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SchoolType = table.Column<int>(type: "int", nullable: true),
                    GenderType = table.Column<int>(type: "int", nullable: true),
                    FoundedYear = table.Column<int>(type: "int", nullable: true),
                    SchoolShortDescriptionAr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SchoolShortDescriptionEn = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SchoolWebsiteUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequestedSlug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DistrictId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AddressLineAr = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    AddressLineEn = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    BuildingNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    StreetName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Landmark = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    LocalAddressReference = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    PublicPhone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    PublicEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    WhatsAppOrAlternatePhone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolOnboardingApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchoolOnboardingApplications_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SchoolOnboardingApplications_Districts_DistrictId",
                        column: x => x.DistrictId,
                        principalTable: "Districts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SchoolOnboardingApplications_Schools_ApprovedSchoolId",
                        column: x => x.ApprovedSchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SchoolOnboardingDocumentTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolOnboardingDocumentTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SchoolOnboardingStatusHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousStatus = table.Column<int>(type: "int", nullable: true),
                    NewStatus = table.Column<int>(type: "int", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerVisibleReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    InternalNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolOnboardingStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchoolOnboardingStatusHistory_SchoolOnboardingApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "SchoolOnboardingApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SchoolOnboardingDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    StoredFileReference = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    Sha256Hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UploadedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReplacedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolOnboardingDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchoolOnboardingDocuments_SchoolOnboardingApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "SchoolOnboardingApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SchoolOnboardingDocuments_SchoolOnboardingDocumentTypes_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalTable: "SchoolOnboardingDocumentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SchoolOnboardingApplications_ApprovedSchoolId",
                table: "SchoolOnboardingApplications",
                column: "ApprovedSchoolId",
                unique: true,
                filter: "[ApprovedSchoolId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolOnboardingApplications_CityId",
                table: "SchoolOnboardingApplications",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolOnboardingApplications_DistrictId",
                table: "SchoolOnboardingApplications",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolOnboardingApplications_OwnerUserId_Active",
                table: "SchoolOnboardingApplications",
                column: "OwnerUserId",
                unique: true,
                filter: "[Status] < 5");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolOnboardingApplications_Registration_Active",
                table: "SchoolOnboardingApplications",
                columns: new[] { "CountryCode", "NormalizedRegistrationNumber" },
                unique: true,
                filter: "[NormalizedRegistrationNumber] IS NOT NULL AND [Status] <> 6");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolOnboardingApplications_Status",
                table: "SchoolOnboardingApplications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolOnboardingDocuments_ApplicationId_DocumentTypeId_IsCurrent",
                table: "SchoolOnboardingDocuments",
                columns: new[] { "ApplicationId", "DocumentTypeId", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_SchoolOnboardingDocuments_Current_Unique",
                table: "SchoolOnboardingDocuments",
                columns: new[] { "ApplicationId", "DocumentTypeId" },
                unique: true,
                filter: "[IsCurrent] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolOnboardingDocuments_DocumentTypeId",
                table: "SchoolOnboardingDocuments",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolOnboardingDocumentTypes_Code",
                table: "SchoolOnboardingDocumentTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SchoolOnboardingStatusHistory_ApplicationId_CreatedAtUtc",
                table: "SchoolOnboardingStatusHistory",
                columns: new[] { "ApplicationId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SchoolOnboardingDocuments");

            migrationBuilder.DropTable(
                name: "SchoolOnboardingStatusHistory");

            migrationBuilder.DropTable(
                name: "SchoolOnboardingDocumentTypes");

            migrationBuilder.DropTable(
                name: "SchoolOnboardingApplications");
        }
    }
}
