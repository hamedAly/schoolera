using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Schoolera.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicSchoolSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Schools_Status_CreatedAtUtc",
                table: "Schools",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Schools_Status_NameAr",
                table: "Schools",
                columns: new[] { "Status", "NameAr" });

            migrationBuilder.CreateIndex(
                name: "IX_SchoolBranches_IsActive_CityId_DistrictId",
                table: "SchoolBranches",
                columns: new[] { "IsActive", "CityId", "DistrictId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Schools_Status_CreatedAtUtc",
                table: "Schools");

            migrationBuilder.DropIndex(
                name: "IX_Schools_Status_NameAr",
                table: "Schools");

            migrationBuilder.DropIndex(
                name: "IX_SchoolBranches_IsActive_CityId_DistrictId",
                table: "SchoolBranches");
        }
    }
}
