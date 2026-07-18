using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Schoolera.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSchoolTeamRolesAndBranchScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BranchScopeMode",
                table: "SchoolTeamMembers",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "SchoolTeamMemberBranches",
                columns: table => new
                {
                    SchoolTeamMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolBranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolTeamMemberBranches", x => new { x.SchoolTeamMemberId, x.SchoolBranchId });
                    table.ForeignKey(
                        name: "FK_SchoolTeamMemberBranches_SchoolBranches_SchoolBranchId",
                        column: x => x.SchoolBranchId,
                        principalTable: "SchoolBranches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SchoolTeamMemberBranches_SchoolTeamMembers_SchoolTeamMemberId",
                        column: x => x.SchoolTeamMemberId,
                        principalTable: "SchoolTeamMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SchoolTeamMemberBranches_SchoolBranchId",
                table: "SchoolTeamMemberBranches",
                column: "SchoolBranchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SchoolTeamMemberBranches");

            migrationBuilder.DropColumn(
                name: "BranchScopeMode",
                table: "SchoolTeamMembers");
        }
    }
}
