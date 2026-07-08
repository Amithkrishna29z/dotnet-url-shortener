using FluentAssertions;
using SnipLink.Application.Links;

namespace SnipLink.UnitTests;

public class ValidatorTests
{
    private readonly CreateLinkRequestValidator _validator = new();

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("http://example.com/path?q=1")]
    public void Create_AcceptsValidAbsoluteUrls(string url)
    {
        var result = _validator.Validate(new CreateLinkRequest(url, null, null));
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com")]
    [InlineData("/relative/path")]
    [InlineData("")]
    public void Create_RejectsInvalidUrls(string url)
    {
        var result = _validator.Validate(new CreateLinkRequest(url, null, null));
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("my-alias-1234")] // hyphen is not alphanumeric -> invalid (covered below)
    [InlineData("Valid123")]
    public void Create_AliasAlphanumericRule(string alias)
    {
        var result = _validator.Validate(new CreateLinkRequest("https://example.com", alias, null));
        // "my-alias-1234" contains a hyphen, so it should fail; the others pass.
        var expectValid = alias.All(char.IsLetterOrDigit);
        result.IsValid.Should().Be(expectValid);
    }

    [Theory]
    [InlineData("ab")]      // too short
    [InlineData("api")]     // reserved
    [InlineData("swagger")] // reserved
    public void Create_RejectsBadAliases(string alias)
    {
        var result = _validator.Validate(new CreateLinkRequest("https://example.com", alias, null));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Create_RejectsAliasOverMaxLength()
    {
        var alias = new string('a', AliasRules.MaxLength + 1);
        var result = _validator.Validate(new CreateLinkRequest("https://example.com", alias, null));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Create_RejectsPastExpiry()
    {
        var result = _validator.Validate(
            new CreateLinkRequest("https://example.com", null, DateTime.UtcNow.AddMinutes(-5)));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Create_AcceptsFutureExpiry()
    {
        var result = _validator.Validate(
            new CreateLinkRequest("https://example.com", null, DateTime.UtcNow.AddDays(1)));
        result.IsValid.Should().BeTrue();
    }
}
