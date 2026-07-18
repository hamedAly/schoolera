using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Schoolera.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSchoolPortal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "FullDescriptionEn",
                table: "Schools",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FullDescriptionAr",
                table: "Schools",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "SchoolAdditionalServices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DescriptionAr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DescriptionEn = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IconKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolAdditionalServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchoolAdditionalServices_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SchoolTeamMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DeactivatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolTeamMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchoolTeamMembers_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Schools_OwnerUserId",
                table: "Schools",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolAdditionalServices_SchoolId_IsActive",
                table: "SchoolAdditionalServices",
                columns: new[] { "SchoolId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_SchoolAdditionalServices_SchoolId_SortOrder",
                table: "SchoolAdditionalServices",
                columns: new[] { "SchoolId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_SchoolTeamMembers_SchoolId",
                table: "SchoolTeamMembers",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolTeamMembers_SchoolId_UserId",
                table: "SchoolTeamMembers",
                columns: new[] { "SchoolId", "UserId" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolTeamMembers_UserId_IsActive",
                table: "SchoolTeamMembers",
                columns: new[] { "UserId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SchoolAdditionalServices");

            migrationBuilder.DropTable(
                name: "SchoolTeamMembers");

            migrationBuilder.DropIndex(
                name: "IX_Schools_OwnerUserId",
                table: "Schools");

            migrationBuilder.AlterColumn<string>(
                name: "FullDescriptionEn",
                table: "Schools",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldMaxLength: 8000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FullDescriptionAr",
                table: "Schools",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldMaxLength: 8000,
                oldNullable: true);
        }
    }
}
