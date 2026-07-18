using Schoolera.Application.Schools.Map;

namespace Schoolera.Application.Common.Interfaces;

public interface ISchoolMapPinReadRepository
{
    Task<(IReadOnlyList<PublicSchoolMapPinProjection> Pins, bool IsTruncated)> SearchMapPinsAsync(
        PublicSchoolMapPinsFilter filter,
        CancellationToken cancellationToken = default);
}
