using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Schoolera.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSchoolCatalogAndTaxonomies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Schools_Name",
                table: "Schools");

            migrationBuilder.AddColumn<string>(
                name: "NameAr",
                table: "Schools",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Schools",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE [Schools]
                SET [NameAr] = LTRIM(RTRIM([Name])),
                    [Slug] = LOWER(REPLACE(REPLACE(REPLACE(LTRIM(RTRIM([Name])), ' ', '-'), '.', ''), '''', ''))
                WHERE [Name] IS NOT NULL;
                """);

            migrationBuilder.Sql("""
                UPDATE [Schools]
                SET [Slug] = CONCAT('school-', ABS(CHECKSUM([Id])))
                WHERE [Slug] IS NULL OR [Slug] = '';
                """);

            migrationBuilder.DropColumn(
                name: "City",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Schools");

            migrationBuilder.AlterColumn<string>(
                name: "NameAr",
                table: "Schools",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "Schools",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoverUrl",
                table: "Schools",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FoundedYear",
                table: "Schools",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FullDescriptionAr",
                table: "Schools",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FullDescriptionEn",
                table: "Schools",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GenderType",
                table: "Schools",
                type: "int",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "Schools",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameEn",
                table: "Schools",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                table: "Schools",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublicEmail",
                table: "Schools",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublicPhone",
                table: "Schools",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SchoolType",
                table: "Schools",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "SeoDescriptionAr",
                table: "Schools",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeoDescriptionEn",
                table: "Schools",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeoTitleAr",
                table: "Schools",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeoTitleEn",
                table: "Schools",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShortDescriptionAr",
                table: "Schools",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShortDescriptionEn",
                table: "Schools",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Schools",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "StudentCount",
                table: "Schools",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAtUtc",
                table: "Schools",
                type: "datetimeoffset",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<string>(
                name: "WebsiteUrl",
                table: "Schools",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WhatsAppNumber",
                table: "Schools",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AcademicYears",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicYears", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Curricula",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DescriptionAr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DescriptionEn = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Curricula", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EducationalStages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EducationalStages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Facilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IconKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Facilities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SchoolImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolBranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CaptionAr = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CaptionEn = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    EducationalStageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AltTextAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AltTextEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchoolImages_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Districts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Districts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Districts_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SchoolCurricula",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurriculumId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolCurricula", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchoolCurricula_Curricula_CurriculumId",
                        column: x => x.CurriculumId,
                        principalTable: "Curricula",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SchoolCurricula_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Grades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EducationalStageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Grades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Grades_EducationalStages_EducationalStageId",
                        column: x => x.EducationalStageId,
                        principalTable: "EducationalStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SchoolFacilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FacilityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolFacilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchoolFacilities_Facilities_FacilityId",
                        column: x => x.FacilityId,
                        principalTable: "Facilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SchoolFacilities_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SchoolBranches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DistrictId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AddressLineAr = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    AddressLineEn = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    BuildingNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    StreetName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Landmark = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AddressReference = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsMainBranch = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolBranches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchoolBranches_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SchoolBranches_Districts_DistrictId",
                        column: x => x.DistrictId,
                        principalTable: "Districts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SchoolBranches_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SchoolStageOfferings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolBranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EducationalStageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GenderType = table.Column<int>(type: "int", nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: true),
                    IsAdmissionOpen = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolStageOfferings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchoolStageOfferings_EducationalStages_EducationalStageId",
                        column: x => x.EducationalStageId,
                        principalTable: "EducationalStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SchoolStageOfferings_SchoolBranches_SchoolBranchId",
                        column: x => x.SchoolBranchId,
                        principalTable: "SchoolBranches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TuitionFees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolBranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EducationalStageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GradeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NotesAr = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NotesEn = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TuitionFees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TuitionFees_AcademicYears_AcademicYearId",
                        column: x => x.AcademicYearId,
                        principalTable: "AcademicYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TuitionFees_EducationalStages_EducationalStageId",
                        column: x => x.EducationalStageId,
                        principalTable: "EducationalStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TuitionFees_Grades_GradeId",
                        column: x => x.GradeId,
                        principalTable: "Grades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TuitionFees_SchoolBranches_SchoolBranchId",
                        column: x => x.SchoolBranchId,
                        principalTable: "SchoolBranches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SchoolGradeOfferings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolStageOfferingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GradeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolGradeOfferings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchoolGradeOfferings_Grades_GradeId",
                        column: x => x.GradeId,
                        principalTable: "Grades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SchoolGradeOfferings_SchoolStageOfferings_SchoolStageOfferingId",
                        column: x => x.SchoolStageOfferingId,
                        principalTable: "SchoolStageOfferings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Schools_NameAr",
                table: "Schools",
                column: "NameAr");

            migrationBuilder.CreateIndex(
                name: "IX_Schools_Slug",
                table: "Schools",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AcademicYears_Slug",
                table: "AcademicYears",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cities_Slug",
                table: "Cities",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Curricula_Slug",
                table: "Curricula",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Districts_CityId_Slug",
                table: "Districts",
                columns: new[] { "CityId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EducationalStages_Slug",
                table: "EducationalStages",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Facilities_Slug",
                table: "Facilities",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Grades_EducationalStageId_Slug",
                table: "Grades",
                columns: new[] { "EducationalStageId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SchoolBranches_CityId",
                table: "SchoolBranches",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolBranches_DistrictId",
                table: "SchoolBranches",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolBranches_SchoolId_Slug",
                table: "SchoolBranches",
                columns: new[] { "SchoolId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SchoolCurricula_CurriculumId",
                table: "SchoolCurricula",
                column: "CurriculumId");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolCurricula_SchoolId_CurriculumId",
                table: "SchoolCurricula",
                columns: new[] { "SchoolId", "CurriculumId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SchoolFacilities_FacilityId",
                table: "SchoolFacilities",
                column: "FacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolFacilities_SchoolId_FacilityId",
                table: "SchoolFacilities",
                columns: new[] { "SchoolId", "FacilityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SchoolGradeOfferings_GradeId",
                table: "SchoolGradeOfferings",
                column: "GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolGradeOfferings_SchoolStageOfferingId_GradeId",
                table: "SchoolGradeOfferings",
                columns: new[] { "SchoolStageOfferingId", "GradeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SchoolImages_SchoolId",
                table: "SchoolImages",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolStageOfferings_EducationalStageId",
                table: "SchoolStageOfferings",
                column: "EducationalStageId");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolStageOfferings_SchoolBranchId_EducationalStageId_GenderType",
                table: "SchoolStageOfferings",
                columns: new[] { "SchoolBranchId", "EducationalStageId", "GenderType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TuitionFees_AcademicYearId",
                table: "TuitionFees",
                column: "AcademicYearId");

            migrationBuilder.CreateIndex(
                name: "IX_TuitionFees_EducationalStageId",
                table: "TuitionFees",
                column: "EducationalStageId");

            migrationBuilder.CreateIndex(
                name: "IX_TuitionFees_GradeId",
                table: "TuitionFees",
                column: "GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_TuitionFees_SchoolBranchId_EducationalStageId_GradeId_AcademicYearId",
                table: "TuitionFees",
                columns: new[] { "SchoolBranchId", "EducationalStageId", "GradeId", "AcademicYearId" },
                unique: true,
                filter: "[IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SchoolCurricula");

            migrationBuilder.DropTable(
                name: "SchoolFacilities");

            migrationBuilder.DropTable(
                name: "SchoolGradeOfferings");

            migrationBuilder.DropTable(
                name: "SchoolImages");

            migrationBuilder.DropTable(
                name: "TuitionFees");

            migrationBuilder.DropTable(
                name: "Curricula");

            migrationBuilder.DropTable(
                name: "Facilities");

            migrationBuilder.DropTable(
                name: "SchoolStageOfferings");

            migrationBuilder.DropTable(
                name: "AcademicYears");

            migrationBuilder.DropTable(
                name: "Grades");

            migrationBuilder.DropTable(
                name: "SchoolBranches");

            migrationBuilder.DropTable(
                name: "EducationalStages");

            migrationBuilder.DropTable(
                name: "Districts");

            migrationBuilder.DropTable(
                name: "Cities");

            migrationBuilder.DropIndex(
                name: "IX_Schools_NameAr",
                table: "Schools");

            migrationBuilder.DropIndex(
                name: "IX_Schools_Slug",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "CoverUrl",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "FoundedYear",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "FullDescriptionAr",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "FullDescriptionEn",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "GenderType",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "NameAr",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "NameEn",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "PublicEmail",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "PublicPhone",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "SchoolType",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "SeoDescriptionAr",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "SeoDescriptionEn",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "SeoTitleAr",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "SeoTitleEn",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "ShortDescriptionAr",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "ShortDescriptionEn",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "StudentCount",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "WebsiteUrl",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "WhatsAppNumber",
                table: "Schools");

            migrationBuilder.RenameColumn(
                name: "Slug",
                table: "Schools",
                newName: "Name");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Schools",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Schools_Name",
                table: "Schools",
                column: "Name");
        }
    }
}
