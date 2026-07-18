using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Schoolera.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFavoritesAndSupportTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FavoriteSchools",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FavoriteSchools", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FavoriteSchools_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupportTicketNumberSequences",
                columns: table => new
                {
                    DayKey = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    LastValue = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTicketNumberSequences", x => x.DayKey);
                });

            migrationBuilder.CreateTable(
                name: "SupportTickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ParentUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdmissionApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceContactRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AssignedSupportAgentUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FirstResponseAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ResolvedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FirstResponseDueAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ResolutionDueAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTickets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupportTicketAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Visibility = table.Column<int>(type: "int", nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTicketAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportTicketAttachments_SupportTickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "SupportTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupportTicketHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FromValue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ToValue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTicketHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportTicketHistory_SupportTickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "SupportTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupportTicketMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorType = table.Column<int>(type: "int", nullable: false),
                    Visibility = table.Column<int>(type: "int", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTicketMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportTicketMessages_SupportTickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "SupportTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteSchools_ParentUserId",
                table: "FavoriteSchools",
                column: "ParentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteSchools_ParentUserId_SchoolId",
                table: "FavoriteSchools",
                columns: new[] { "ParentUserId", "SchoolId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteSchools_SchoolId",
                table: "FavoriteSchools",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTicketAttachments_MessageId",
                table: "SupportTicketAttachments",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTicketAttachments_TicketId",
                table: "SupportTicketAttachments",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTicketHistory_CreatedAtUtc",
                table: "SupportTicketHistory",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTicketHistory_TicketId",
                table: "SupportTicketHistory",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTicketMessages_CreatedAtUtc",
                table: "SupportTicketMessages",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTicketMessages_TicketId",
                table: "SupportTicketMessages",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_AdmissionApplicationId",
                table: "SupportTickets",
                column: "AdmissionApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_AssignedSupportAgentUserId",
                table: "SupportTickets",
                column: "AssignedSupportAgentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_Category",
                table: "SupportTickets",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_CreatedAtUtc",
                table: "SupportTickets",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_ParentUserId",
                table: "SupportTickets",
                column: "ParentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_Priority",
                table: "SupportTickets",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_Reference",
                table: "SupportTickets",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_SourceContactRequestId",
                table: "SupportTickets",
                column: "SourceContactRequestId",
                unique: true,
                filter: "[SourceContactRequestId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_Status",
                table: "SupportTickets",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FavoriteSchools");

            migrationBuilder.DropTable(
                name: "SupportTicketAttachments");

            migrationBuilder.DropTable(
                name: "SupportTicketHistory");

            migrationBuilder.DropTable(
                name: "SupportTicketMessages");

            migrationBuilder.DropTable(
                name: "SupportTicketNumberSequences");

            migrationBuilder.DropTable(
                name: "SupportTickets");
        }
    }
}
