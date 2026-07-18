using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Schoolera.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentsAndFinancing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinancingRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ParentUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolBranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayableItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdmissionApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    IntegrationConfigurationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Environment = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ConsentDocumentVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SelectedOfferId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProviderRequestReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancingRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentIntents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ParentUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolBranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayableItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdmissionApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    IntegrationConfigurationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Environment = table.Column<int>(type: "int", nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderCheckoutSessionId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ProviderPaymentReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RedirectUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ReturnedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SucceededAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SafeFailureCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ConsentDocumentVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentIntents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentIntentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceiptNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ParentUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Environment = table.Column<int>(type: "int", nullable: false),
                    ProviderPublicName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProviderPaymentReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    PaidAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsTaxInvoice = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentReceipts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentReconciliationRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentIntentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FinancingRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProviderCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Environment = table.Column<int>(type: "int", nullable: false),
                    MismatchType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    InternalStatus = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProviderStatus = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ExpectedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ProviderAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    DetectedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ResolvedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SafeResolutionCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    InternalNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ResolvedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentReconciliationRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentReferenceSequences",
                columns: table => new
                {
                    DayKey = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    LastValue = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentReferenceSequences", x => x.DayKey);
                });

            migrationBuilder.CreateTable(
                name: "SchoolPayableItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolBranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TuitionFeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayableFromUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PayableToUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PaymentInstructionsAr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PaymentInstructionsEn = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolPayableItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchoolPayableItems_TuitionFees_TuitionFeeId",
                        column: x => x.TuitionFeeId,
                        principalTable: "TuitionFees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinancingDecisionEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinancingRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DecisionType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SafeSummary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancingDecisionEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancingDecisionEvents_FinancingRequests_FinancingRequestId",
                        column: x => x.FinancingRequestId,
                        principalTable: "FinancingRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FinancingOffers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinancingRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderOfferReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    TenorMonths = table.Column<int>(type: "int", nullable: false),
                    PeriodicInstallment = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalRepayment = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Fees = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    InterestOrProfitRate = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    AprProviderReported = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    DownPayment = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DisclosureText = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancingOffers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancingOffers_FinancingRequests_FinancingRequestId",
                        column: x => x.FinancingRequestId,
                        principalTable: "FinancingRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentProviderEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentIntentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FinancingRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProviderEventId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Processed = table.Column<bool>(type: "bit", nullable: false),
                    SafeSummary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReceivedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentProviderEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentProviderEvents_PaymentIntents_PaymentIntentId",
                        column: x => x.PaymentIntentId,
                        principalTable: "PaymentIntents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentIntentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    ProviderTransactionId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Succeeded = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_PaymentIntents_PaymentIntentId",
                        column: x => x.PaymentIntentId,
                        principalTable: "PaymentIntents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinancingDecisionEvents_FinancingRequestId",
                table: "FinancingDecisionEvents",
                column: "FinancingRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancingOffers_FinancingRequestId",
                table: "FinancingOffers",
                column: "FinancingRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancingOffers_ProviderOfferReference",
                table: "FinancingOffers",
                column: "ProviderOfferReference");

            migrationBuilder.CreateIndex(
                name: "IX_FinancingRequests_Parent_Idempotency",
                table: "FinancingRequests",
                columns: new[] { "ParentUserId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancingRequests_ParentUserId",
                table: "FinancingRequests",
                column: "ParentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancingRequests_PayableItemId",
                table: "FinancingRequests",
                column: "PayableItemId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancingRequests_Reference",
                table: "FinancingRequests",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancingRequests_SchoolId",
                table: "FinancingRequests",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancingRequests_Status",
                table: "FinancingRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentIntents_Parent_Idempotency",
                table: "PaymentIntents",
                columns: new[] { "ParentUserId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentIntents_ParentUserId",
                table: "PaymentIntents",
                column: "ParentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentIntents_PayableItemId",
                table: "PaymentIntents",
                column: "PayableItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentIntents_ProviderCheckoutSessionId",
                table: "PaymentIntents",
                column: "ProviderCheckoutSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentIntents_Reference",
                table: "PaymentIntents",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentIntents_SchoolId",
                table: "PaymentIntents",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentIntents_Status",
                table: "PaymentIntents",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentProviderEvents_FinancingRequestId",
                table: "PaymentProviderEvents",
                column: "FinancingRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentProviderEvents_PaymentIntentId",
                table: "PaymentProviderEvents",
                column: "PaymentIntentId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentProviderEvents_ProviderEventId",
                table: "PaymentProviderEvents",
                column: "ProviderEventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReceipts_ParentUserId",
                table: "PaymentReceipts",
                column: "ParentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReceipts_PaymentIntentId",
                table: "PaymentReceipts",
                column: "PaymentIntentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReceipts_ReceiptNumber",
                table: "PaymentReceipts",
                column: "ReceiptNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReceipts_SchoolId",
                table: "PaymentReceipts",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReconciliationRecords_FinancingRequestId",
                table: "PaymentReconciliationRecords",
                column: "FinancingRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReconciliationRecords_PaymentIntentId",
                table: "PaymentReconciliationRecords",
                column: "PaymentIntentId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReconciliationRecords_Status",
                table: "PaymentReconciliationRecords",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_PaymentIntentId",
                table: "PaymentTransactions",
                column: "PaymentIntentId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_ProviderTransactionId",
                table: "PaymentTransactions",
                column: "ProviderTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolPayableItems_School_TuitionFee",
                table: "SchoolPayableItems",
                columns: new[] { "SchoolId", "TuitionFeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SchoolPayableItems_SchoolBranchId",
                table: "SchoolPayableItems",
                column: "SchoolBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolPayableItems_SchoolId",
                table: "SchoolPayableItems",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolPayableItems_TuitionFeeId",
                table: "SchoolPayableItems",
                column: "TuitionFeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FinancingDecisionEvents");

            migrationBuilder.DropTable(
                name: "FinancingOffers");

            migrationBuilder.DropTable(
                name: "PaymentProviderEvents");

            migrationBuilder.DropTable(
                name: "PaymentReceipts");

            migrationBuilder.DropTable(
                name: "PaymentReconciliationRecords");

            migrationBuilder.DropTable(
                name: "PaymentReferenceSequences");

            migrationBuilder.DropTable(
                name: "PaymentTransactions");

            migrationBuilder.DropTable(
                name: "SchoolPayableItems");

            migrationBuilder.DropTable(
                name: "FinancingRequests");

            migrationBuilder.DropTable(
                name: "PaymentIntents");
        }
    }
}
