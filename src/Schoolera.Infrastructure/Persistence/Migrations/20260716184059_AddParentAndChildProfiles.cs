using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Schoolera.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddParentAndChildProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ParentProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlternatePhone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    AddressLine = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DistrictId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PreferredContactMethod = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParentProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParentProfiles_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParentProfiles_Districts_DistrictId",
                        column: x => x.DistrictId,
                        principalTable: "Districts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChildProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IdentityType = table.Column<int>(type: "int", nullable: false),
                    ProtectedIdentityValue = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    IdentityLookupHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IdentityLastFour = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Gender = table.Column<int>(type: "int", nullable: false),
                    CurrentGradeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HasSpecialNeeds = table.Column<bool>(type: "bit", nullable: false),
                    SpecialNeedsNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChildProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChildProfiles_Grades_CurrentGradeId",
                        column: x => x.CurrentGradeId,
                        principalTable: "Grades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChildProfiles_ParentProfiles_ParentProfileId",
                        column: x => x.ParentProfileId,
                        principalTable: "ParentProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChildProfiles_CurrentGradeId",
                table: "ChildProfiles",
                column: "CurrentGradeId");

            migrationBuilder.CreateIndex(
                name: "IX_ChildProfiles_ParentProfileId",
                table: "ChildProfiles",
                column: "ParentProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ChildProfiles_ParentUserId_IdentityLookupHash",
                table: "ChildProfiles",
                columns: new[] { "ParentUserId", "IdentityLookupHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChildProfiles_ParentUserId_IsActive",
                table: "ChildProfiles",
                columns: new[] { "ParentUserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ParentProfiles_CityId",
                table: "ParentProfiles",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_ParentProfiles_DistrictId",
                table: "ParentProfiles",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_ParentProfiles_UserId",
                table: "ParentProfiles",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChildProfiles");

            migrationBuilder.DropTable(
                name: "ParentProfiles");
        }
    }
}
