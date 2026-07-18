using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Schoolera.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCmsFaqHomepageAndContact : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CmsPages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TitleAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TitleEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContentAr = table.Column<string>(type: "nvarchar(max)", maxLength: 50000, nullable: false),
                    ContentEn = table.Column<string>(type: "nvarchar(max)", maxLength: 50000, nullable: false),
                    MetaTitleAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MetaTitleEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MetaDescriptionAr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MetaDescriptionEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsSystemPage = table.Column<bool>(type: "bit", nullable: false),
                    PublishedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CmsPages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContactRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ConsentAccepted = table.Column<bool>(type: "bit", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReviewedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AdminNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FaqCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaqCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HomepageContents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HeroTitleAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HeroTitleEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HeroSubtitleAr = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    HeroSubtitleEn = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    PrimaryCtaLabelAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PrimaryCtaLabelEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PrimaryCtaUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SecondaryCtaLabelAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SecondaryCtaLabelEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SecondaryCtaUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SchoolsSectionTitleAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SchoolsSectionTitleEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ParentJourneyTitleAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ParentJourneyTitleEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ParentJourneyTextAr = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ParentJourneyTextEn = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    SchoolJourneyTitleAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SchoolJourneyTitleEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SchoolJourneyTextAr = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    SchoolJourneyTextEn = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    FaqSectionTitleAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FaqSectionTitleEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FaqSectionSubtitleAr = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FaqSectionSubtitleEn = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PublishedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomepageContents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FaqItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FaqCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionAr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    QuestionEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AnswerAr = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: false),
                    AnswerEn = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaqItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FaqItems_FaqCategories_FaqCategoryId",
                        column: x => x.FaqCategoryId,
                        principalTable: "FaqCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CmsPages_PublishedAtUtc",
                table: "CmsPages",
                column: "PublishedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CmsPages_Slug",
                table: "CmsPages",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CmsPages_Status",
                table: "CmsPages",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ContactRequests_Category",
                table: "ContactRequests",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_ContactRequests_CreatedAtUtc",
                table: "ContactRequests",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ContactRequests_Reference",
                table: "ContactRequests",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContactRequests_Status",
                table: "ContactRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_FaqCategories_IsPublished",
                table: "FaqCategories",
                column: "IsPublished");

            migrationBuilder.CreateIndex(
                name: "IX_FaqCategories_Slug",
                table: "FaqCategories",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FaqCategories_SortOrder",
                table: "FaqCategories",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_FaqItems_FaqCategoryId_SortOrder",
                table: "FaqItems",
                columns: new[] { "FaqCategoryId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_FaqItems_IsPublished",
                table: "FaqItems",
                column: "IsPublished");

            migrationBuilder.CreateIndex(
                name: "IX_HomepageContents_PublishedAtUtc",
                table: "HomepageContents",
                column: "PublishedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_HomepageContents_Status",
                table: "HomepageContents",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CmsPages");

            migrationBuilder.DropTable(
                name: "ContactRequests");

            migrationBuilder.DropTable(
                name: "FaqItems");

            migrationBuilder.DropTable(
                name: "HomepageContents");

            migrationBuilder.DropTable(
                name: "FaqCategories");
        }
    }
}
