using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SnipLink.Application.Links;
using SnipLink.Domain.Entities;
using SnipLink.Infrastructure.Persistence;

namespace SnipLink.IntegrationTests;

public class LinkApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public LinkApiTests(CustomWebApplicationFactory factory) => _factory = factory;

    private HttpClient CreateClient() =>
        _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    [Fact]
    public async Task Create_Redirect_RecordsClick_StatsReflectIt()
    {
        var client = CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/v1/links",
            new CreateLinkRequest("https://example.com/target", null, null));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateLinkResponse>();
        created.Should().NotBeNull();
        created!.Code.Should().NotBeNullOrEmpty();

        var redirect = await client.GetAsync($"/{created.Code}");
        redirect.StatusCode.Should().Be(HttpStatusCode.Found);
        redirect.Headers.Location!.ToString().Should().Be("https://example.com/target");

        var dbClicks = await PollDbClicks(created.Code);
        dbClicks.Should().BeGreaterThanOrEqualTo(1);

        var stats = await PollStatsUntilClicks(client, created.Code, created.OwnerToken);
        stats.TotalClicks.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Redirect_UnknownCode_Returns404()
    {
        var client = CreateClient();
        var response = await client.GetAsync("/does-not-exist");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Redirect_ExpiredLink_Returns410()
    {
        var code = "expired1";
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.ShortLinks.Add(new ShortLink
            {
                Id = Guid.NewGuid(),
                Code = code,
                LongUrl = "https://example.com",
                OwnerToken = "t",
                IsActive = true,
                ExpiresAt = DateTime.UtcNow.AddMinutes(-5),
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            });
            await db.SaveChangesAsync();
        }

        var client = CreateClient();
        var response = await client.GetAsync($"/{code}");
        response.StatusCode.Should().Be(HttpStatusCode.Gone);
    }

    [Fact]
    public async Task Create_InvalidUrl_Returns400()
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/links",
            new CreateLinkRequest("not-a-url", null, null));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_WithWrongOwnerToken_Returns403()
    {
        var client = CreateClient();
        var created = await (await client.PostAsJsonAsync("/api/v1/links",
            new CreateLinkRequest("https://example.com", null, null)))
            .Content.ReadFromJsonAsync<CreateLinkResponse>();

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/links/{created!.Code}");
        request.Headers.Add("X-Owner-Token", "wrong-token");
        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_DeactivatesLink_ThenRedirectReturns410()
    {
        var client = CreateClient();
        var created = await (await client.PostAsJsonAsync("/api/v1/links",
            new CreateLinkRequest("https://example.com/deactivate", null, null)))
            .Content.ReadFromJsonAsync<CreateLinkResponse>();

        var delete = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/links/{created!.Code}");
        delete.Headers.Add("X-Owner-Token", created.OwnerToken);
        (await client.SendAsync(delete)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var redirect = await client.GetAsync($"/{created.Code}");
        redirect.StatusCode.Should().Be(HttpStatusCode.Gone);
    }

    private async Task<int> PollDbClicks(string code)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var link = db.ShortLinks.FirstOrDefault(l => l.Code == code);
            if (link is not null)
            {
                var count = db.ClickEvents.Count(c => c.ShortLinkId == link.Id);
                if (count >= 1)
                    return count;
            }
            await Task.Delay(100);
        }
        return 0;
    }

    private static async Task<StatsResponse> PollStatsUntilClicks(HttpClient client, string code, string ownerToken)
    {
        HttpStatusCode lastStatus = default;
        string lastBody = "";
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/links/{code}/stats");
            request.Headers.Add("X-Owner-Token", ownerToken);
            var response = await client.SendAsync(request);
            lastStatus = response.StatusCode;
            lastBody = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                var stats = System.Text.Json.JsonSerializer.Deserialize<StatsResponse>(lastBody,
                    new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
                if (stats is not null && stats.TotalClicks >= 1)
                    return stats;
            }
            await Task.Delay(100);
        }

        throw new Exception($"Click not reflected in stats. Last status={lastStatus}, body={lastBody}");
    }
}
