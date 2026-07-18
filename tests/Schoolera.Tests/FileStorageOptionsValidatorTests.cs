using Microsoft.Extensions.Options;
using Schoolera.Infrastructure.Storage;

namespace Schoolera.Tests;

public sealed class FileStorageOptionsValidatorTests
{
    private readonly FileStorageOptionsValidator _validator = new();

    [Fact]
    public void Validate_SucceedsForSafeDefaults()
    {
        var result = _validator.Validate(null, new FileStorageOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_FailsWhenPublicPathMissingSlash()
    {
        var options = new FileStorageOptions
        {
            PublicRequestPath = "uploads",
        };

        var result = _validator.Validate(null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures!, failure => failure.Contains("PublicRequestPath", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_FailsWhenMaxSizeInvalid()
    {
        var options = new FileStorageOptions
        {
            MaxFileSizeBytes = 0,
        };

        var result = _validator.Validate(null, options);

        Assert.False(result.Succeeded);
    }
}
