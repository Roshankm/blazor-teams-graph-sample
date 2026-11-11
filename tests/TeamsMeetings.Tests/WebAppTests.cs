using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc.Testing;

using TeamsMeetings.Core;

namespace TeamsMeetings.Tests;

/// <summary>Runs the real app in demo mode (no Entra ID settings) through the full HTTP pipeline.</summary>
public class WebAppTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Theory]
    [InlineData("/")]
    [InlineData("/meetings/new")]
    [InlineData("/meetings/find")]
    [InlineData("/meetings/demo-weekly-sync")]
    [InlineData("/health")]
    public async Task Pages_render(string url)
    {
        var response = await factory.CreateClient().GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Api_creates_a_meeting_and_finds_it_by_join_link()
    {
        var client = factory.CreateClient();
        var create = await client.PostAsJsonAsync("/api/meetings", new CreateMeetingRequest
        {
            Subject = "API test",
            StartLocal = DateTime.UtcNow.AddDays(2),
            DurationMinutes = 30,
            TimeZoneId = "UTC",
        });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var meeting = await create.Content.ReadFromJsonAsync<MeetingDto>();

        var found = await client.GetFromJsonAsync<MeetingDto>($"/api/meetings?joinUrl={Uri.EscapeDataString(meeting!.JoinUrl)}");
        Assert.Equal(meeting.Id, found!.Id);
    }

    [Fact]
    public async Task Api_returns_problem_details_for_invalid_requests()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/meetings",
            new CreateMeetingRequest { Subject = "", DurationMinutes = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Transcript_downloads_as_vtt()
    {
        var response = await factory.CreateClient().GetAsync("/meetings/demo-weekly-sync/transcripts/tr-1/content");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/vtt", response.Content.Headers.ContentType?.MediaType);
    }
}
