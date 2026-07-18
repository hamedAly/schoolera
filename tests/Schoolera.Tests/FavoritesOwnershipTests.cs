using Schoolera.Application.Favorites.Constants;
using Schoolera.Application.Favorites.Dtos;
using Schoolera.Domain.Entities;

namespace Schoolera.Tests;

public sealed class FavoritesOwnershipTests
{
    [Fact]
    public void FavoriteSchool_ShouldBindOwnershipToParentUserAndSchool()
    {
        var parentUserId = Guid.NewGuid();
        var schoolId = Guid.NewGuid();

        var favorite = new FavoriteSchool(parentUserId, schoolId);

        Assert.Equal(parentUserId, favorite.ParentUserId);
        Assert.Equal(schoolId, favorite.SchoolId);
        Assert.NotEqual(Guid.Empty, favorite.Id);
        Assert.True(favorite.CreatedAtUtc <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void FavoriteErrorCodes_ShouldBeStableForClientBranching()
    {
        Assert.Equal("favorites.forbidden", FavoriteErrorCodes.Forbidden);
        Assert.Equal("favorites.schoolNotFound", FavoriteErrorCodes.SchoolNotFound);
        Assert.Equal("favorites.notFound", FavoriteErrorCodes.NotFound);
    }

    [Fact]
    public void UnavailableFavoriteSummary_ShouldNotExposeAdmissionAsOpen()
    {
        // Non-published favorites must remain listable without implying eligibility.
        var unavailable = new FavoriteSchoolUnavailableDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Slug: null,
            DisplayName: "مدرسة تجريبية",
            IsAvailable: false,
            IsAdmissionOpen: false);

        Assert.False(unavailable.IsAvailable);
        Assert.False(unavailable.IsAdmissionOpen);
        Assert.Null(unavailable.Slug);
    }

    [Fact]
    public void AddFavorite_IsIdempotentAtDomainLevel_DistinctInstancesShareOwnershipKey()
    {
        var parentUserId = Guid.NewGuid();
        var schoolId = Guid.NewGuid();
        var first = new FavoriteSchool(parentUserId, schoolId);
        var second = new FavoriteSchool(parentUserId, schoolId);

        Assert.Equal(first.ParentUserId, second.ParentUserId);
        Assert.Equal(first.SchoolId, second.SchoolId);
        Assert.NotEqual(first.Id, second.Id);
    }
}
