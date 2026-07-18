using Schoolera.Domain.Entities;

namespace Schoolera.Tests;

public sealed class SlugHelperTests
{
    [Theory]
    [InlineData("Cairo International School", "cairo-international-school")]
    [InlineData("  Mixed   Spaces  ", "mixed-spaces")]
    [InlineData("STEM.Academy", "stem-academy")]
    public void FromLatinText_NormalizesLatinValues(string input, string expected)
    {
        var slug = SlugHelper.FromLatinText(input);
        Assert.Equal(expected, slug);
    }

    [Fact]
    public void Normalize_RejectsEmptyInput()
    {
        Assert.Throws<ArgumentException>(() => SlugHelper.Normalize("   "));
    }

    [Fact]
    public void Normalize_RejectsPathTraversal()
    {
        Assert.Throws<ArgumentException>(() => SlugHelper.Normalize("../school"));
    }

    [Fact]
    public void Normalize_ArabicInputProducesStableSlug()
    {
        var slug = SlugHelper.Normalize("مدرسة النيل");
        Assert.StartsWith("item-", slug, StringComparison.Ordinal);
        Assert.True(SlugHelper.IsValidSlug(slug));
    }
}
