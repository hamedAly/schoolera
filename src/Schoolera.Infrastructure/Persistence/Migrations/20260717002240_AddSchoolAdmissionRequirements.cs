using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Schoolera.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSchoolAdmissionRequirements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RequiredDocumentCode",
                table: "AdmissionApplicationAttachments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RequirementSnapshotId",
                table: "AdmissionApplicationAttachments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAtUtc",
                table: "AdmissionApplicationAttachments",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.Sql(
                "UPDATE AdmissionApplicationAttachments SET UpdatedAtUtc = CreatedAtUtc WHERE UpdatedAtUtc = '0001-01-01T00:00:00.0000000+00:00'");

            migrationBuilder.CreateTable(
                name: "AdmissionApplicationRequirementSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdmissionApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceRequirementId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequirementCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DescriptionAr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DescriptionEn = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    SchoolBranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EducationalStageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GradeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProfileFieldCode = table.Column<int>(type: "int", nullable: true),
                    DocumentCode = table.Column<int>(type: "int", nullable: true),
                    AllowedFileExtensions = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    MaxFileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    AllowChildVaultCopy = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionApplicationRequirementSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdmissionApplicationRequirementSnapshots_AdmissionApplications_AdmissionApplicationId",
                        column: x => x.AdmissionApplicationId,
                        principalTable: "AdmissionApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SchoolAdmissionRequirementAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequirementId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequirementCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolAdmissionRequirementAudits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SchoolAdmissionRequirements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequirementCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DescriptionAr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DescriptionEn = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    PublicationStatus = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SchoolBranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EducationalStageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GradeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ScopeKey = table.Column<string>(type: "nvarchar(140)", maxLength: 140, nullable: false),
                    ProfileFieldCode = table.Column<int>(type: "int", nullable: true),
                    DocumentCode = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_SchoolAdmissionRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchoolAdmissionRequirements_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplicationAttachments_AdmissionApplicationId_RequirementSnapshotId",
                table: "AdmissionApplicationAttachments",
                columns: new[] { "AdmissionApplicationId", "RequirementSnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplicationAttachments_RequirementSnapshotId",
                table: "AdmissionApplicationAttachments",
                column: "RequirementSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplicationRequirementSnapshots_AdmissionApplicationId",
                table: "AdmissionApplicationRequirementSnapshots",
                column: "AdmissionApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplicationRequirementSnapshots_AdmissionApplicationId_RequirementCode",
                table: "AdmissionApplicationRequirementSnapshots",
                columns: new[] { "AdmissionApplicationId", "RequirementCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SchoolAdmissionRequirementAudits_SchoolId_CreatedAtUtc",
                table: "SchoolAdmissionRequirementAudits",
                columns: new[] { "SchoolId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SchoolAdmissionRequirements_SchoolId_PublicationStatus_IsActive",
                table: "SchoolAdmissionRequirements",
                columns: new[] { "SchoolId", "PublicationStatus", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_SchoolAdmissionRequirements_SchoolId_RequirementCode_ScopeKey",
                table: "SchoolAdmissionRequirements",
                columns: new[] { "SchoolId", "RequirementCode", "ScopeKey" },
                unique: true,
                filter: "[IsActive] = 1 AND [PublicationStatus] = 2");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolAdmissionRequirements_SchoolId_SortOrder",
                table: "SchoolAdmissionRequirements",
                columns: new[] { "SchoolId", "SortOrder" });

            migrationBuilder.AddForeignKey(
                name: "FK_AdmissionApplicationAttachments_AdmissionApplicationRequirementSnapshots_RequirementSnapshotId",
                table: "AdmissionApplicationAttachments",
                column: "RequirementSnapshotId",
                principalTable: "AdmissionApplicationRequirementSnapshots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdmissionApplicationAttachments_AdmissionApplicationRequirementSnapshots_RequirementSnapshotId",
                table: "AdmissionApplicationAttachments");

            migrationBuilder.DropTable(
                name: "AdmissionApplicationRequirementSnapshots");

            migrationBuilder.DropTable(
                name: "SchoolAdmissionRequirementAudits");

            migrationBuilder.DropTable(
                name: "SchoolAdmissionRequirements");

            migrationBuilder.DropIndex(
                name: "IX_AdmissionApplicationAttachments_AdmissionApplicationId_RequirementSnapshotId",
                table: "AdmissionApplicationAttachments");

            migrationBuilder.DropIndex(
                name: "IX_AdmissionApplicationAttachments_RequirementSnapshotId",
                table: "AdmissionApplicationAttachments");

            migrationBuilder.DropColumn(
                name: "RequiredDocumentCode",
                table: "AdmissionApplicationAttachments");

            migrationBuilder.DropColumn(
                name: "RequirementSnapshotId",
                table: "AdmissionApplicationAttachments");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "AdmissionApplicationAttachments");
        }
    }
}
