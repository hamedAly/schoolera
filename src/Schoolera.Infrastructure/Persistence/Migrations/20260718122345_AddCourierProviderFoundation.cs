using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Schoolera.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCourierProviderFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CourierHealthCheckRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntegrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SafeCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CheckedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    IsOperational = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourierHealthCheckRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourierHealthCheckRecords_PlatformIntegrationConfigurations_IntegrationId",
                        column: x => x.IntegrationId,
                        principalTable: "PlatformIntegrationConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CourierProviderProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntegrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DescriptionAr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DescriptionEn = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    LogoReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TermsUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PrivacyUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ConfigurationVersion = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourierProviderProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourierProviderProfiles_PlatformIntegrationConfigurations_IntegrationId",
                        column: x => x.IntegrationId,
                        principalTable: "PlatformIntegrationConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CourierServices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntegrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceType = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DescriptionAr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DescriptionEn = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    MinimumPickupLeadTimeMinutes = table.Column<int>(type: "int", nullable: false),
                    DailyCutoffLocalTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    MaximumFuturePickupDays = table.Column<int>(type: "int", nullable: false),
                    AcceptanceWindowMinutes = table.Column<int>(type: "int", nullable: false),
                    SupportsScheduledPickup = table.Column<bool>(type: "bit", nullable: false),
                    SupportsSameDayPickup = table.Column<bool>(type: "bit", nullable: false),
                    CanCreatePickup = table.Column<bool>(type: "bit", nullable: false),
                    CanQueryStatus = table.Column<bool>(type: "bit", nullable: false),
                    SupportsWebhook = table.Column<bool>(type: "bit", nullable: false),
                    SupportsPolling = table.Column<bool>(type: "bit", nullable: false),
                    SupportsManualUpdates = table.Column<bool>(type: "bit", nullable: false),
                    SupportsCancellationBeforePickup = table.Column<bool>(type: "bit", nullable: false),
                    SupportsCourierAssignment = table.Column<bool>(type: "bit", nullable: false),
                    SupportsProofOfPickup = table.Column<bool>(type: "bit", nullable: false),
                    SupportsProofOfDelivery = table.Column<bool>(type: "bit", nullable: false),
                    SupportsDropOffPoint = table.Column<bool>(type: "bit", nullable: false),
                    MaximumEnvelopeWeightGrams = table.Column<int>(type: "int", nullable: false),
                    MaximumEnvelopeLengthCm = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    MaximumEnvelopeWidthCm = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    MaximumEnvelopeHeightCm = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourierServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourierServices_PlatformIntegrationConfigurations_IntegrationId",
                        column: x => x.IntegrationId,
                        principalTable: "PlatformIntegrationConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CourierCoverageRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntegrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CountryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GovernorateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DistrictId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Result = table.Column<int>(type: "int", nullable: false),
                    ScopeKey = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    NotesAr = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NotesEn = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourierCoverageRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourierCoverageRules_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourierCoverageRules_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourierCoverageRules_CourierServices_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "CourierServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourierCoverageRules_Districts_DistrictId",
                        column: x => x.DistrictId,
                        principalTable: "Districts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourierCoverageRules_Governorates_GovernorateId",
                        column: x => x.GovernorateId,
                        principalTable: "Governorates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourierCoverageRules_PlatformIntegrationConfigurations_IntegrationId",
                        column: x => x.IntegrationId,
                        principalTable: "PlatformIntegrationConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CourierOperatingWindows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntegrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CoverageRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ScopeKey = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    TimeZoneId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    LocalStartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    LocalEndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourierOperatingWindows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourierOperatingWindows_CourierCoverageRules_CoverageRuleId",
                        column: x => x.CoverageRuleId,
                        principalTable: "CourierCoverageRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourierOperatingWindows_CourierServices_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "CourierServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourierOperatingWindows_PlatformIntegrationConfigurations_IntegrationId",
                        column: x => x.IntegrationId,
                        principalTable: "PlatformIntegrationConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CourierSlaDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntegrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CoverageRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ScopeKey = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    AcceptanceTargetMinutes = table.Column<int>(type: "int", nullable: false),
                    PickupSchedulingTargetMinutes = table.Column<int>(type: "int", nullable: false),
                    PickupCompletionTargetMinutes = table.Column<int>(type: "int", nullable: false),
                    DeliveryToSchoolTargetMinutes = table.Column<int>(type: "int", nullable: false),
                    ReceiptConfirmationTargetMinutes = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourierSlaDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourierSlaDefinitions_CourierCoverageRules_CoverageRuleId",
                        column: x => x.CoverageRuleId,
                        principalTable: "CourierCoverageRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourierSlaDefinitions_CourierServices_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "CourierServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourierSlaDefinitions_PlatformIntegrationConfigurations_IntegrationId",
                        column: x => x.IntegrationId,
                        principalTable: "PlatformIntegrationConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourierCoverageRules_ActiveScope",
                table: "CourierCoverageRules",
                columns: new[] { "IntegrationId", "ServiceId", "ScopeKey" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_CourierCoverageRules_CityId",
                table: "CourierCoverageRules",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_CourierCoverageRules_CountryId",
                table: "CourierCoverageRules",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_CourierCoverageRules_DistrictId",
                table: "CourierCoverageRules",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_CourierCoverageRules_GovernorateId",
                table: "CourierCoverageRules",
                column: "GovernorateId");

            migrationBuilder.CreateIndex(
                name: "IX_CourierCoverageRules_IntegrationId_IsActive_Result",
                table: "CourierCoverageRules",
                columns: new[] { "IntegrationId", "IsActive", "Result" });

            migrationBuilder.CreateIndex(
                name: "IX_CourierCoverageRules_ServiceId",
                table: "CourierCoverageRules",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_CourierHealthCheckRecords_IntegrationId_CheckedAtUtc",
                table: "CourierHealthCheckRecords",
                columns: new[] { "IntegrationId", "CheckedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CourierHealthCheckRecords_Status_CheckedAtUtc",
                table: "CourierHealthCheckRecords",
                columns: new[] { "Status", "CheckedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CourierOperatingWindows_CoverageRuleId",
                table: "CourierOperatingWindows",
                column: "CoverageRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_CourierOperatingWindows_ExactWindow",
                table: "CourierOperatingWindows",
                columns: new[] { "IntegrationId", "ServiceId", "ScopeKey", "DayOfWeek", "LocalStartTime", "LocalEndTime" },
                unique: true,
                filter: "[ServiceId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CourierOperatingWindows_IntegrationId_ServiceId_ScopeKey_IsActive_DayOfWeek",
                table: "CourierOperatingWindows",
                columns: new[] { "IntegrationId", "ServiceId", "ScopeKey", "IsActive", "DayOfWeek" });

            migrationBuilder.CreateIndex(
                name: "IX_CourierOperatingWindows_ServiceId",
                table: "CourierOperatingWindows",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_CourierProviderProfiles_IntegrationId",
                table: "CourierProviderProfiles",
                column: "IntegrationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourierServices_Integration_Code",
                table: "CourierServices",
                columns: new[] { "IntegrationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourierServices_IntegrationId_IsActive_SortOrder",
                table: "CourierServices",
                columns: new[] { "IntegrationId", "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_CourierSlaDefinitions_ActiveScope",
                table: "CourierSlaDefinitions",
                columns: new[] { "IntegrationId", "ServiceId", "ScopeKey" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_CourierSlaDefinitions_CoverageRuleId",
                table: "CourierSlaDefinitions",
                column: "CoverageRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_CourierSlaDefinitions_IntegrationId_IsActive",
                table: "CourierSlaDefinitions",
                columns: new[] { "IntegrationId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CourierSlaDefinitions_ServiceId",
                table: "CourierSlaDefinitions",
                column: "ServiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourierHealthCheckRecords");

            migrationBuilder.DropTable(
                name: "CourierOperatingWindows");

            migrationBuilder.DropTable(
                name: "CourierProviderProfiles");

            migrationBuilder.DropTable(
                name: "CourierSlaDefinitions");

            migrationBuilder.DropTable(
                name: "CourierCoverageRules");

            migrationBuilder.DropTable(
                name: "CourierServices");
        }
    }
}
