using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Schoolera.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInterviewFaqOwnershipAndScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AcademicYearId",
                table: "FaqItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EducationalStageId",
                table: "FaqItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GradeId",
                table: "FaqItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InterviewCategory",
                table: "FaqItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "FaqItems",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnershipScope",
                table: "FaqItems",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "FaqItems",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "SchoolBranchId",
                table: "FaqItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SchoolId",
                table: "FaqItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FaqItems_InterviewCategory",
                table: "FaqItems",
                column: "InterviewCategory");

            migrationBuilder.CreateIndex(
                name: "IX_FaqItems_OwnershipScope_SchoolId_IsPublished_IsActive",
                table: "FaqItems",
                columns: new[] { "OwnershipScope", "SchoolId", "IsPublished", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_FaqItems_SchoolId_SortOrder",
                table: "FaqItems",
                columns: new[] { "SchoolId", "SortOrder" });

            migrationBuilder.AddForeignKey(
                name: "FK_FaqItems_Schools_SchoolId",
                table: "FaqItems",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FaqItems_Schools_SchoolId",
                table: "FaqItems");

            migrationBuilder.DropIndex(
                name: "IX_FaqItems_InterviewCategory",
                table: "FaqItems");

            migrationBuilder.DropIndex(
                name: "IX_FaqItems_OwnershipScope_SchoolId_IsPublished_IsActive",
                table: "FaqItems");

            migrationBuilder.DropIndex(
                name: "IX_FaqItems_SchoolId_SortOrder",
                table: "FaqItems");

            migrationBuilder.DropColumn(
                name: "AcademicYearId",
                table: "FaqItems");

            migrationBuilder.DropColumn(
                name: "EducationalStageId",
                table: "FaqItems");

            migrationBuilder.DropColumn(
                name: "GradeId",
                table: "FaqItems");

            migrationBuilder.DropColumn(
                name: "InterviewCategory",
                table: "FaqItems");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "FaqItems");

            migrationBuilder.DropColumn(
                name: "OwnershipScope",
                table: "FaqItems");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "FaqItems");

            migrationBuilder.DropColumn(
                name: "SchoolBranchId",
                table: "FaqItems");

            migrationBuilder.DropColumn(
                name: "SchoolId",
                table: "FaqItems");
        }
    }
}
