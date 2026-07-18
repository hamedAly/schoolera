using Microsoft.EntityFrameworkCore;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;
using Schoolera.Infrastructure.Persistence;

namespace Schoolera.Infrastructure.Payments;

public sealed class PaymentReferenceGenerator(SchooleraDbContext dbContext) : IPaymentReferenceGenerator
{
    public Task<string> GeneratePaymentReferenceAsync(CancellationToken cancellationToken = default) =>
        GenerateAsync("PAY", cancellationToken);

    public Task<string> GenerateFinancingReferenceAsync(CancellationToken cancellationToken = default) =>
        GenerateAsync("FIN", cancellationToken);

    public Task<string> GenerateReceiptNumberAsync(CancellationToken cancellationToken = default) =>
        GenerateAsync("RCP", cancellationToken);

    private Task<string> GenerateAsync(string prefix, CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(async () =>
        {
            var dayKey = DateTime.UtcNow.ToString("yyyyMMdd");
            var ownsTransaction = dbContext.Database.CurrentTransaction is null;

            if (ownsTransaction)
            {
                await dbContext.Database.BeginTransactionAsync(cancellationToken);
            }

            try
            {
                var sequence = await dbContext.PaymentReferenceSequences
                    .FromSqlInterpolated(
                        $"""
                        SELECT * FROM [PaymentReferenceSequences] WITH (UPDLOCK, ROWLOCK, HOLDLOCK)
                        WHERE [DayKey] = {dayKey}
                        """)
                    .AsTracking()
                    .SingleOrDefaultAsync(cancellationToken);

                if (sequence is null)
                {
                    sequence = new PaymentReferenceSequence(dayKey);
                    dbContext.PaymentReferenceSequences.Add(sequence);
                    await dbContext.SaveChangesAsync(cancellationToken);
                }

                var next = sequence.Next();

                if (ownsTransaction)
                {
                    await dbContext.SaveChangesAsync(cancellationToken);
                    await dbContext.Database.CommitTransactionAsync(cancellationToken);
                }

                return $"{prefix}-{dayKey}-{next:D5}";
            }
            catch
            {
                if (ownsTransaction && dbContext.Database.CurrentTransaction is not null)
                {
                    await dbContext.Database.RollbackTransactionAsync(cancellationToken);
                }

                throw;
            }
        });
    }
}

public sealed class PaymentRepository(SchooleraDbContext dbContext) : IPaymentRepository
{
    private static readonly PaymentIntentStatus[] ActiveStatuses =
    [
        PaymentIntentStatus.Created,
        PaymentIntentStatus.PendingProvider,
        PaymentIntentStatus.RequiresParentAction,
        PaymentIntentStatus.Processing,
    ];

    public Task<SchoolPayableItem?> GetPayableItemAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.SchoolPayableItems
            .Include(item => item.TuitionFee)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public Task<SchoolPayableItem?> GetPayableItemBySchoolAndFeeAsync(
        Guid schoolId,
        Guid tuitionFeeId,
        CancellationToken cancellationToken = default) =>
        dbContext.SchoolPayableItems
            .Include(item => item.TuitionFee)
            .FirstOrDefaultAsync(
                item => item.SchoolId == schoolId && item.TuitionFeeId == tuitionFeeId,
                cancellationToken);

