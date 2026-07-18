using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Infrastructure.Identity;

namespace Schoolera.Tests;

public sealed class IdentityErrorMapperTests
{
    private readonly IStringLocalizer<AuthMessages> _localizer =
        new SimpleAuthMessagesLocalizer();

    [Theory]
    [InlineData("PasswordRequiresNonAlphanumeric", AuthErrorCodes.PasswordRequiresNonAlphanumeric)]
    [InlineData("PasswordRequiresLower", AuthErrorCodes.PasswordRequiresLowercase)]
    [InlineData("PasswordRequiresUpper", AuthErrorCodes.PasswordRequiresUppercase)]
    [InlineData("PasswordRequiresDigit", AuthErrorCodes.PasswordRequiresDigit)]
    [InlineData("PasswordTooShort", AuthErrorCodes.PasswordTooShort)]
    [InlineData("DuplicateEmail", AuthErrorCodes.EmailAlreadyExists)]
    [InlineData("DuplicateUserName", AuthErrorCodes.EmailAlreadyExists)]
    [InlineData("InvalidEmail", AuthErrorCodes.InvalidEmail)]
    public void Map_UsesIdentityCode_NotDescription(string identityCode, string expectedErrorCode)
    {
        var mapped = IdentityErrorMapper.Map(identityCode);
        Assert.Equal(expectedErrorCode, mapped.ErrorCode);
    }

    [Fact]
    public void ToFailureResult_MapsMultiplePasswordCodes_WithoutDescriptions()
    {
        var identityErrors = new[]
        {
            new IdentityError
            {
                Code = "PasswordRequiresUpper",
                Description = "Passwords must have at least one uppercase.",
            },
            new IdentityError
            {
                Code = "PasswordRequiresNonAlphanumeric",
                Description = "Passwords must have at least one non alphanumeric character.",
            },
        };

        var result = IdentityErrorMapper.ToFailureResult<object>(identityErrors, _localizer);

        Assert.False(result.Succeeded);
        Assert.Contains(AuthErrorCodes.PasswordRequiresUppercase, result.ErrorCodes);
        Assert.Contains(AuthErrorCodes.PasswordRequiresNonAlphanumeric, result.ErrorCodes);
        Assert.DoesNotContain(
            result.Errors,
            message => message.Contains("Passwords must have", StringComparison.Ordinal));
    }

    [Fact]
    public void ToFailureResult_UnknownCode_FallsBackToValidation()
    {
        var result = IdentityErrorMapper.ToFailureResult<object>(
            [new IdentityError { Code = "UnknownThing", Description = "raw identity text" }],
            _localizer);

        Assert.Contains(ErrorCodes.Validation, result.ErrorCodes);
        Assert.DoesNotContain("raw identity text", result.Errors);
    }

    private sealed class SimpleAuthMessagesLocalizer : IStringLocalizer<AuthMessages>
    {
        public LocalizedString this[string name] => new(name, $"localized:{name}");

        public LocalizedString this[string name, params object[] arguments] => this[name];

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    }
}
