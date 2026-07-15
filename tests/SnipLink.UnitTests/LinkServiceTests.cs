using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using SnipLink.Application;
using SnipLink.Application.Abstractions;
using SnipLink.Application.Links;
using SnipLink.Domain.Entities;

namespace SnipLink.UnitTests;

public class LinkServiceTests
{
    private static readonly DateTime Now = new(2026, 6, 14, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IShortLinkRepository> _repo = new();
    private readonly Mock<IAnalyticsRepository> _analytics = new();
    private readonly Mock<ILinkCache> _cache = new();
    private readonly SnipLinkOptions _options = new() { BaseUrl = "https://snip.test", CodeLength = 6 };

    private LinkService CreateSut() =>
        new(_repo.Object, _analytics.Object, _cache.Object, new FixedClock(Now), Options.Create(_options));

    private sealed class FixedClock(DateTime now) : IClock
    {
        public DateTime UtcNow { get; } = now;
    }

    [Fact]
    public async Task Create_GeneratesCodeOwnerTokenAndShortUrl()
    {
        _repo.Setup(r => r.CodeExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var sut = CreateSut();

        var result = await sut.CreateAsync(new CreateLinkRequest("https://example.com", null, null));

        result.Code.Should().HaveLength(6);
        result.OwnerToken.Should().NotBeNullOrEmpty();
        result.ShortUrl.Should().Be($"https://snip.test/{result.Code}");
        _repo.Verify(r => r.AddAsync(It.Is<ShortLink>(l => l.LongUrl == "https://example.com"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_RetriesOnCodeCollision()
    {
        _repo.SetupSequence(r => r.CodeExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        var sut = CreateSut();

        var result = await sut.CreateAsync(new CreateLinkRequest("https://example.com", null, null));

        result.Code.Should().NotBeNullOrEmpty();
        _repo.Verify(r => r.CodeExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Create_WithTakenAlias_Throws()
    {
        _repo.Setup(r => r.CodeExistsAsync("taken", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = CreateSut();

        var act = () => sut.CreateAsync(new CreateLinkRequest("https://example.com", "taken", null));

        await act.Should().ThrowAsync<AliasConflictException>();
    }

    [Fact]
    public async Task Create_UsesSuppliedOwnerToken()
    {
        _repo.Setup(r => r.CodeExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var sut = CreateSut();

        var result = await sut.CreateAsync(new CreateLinkRequest("https://example.com", null, null), ownerToken: "browser-token");

        result.OwnerToken.Should().Be("browser-token");
    }

    [Fact]
    public async Task Resolve_CacheHit_DoesNotTouchDatabase()
    {
        _cache.Setup(c => c.GetAsync("abc", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CachedLink(Guid.NewGuid(), "https://cached.example", null));
        var sut = CreateSut();

        var result = await sut.ResolveForRedirectAsync("abc");

        result.Status.Should().Be(RedirectStatus.Found);
        result.LongUrl.Should().Be("https://cached.example");
        _repo.Verify(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Resolve_CacheMiss_FallsBackToDatabaseAndCaches()
    {
        _cache.Setup(c => c.GetAsync("abc", It.IsAny<CancellationToken>())).ReturnsAsync((CachedLink?)null);
        var link = new ShortLink { Id = Guid.NewGuid(), Code = "abc", LongUrl = "https://db.example", IsActive = true };
        _repo.Setup(r => r.GetByCodeAsync("abc", It.IsAny<CancellationToken>())).ReturnsAsync(link);
        var sut = CreateSut();

        var result = await sut.ResolveForRedirectAsync("abc");

        result.Status.Should().Be(RedirectStatus.Found);
        result.LongUrl.Should().Be("https://db.example");
        _cache.Verify(c => c.SetAsync("abc", It.IsAny<CachedLink>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Resolve_UnknownCode_ReturnsNotFound()
    {
        _cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((CachedLink?)null);
        _repo.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((ShortLink?)null);
        var sut = CreateSut();

        var result = await sut.ResolveForRedirectAsync("missing");

        result.Status.Should().Be(RedirectStatus.NotFound);
    }

    [Fact]
    public async Task Resolve_ExpiredLink_ReturnsGoneAndIsNotCached()
    {
        _cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((CachedLink?)null);
        var link = new ShortLink { Id = Guid.NewGuid(), Code = "old", LongUrl = "https://x", IsActive = true, ExpiresAt = Now.AddMinutes(-1) };
        _repo.Setup(r => r.GetByCodeAsync("old", It.IsAny<CancellationToken>())).ReturnsAsync(link);
        var sut = CreateSut();

        var result = await sut.ResolveForRedirectAsync("old");

        result.Status.Should().Be(RedirectStatus.Gone);
        _cache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<CachedLink>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Resolve_InactiveLink_ReturnsGone()
    {
        _cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((CachedLink?)null);
        var link = new ShortLink { Id = Guid.NewGuid(), Code = "off", LongUrl = "https://x", IsActive = false };
        _repo.Setup(r => r.GetByCodeAsync("off", It.IsAny<CancellationToken>())).ReturnsAsync(link);
        var sut = CreateSut();

        var result = await sut.ResolveForRedirectAsync("off");

        result.Status.Should().Be(RedirectStatus.Gone);
    }

    [Fact]
    public async Task Resolve_CachedButExpired_ReturnsGoneAndEvicts()
    {
        _cache.Setup(c => c.GetAsync("stale", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CachedLink(Guid.NewGuid(), "https://x", Now.AddMinutes(-1)));
        var sut = CreateSut();

        var result = await sut.ResolveForRedirectAsync("stale");

        result.Status.Should().Be(RedirectStatus.Gone);
        _cache.Verify(c => c.RemoveAsync("stale", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Get_WithWrongOwnerToken_Throws()
    {
        var link = new ShortLink { Id = Guid.NewGuid(), Code = "abc", OwnerToken = "right" };
        _repo.Setup(r => r.GetByCodeAsync("abc", It.IsAny<CancellationToken>())).ReturnsAsync(link);
        var sut = CreateSut();

        var act = () => sut.GetAsync("abc", "wrong");

        await act.Should().ThrowAsync<OwnerTokenMismatchException>();
    }

    [Fact]
    public async Task Get_WithUnknownCode_Throws()
    {
        _repo.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((ShortLink?)null);
        var sut = CreateSut();

        var act = () => sut.GetAsync("nope", "token");

        await act.Should().ThrowAsync<LinkNotFoundException>();
    }

    [Fact]
    public async Task Deactivate_SetsInactiveAndEvictsCache()
    {
        var link = new ShortLink { Id = Guid.NewGuid(), Code = "abc", OwnerToken = "t", IsActive = true };
        _repo.Setup(r => r.GetByCodeAsync("abc", It.IsAny<CancellationToken>())).ReturnsAsync(link);
        var sut = CreateSut();

        await sut.DeactivateAsync("abc", "t");

        link.IsActive.Should().BeFalse();
        _repo.Verify(r => r.UpdateAsync(link, It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveAsync("abc", It.IsAny<CancellationToken>()), Times.Once);
    }
}