    public async Task<IReadOnlyList<SchoolPayableItem>> ListPayableItemsForSchoolAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default) =>
        await dbContext.SchoolPayableItems
            .Include(item => item.TuitionFee)
            .Where(item => item.SchoolId == schoolId)
            .OrderByDescending(item => item.UpdatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SchoolPayableItem>> ListActivePayableItemsAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.SchoolPayableItems
            .Include(item => item.TuitionFee)
            .Where(item => item.IsActive)
            .ToListAsync(cancellationToken);

    public async Task AddPayableItemAsync(SchoolPayableItem item, CancellationToken cancellationToken = default) =>
        await dbContext.SchoolPayableItems.AddAsync(item, cancellationToken);

    public Task<PaymentIntent?> GetIntentByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.PaymentIntents
            .Include(intent => intent.Transactions)
            .Include(intent => intent.Events)
            .FirstOrDefaultAsync(intent => intent.Id == id, cancellationToken);

    public Task<PaymentIntent?> GetIntentByReferenceAsync(
        string reference,
        CancellationToken cancellationToken = default) =>
        dbContext.PaymentIntents
            .Include(intent => intent.Transactions)
            .FirstOrDefaultAsync(intent => intent.Reference == reference, cancellationToken);

    public Task<PaymentIntent?> GetIntentByIdempotencyAsync(
        Guid parentUserId,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        dbContext.PaymentIntents.FirstOrDefaultAsync(
            intent => intent.ParentUserId == parentUserId && intent.IdempotencyKey == idempotencyKey,
            cancellationToken);

    public Task<PaymentIntent?> GetIntentByCheckoutSessionAsync(
        string checkoutSessionId,
        CancellationToken cancellationToken = default) =>
        dbContext.PaymentIntents.FirstOrDefaultAsync(
            intent => intent.ProviderCheckoutSessionId == checkoutSessionId,
            cancellationToken);

    public async Task<IReadOnlyList<PaymentIntent>> ListIntentsForParentAsync(
        Guid parentUserId,
        CancellationToken cancellationToken = default) =>
        await dbContext.PaymentIntents
            .AsNoTracking()
            .Where(intent => intent.ParentUserId == parentUserId)
            .OrderByDescending(intent => intent.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PaymentIntent>> ListIntentsForSchoolAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default) =>
        await dbContext.PaymentIntents
            .AsNoTracking()
            .Where(intent => intent.SchoolId == schoolId)
            .OrderByDescending(intent => intent.CreatedAtUtc)
            .Take(200)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PaymentIntent>> ListIntentsForAdminAsync(
        PaymentIntentStatus? status,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.PaymentIntents.AsNoTracking().AsQueryable();
        if (status is { } s)
        {
            query = query.Where(intent => intent.Status == s);
        }

        return await query
            .OrderByDescending(intent => intent.CreatedAtUtc)
            .Take(Math.Clamp(take, 1, 500))
            .ToListAsync(cancellationToken);
    }

    public async Task AddIntentAsync(PaymentIntent intent, CancellationToken cancellationToken = default) =>
        await dbContext.PaymentIntents.AddAsync(intent, cancellationToken);

    public Task<bool> HasSucceededPaymentForPayableAsync(
        Guid payableItemId,
        Guid parentUserId,
        CancellationToken cancellationToken = default) =>
        dbContext.PaymentIntents.AnyAsync(
            intent => intent.PayableItemId == payableItemId &&
                      intent.ParentUserId == parentUserId &&
                      (intent.Status == PaymentIntentStatus.Succeeded ||
                       intent.Status == PaymentIntentStatus.PartiallyRefunded ||
                       intent.Status == PaymentIntentStatus.Refunded),
            cancellationToken);

    public Task<bool> HasActiveIntentForPayableAsync(
        Guid payableItemId,
        Guid parentUserId,
        CancellationToken cancellationToken = default) =>
        dbContext.PaymentIntents.AnyAsync(
            intent => intent.PayableItemId == payableItemId &&
                      intent.ParentUserId == parentUserId &&
                      ActiveStatuses.Contains(intent.Status),
            cancellationToken);

    public Task<PaymentProviderEvent?> GetProviderEventAsync(
        string providerEventId,
        CancellationToken cancellationToken = default) =>
        dbContext.PaymentProviderEvents.FirstOrDefaultAsync(
            evt => evt.ProviderEventId == providerEventId,
            cancellationToken);

    public async Task AddProviderEventAsync(
        PaymentProviderEvent evt,
        CancellationToken cancellationToken = default) =>
        await dbContext.PaymentProviderEvents.AddAsync(evt, cancellationToken);

    public async Task AddReceiptAsync(PaymentReceipt receipt, CancellationToken cancellationToken = default) =>
        await dbContext.PaymentReceipts.AddAsync(receipt, cancellationToken);

    public Task<PaymentReceipt?> GetReceiptByIntentIdAsync(
        Guid paymentIntentId,
        CancellationToken cancellationToken = default) =>
        dbContext.PaymentReceipts.FirstOrDefaultAsync(
            receipt => receipt.PaymentIntentId == paymentIntentId,
            cancellationToken);

    public Task<PaymentReceipt?> GetReceiptByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.PaymentReceipts.FirstOrDefaultAsync(receipt => receipt.Id == id, cancellationToken);

    public async Task AddReconciliationAsync(
        PaymentReconciliationRecord record,
        CancellationToken cancellationToken = default) =>
        await dbContext.PaymentReconciliationRecords.AddAsync(record, cancellationToken);

    public Task<PaymentReconciliationRecord?> GetReconciliationAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        dbContext.PaymentReconciliationRecords.FirstOrDefaultAsync(
            record => record.Id == id,
            cancellationToken);

    public async Task<IReadOnlyList<PaymentReconciliationRecord>> ListReconciliationAsync(
        PaymentReconciliationStatus? status,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.PaymentReconciliationRecords.AsNoTracking().AsQueryable();
        if (status is { } s)
        {
            query = query.Where(record => record.Status == s);
        }

        return await query
            .OrderByDescending(record => record.DetectedAtUtc)
            .Take(200)
            .ToListAsync(cancellationToken);
    }

    public Task<FinancingRequest?> GetFinancingByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        dbContext.FinancingRequests
            .Include(request => request.Offers)
            .Include(request => request.Decisions)
            .FirstOrDefaultAsync(request => request.Id == id, cancellationToken);

    public Task<FinancingRequest?> GetFinancingByReferenceAsync(
        string reference,
        CancellationToken cancellationToken = default) =>
        dbContext.FinancingRequests
            .Include(request => request.Offers)
            .FirstOrDefaultAsync(request => request.Reference == reference, cancellationToken);

    public Task<FinancingRequest?> GetFinancingByIdempotencyAsync(
        Guid parentUserId,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        dbContext.FinancingRequests.FirstOrDefaultAsync(
            request => request.ParentUserId == parentUserId && request.IdempotencyKey == idempotencyKey,
            cancellationToken);

    public async Task<IReadOnlyList<FinancingRequest>> ListFinancingForParentAsync(
        Guid parentUserId,
        CancellationToken cancellationToken = default) =>
        await dbContext.FinancingRequests
            .AsNoTracking()
            .Include(request => request.Offers)
            .Where(request => request.ParentUserId == parentUserId)
            .OrderByDescending(request => request.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task AddFinancingAsync(
        FinancingRequest request,
        CancellationToken cancellationToken = default) =>
        await dbContext.FinancingRequests.AddAsync(request, cancellationToken);

    public async Task<decimal> SumSuccessfulRefundsAsync(
        Guid paymentIntentId,
        CancellationToken cancellationToken = default) =>
        await dbContext.PaymentTransactions
            .Where(tx => tx.PaymentIntentId == paymentIntentId &&
                         tx.Type == PaymentTransactionType.Refund &&
                         tx.Succeeded)
            .SumAsync(tx => (decimal?)tx.Amount, cancellationToken) ?? 0m;
}
