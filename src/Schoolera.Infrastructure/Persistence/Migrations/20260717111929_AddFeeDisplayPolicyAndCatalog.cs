using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Schoolera.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFeeDisplayPolicyAndCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TuitionFees_SchoolBranchId_EducationalStageId_GradeId_AcademicYearId",
                table: "TuitionFees");

            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "TuitionFees",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EffectiveFromUtc",
                table: "TuitionFees",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EffectiveToUtc",
                table: "TuitionFees",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InternalNotesAr",
                table: "TuitionFees",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InternalNotesEn",
                table: "TuitionFees",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "TuitionFees",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsStartingFrom",
                table: "TuitionFees",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NameAr",
                table: "TuitionFees",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameEn",
                table: "TuitionFees",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "TuitionFees",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FeeVisibilityPolicy",
                table: "Schools",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SchoolFeeInstallmentDisplays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TuitionFeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AmountMode = table.Column<int>(type: "int", nullable: false),
                    FixedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Percentage = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    DueDateUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DueWindowStartUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DueWindowEndUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    NotesAr = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NotesEn = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolFeeInstallmentDisplays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchoolFeeInstallmentDisplays_TuitionFees_TuitionFeeId",
                        column: x => x.TuitionFeeId,
                        principalTable: "TuitionFees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SchoolFinancialNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TextAr = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    TextEn = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsInternal = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolFinancialNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchoolFinancialNotes_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SchoolPublishedDiscounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TitleAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TitleEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EligibilityDescriptionAr = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    EligibilityDescriptionEn = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DiscountType = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    StartUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SchoolBranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EducationalStageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GradeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolPublishedDiscounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchoolPublishedDiscounts_AcademicYears_AcademicYearId",
                        column: x => x.AcademicYearId,
                        principalTable: "AcademicYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SchoolPublishedDiscounts_EducationalStages_EducationalStageId",
                        column: x => x.EducationalStageId,
                        principalTable: "EducationalStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SchoolPublishedDiscounts_Grades_GradeId",
                        column: x => x.GradeId,
                        principalTable: "Grades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SchoolPublishedDiscounts_SchoolBranches_SchoolBranchId",
                        column: x => x.SchoolBranchId,
                        principalTable: "SchoolBranches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SchoolPublishedDiscounts_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TuitionFees_SchoolBranchId_EducationalStageId_GradeId_AcademicYearId_Category",
                table: "TuitionFees",
                columns: new[] { "SchoolBranchId", "EducationalStageId", "GradeId", "AcademicYearId", "Category" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolFeeInstallmentDisplays_TuitionFeeId",
                table: "SchoolFeeInstallmentDisplays",
                column: "TuitionFeeId");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolFeeInstallmentDisplays_TuitionFeeId_SequenceNumber",
                table: "SchoolFeeInstallmentDisplays",
                columns: new[] { "TuitionFeeId", "SequenceNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_SchoolFinancialNotes_SchoolId_IsInternal_IsActive",
                table: "SchoolFinancialNotes",
                columns: new[] { "SchoolId", "IsInternal", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_SchoolPublishedDiscounts_AcademicYearId",
                table: "SchoolPublishedDiscounts",
                column: "AcademicYearId");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolPublishedDiscounts_EducationalStageId",
                table: "SchoolPublishedDiscounts",
                column: "EducationalStageId");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolPublishedDiscounts_GradeId",
                table: "SchoolPublishedDiscounts",
                column: "GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolPublishedDiscounts_SchoolBranchId",
                table: "SchoolPublishedDiscounts",
                column: "SchoolBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolPublishedDiscounts_SchoolId_IsActive_IsPublished",
                table: "SchoolPublishedDiscounts",
                columns: new[] { "SchoolId", "IsActive", "IsPublished" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SchoolFeeInstallmentDisplays");

            migrationBuilder.DropTable(
                name: "SchoolFinancialNotes");

            migrationBuilder.DropTable(
                name: "SchoolPublishedDiscounts");

            migrationBuilder.DropIndex(
                name: "IX_TuitionFees_SchoolBranchId_EducationalStageId_GradeId_AcademicYearId_Category",
                table: "TuitionFees");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "TuitionFees");

            migrationBuilder.DropColumn(
                name: "EffectiveFromUtc",
                table: "TuitionFees");

            migrationBuilder.DropColumn(
                name: "EffectiveToUtc",
                table: "TuitionFees");

            migrationBuilder.DropColumn(
                name: "InternalNotesAr",
                table: "TuitionFees");

            migrationBuilder.DropColumn(
                name: "InternalNotesEn",
                table: "TuitionFees");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "TuitionFees");

            migrationBuilder.DropColumn(
                name: "IsStartingFrom",
                table: "TuitionFees");

            migrationBuilder.DropColumn(
                name: "NameAr",
                table: "TuitionFees");

            migrationBuilder.DropColumn(
                name: "NameEn",
                table: "TuitionFees");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "TuitionFees");

            migrationBuilder.DropColumn(
                name: "FeeVisibilityPolicy",
                table: "Schools");

            migrationBuilder.CreateIndex(
                name: "IX_TuitionFees_SchoolBranchId_EducationalStageId_GradeId_AcademicYearId",
                table: "TuitionFees",
                columns: new[] { "SchoolBranchId", "EducationalStageId", "GradeId", "AcademicYearId" },
                unique: true,
                filter: "[IsActive] = 1");
        }
    }
}
