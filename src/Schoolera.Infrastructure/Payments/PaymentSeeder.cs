using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;
using Schoolera.Infrastructure.Persistence;

namespace Schoolera.Infrastructure.Payments;

/// <summary>
/// Idempotent Development seed for Sandbox Payment/Financing integrations.
/// Does not overwrite Platform Admin-edited Payment/Financing configurations.
/// Never seeds real provider credentials or real financial approvals.
/// </summary>
public sealed class PaymentSeeder(
    SchooleraDbContext dbContext,
    ILogger<PaymentSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Running payment / financing sandbox seed...");
        await EnsureSandboxIntegrationAsync(
            IntegrationType.Payment,
            "مدفوعات Sandbox (تطوير)",
            "Sandbox Payments (Development)",
            """
            {
              "environment": "Sandbox",
              "webhookSecret": "sandbox-dev-webhook-secret-not-real",
              "supportedCurrencies": ["EGP"],
              "supportedPaymentMethods": ["ProviderHostedCheckout"],
              "minimumAmount": 0,
              "maximumAmount": 100000,
              "displayProviderFees": false,
              "requestTimeoutSeconds": 30,
              "sandboxDefaultOutcome": "success"
            }
            """,
            cancellationToken);

        await EnsureSandboxIntegrationAsync(
            IntegrationType.Financing,
            "تمويل Sandbox (تطوير)",
            "Sandbox Financing (Development)",
            """
            {
              "environment": "Sandbox",
              "webhookSecret": "sandbox-dev-webhook-secret-not-real",
              "supportedCurrencies": ["EGP"],
              "supportedFinancingMethods": ["Installments"],
              "supportedTenorsMonths": [3, 6, 12],
              "minimumAmount": 0,
              "maximumAmount": 100000,
              "displayProviderFees": true,
              "requestTimeoutSeconds": 30,
              "sandboxDefaultOutcome": "offers"
            }
            """,
            cancellationToken);

        await EnsureSamplePayableItemAsync(cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Payment / financing sandbox seed completed.");
    }

    private async Task EnsureSandboxIntegrationAsync(
        IntegrationType type,
        string displayNameAr,
        string displayNameEn,
        string settingsJson,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.PlatformIntegrationConfigurations
            .AnyAsync(item => item.IntegrationType == type, cancellationToken);
        if (exists)
        {
            return;
        }

        var entity = new PlatformIntegrationConfiguration(
            type,
            IntegrationProviderCodes.SchooleraSandbox,
            displayNameAr,
            displayNameEn,
            settingsJson,
            settingsSchemaVersion: 1,
            sortOrder: 0);
        // Seeded disabled — Platform Admin must activate deliberately.
        await dbContext.PlatformIntegrationConfigurations.AddAsync(entity, cancellationToken);
    }

    private async Task EnsureSamplePayableItemAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.SchoolPayableItems.AnyAsync(cancellationToken))
        {
            return;
        }

        var fee = await dbContext.TuitionFees
            .AsNoTracking()
            .Include(item => item.SchoolBranch)
            .Where(item => item.IsActive && item.IsPublished && item.Amount > 0)
            .OrderBy(item => item.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (fee?.SchoolBranch is null)
        {
            return;
        }

        var payable = new SchoolPayableItem(
            fee.SchoolBranch.SchoolId,
            fee.SchoolBranchId,
            fee.Id,
            payableFromUtc: null,
            payableToUtc: null,
            paymentInstructionsAr: "عنصر مستحق تجريبي — Sandbox فقط.",
            paymentInstructionsEn: "Sample payable item — Sandbox only.");

        await dbContext.SchoolPayableItems.AddAsync(payable, cancellationToken);
    }
}
