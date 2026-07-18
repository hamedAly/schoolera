using Schoolera.Domain.Entities;

namespace Schoolera.Application.Common.Interfaces;

public interface ISchoolContactLeadRepository
{
    Task AddAsync(SchoolContactLead lead, CancellationToken cancellationToken = default);

    Task<bool> HasRecentDuplicateAsync(
        Guid schoolId,
        string phone,
        TimeSpan window,
        CancellationToken cancellationToken = default);
}
