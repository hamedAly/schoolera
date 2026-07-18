using Microsoft.EntityFrameworkCore;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Domain.Entities;
using Schoolera.Infrastructure.Persistence;

namespace Schoolera.Infrastructure.Persistence.Repositories;

public sealed class SchoolContactLeadRepository(SchooleraDbContext dbContext) : ISchoolContactLeadRepository
{
    public async Task AddAsync(SchoolContactLead lead, CancellationToken cancellationToken = default)
    {
        await dbContext.SchoolContactLeads.AddAsync(lead, cancellationToken);
    }

    public Task<bool> HasRecentDuplicateAsync(
        Guid schoolId,
        string phone,
        TimeSpan window,
        CancellationToken cancellationToken = default)
    {
        var since = DateTimeOffset.UtcNow - window;
        var normalizedPhone = phone.Trim();

        return dbContext.SchoolContactLeads
            .AsNoTracking()
            .AnyAsync(
                lead =>
                    lead.SchoolId == schoolId &&
                    lead.Phone == normalizedPhone &&
                    lead.CreatedAtUtc >= since,
                cancellationToken);
    }
}
