namespace Schoolera.Application.Common.Interfaces;

/// <summary>
/// Best-effort in-process enqueue for daily school profile view aggregates.
/// Dropped when the bounded channel is full; never fails the profile HTTP response.
/// </summary>
public interface ISchoolProfileViewQueue
{
    bool TryEnqueue(Guid schoolId, DateTimeOffset viewedAtUtc);
}

public sealed record SchoolProfileViewEvent(Guid SchoolId, DateTimeOffset ViewedAtUtc);
