using Schoolera.Application.SchoolOnboarding.Common;
using Schoolera.Application.SchoolOnboarding.Constants;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Tests;

public sealed class OnboardingCompletenessTests
{
    private static SchoolOnboardingApplication BuildComplete()
    {
        var application = new SchoolOnboardingApplication(Guid.NewGuid());
        application.SaveOrganization("مؤسسة", "Org", null, "EG", "REG-1", null, null, null, null);
        application.SaveAuthorizedRepresentative(
            "ممثل", "Rep", "ID-1", "مدير", "Manager", "rep@example.com", "+201000000000");
        application.SaveSchoolDetails(
            "مدرسة", "School", SchoolType.International, GenderType.Mixed, 2010, null, null, null, null);
        application.SavePrimaryBranch(
            Guid.NewGuid(), Guid.NewGuid(), "عنوان", null, null, null, null, null, null, null, null,
            "+201111111111", null, null);
        return application;
    }

    [Fact]
    public void CompleteApplication_HasNoSubmissionErrors()
    {
        var application = BuildComplete();
        var requiredDocType = Guid.NewGuid();

        var codes = OnboardingCompleteness.GetSubmissionErrorCodes(
            application,
            requiredDocumentTypeIds: [requiredDocType],
            currentDocumentTypeIds: [requiredDocType],
            districtBelongsToCity: true);

        Assert.Empty(codes);
    }

    [Fact]
    public void MissingRequiredFields_ReturnsIncomplete()
    {
        var application = new SchoolOnboardingApplication(Guid.NewGuid());

        var codes = OnboardingCompleteness.GetSubmissionErrorCodes(
            application, [], [], districtBelongsToCity: true);

        Assert.Contains(OnboardingErrorCodes.Incomplete, codes);
    }

    [Fact]
    public void MissingRequiredDocument_ReturnsRequiredDocumentMissing()
    {
        var application = BuildComplete();
        var requiredDocType = Guid.NewGuid();

        var codes = OnboardingCompleteness.GetSubmissionErrorCodes(
            application, [requiredDocType], [], districtBelongsToCity: true);

        Assert.Contains(OnboardingErrorCodes.RequiredDocumentMissing, codes);
    }

    [Fact]
    public void DistrictNotInCity_ReturnsCityDistrictMismatch()
    {
        var application = BuildComplete();

        var codes = OnboardingCompleteness.GetSubmissionErrorCodes(
            application, [], [], districtBelongsToCity: false);

        Assert.Contains(OnboardingErrorCodes.CityDistrictMismatch, codes);
    }

    [Theory]
    [InlineData(1700)]
    [InlineData(3000)]
    public void InvalidFoundedYear_ReturnsIncomplete(int year)
    {
        var application = BuildComplete();
        application.SaveSchoolDetails(
            "مدرسة", "School", SchoolType.International, GenderType.Mixed, year, null, null, null, null);

        var codes = OnboardingCompleteness.GetSubmissionErrorCodes(
            application, [], [], districtBelongsToCity: true);

        Assert.Contains(OnboardingErrorCodes.Incomplete, codes);
    }

    [Fact]
    public void OutOfRangeCoordinates_ReturnsIncomplete()
    {
        var application = BuildComplete();
        application.SavePrimaryBranch(
            application.CityId, application.DistrictId, "عنوان", null, null, null, null, null, null,
            latitude: 200m, longitude: 400m, "+201111111111", null, null);

        var codes = OnboardingCompleteness.GetSubmissionErrorCodes(
            application, [], [], districtBelongsToCity: true);

        Assert.Contains(OnboardingErrorCodes.Incomplete, codes);
    }

    [Theory]
    [InlineData("not-an-email", false)]
    [InlineData("valid@example.com", true)]
    public void IsValidEmail_ValidatesFormat(string value, bool expected)
    {
        Assert.Equal(expected, OnboardingCompleteness.IsValidEmail(value));
    }
}
