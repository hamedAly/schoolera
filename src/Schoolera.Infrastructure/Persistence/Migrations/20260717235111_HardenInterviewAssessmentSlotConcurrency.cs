using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Schoolera.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenInterviewAssessmentSlotConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AdmissionInterviewAppointments_AdmissionApplicationId_Lifecycle",
                table: "AdmissionInterviewAppointments");

            migrationBuilder.DropIndex(
                name: "IX_AdmissionAssessmentAppointments_AdmissionApplicationId_Lifecycle",
                table: "AdmissionAssessmentAppointments");

            migrationBuilder.Sql("""
                UPDATE [InterviewAssessmentSlots]
                SET [MeetingProviderCode] = LEFT([MeetingProviderCode], 64)
                WHERE LEN([MeetingProviderCode]) > 64;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "MeetingProviderCode",
                table: "InterviewAssessmentSlots",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionInterviewAppointments_AdmissionApplicationId",
                table: "AdmissionInterviewAppointments",
                column: "AdmissionApplicationId",
                unique: true,
                filter: "[Lifecycle] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionAssessmentAppointments_AdmissionApplicationId",
                table: "AdmissionAssessmentAppointments",
                column: "AdmissionApplicationId",
                unique: true,
                filter: "[Lifecycle] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AdmissionInterviewAppointments_AdmissionApplicationId",
                table: "AdmissionInterviewAppointments");

            migrationBuilder.DropIndex(
                name: "IX_AdmissionAssessmentAppointments_AdmissionApplicationId",
                table: "AdmissionAssessmentAppointments");

            migrationBuilder.AlterColumn<string>(
                name: "MeetingProviderCode",
                table: "InterviewAssessmentSlots",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionInterviewAppointments_AdmissionApplicationId_Lifecycle",
                table: "AdmissionInterviewAppointments",
                columns: new[] { "AdmissionApplicationId", "Lifecycle" });

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionAssessmentAppointments_AdmissionApplicationId_Lifecycle",
                table: "AdmissionAssessmentAppointments",
                columns: new[] { "AdmissionApplicationId", "Lifecycle" });
        }
    }
}
