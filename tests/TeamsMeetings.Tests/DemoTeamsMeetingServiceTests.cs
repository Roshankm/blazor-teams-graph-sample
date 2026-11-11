using TeamsMeetings.Core;

namespace TeamsMeetings.Tests;

public class DemoTeamsMeetingServiceTests
{
    private readonly DemoTeamsMeetingService _service =
        new(new FixedTimeProvider(new DateTimeOffset(2026, 11, 10, 6, 0, 0, TimeSpan.Zero)));

    [Fact]
    public async Task Created_meeting_can_be_read_back_and_found_by_join_link()
    {
        var created = await _service.CreateMeetingAsync(new CreateMeetingRequest
        {
            Subject = "  Architecture review  ",
            StartLocal = new DateTime(2026, 11, 12, 15, 0, 0),
            DurationMinutes = 30,
            TimeZoneId = "Asia/Dubai",
        }, CancellationToken.None);

        Assert.Equal("Architecture review", created.Subject);
        Assert.StartsWith("https://teams.microsoft.com/l/meetup-join/", created.JoinUrl);
        Assert.Equal(created, await _service.GetMeetingAsync(created.Id, CancellationToken.None));
        Assert.Equal(created, await _service.FindByJoinUrlAsync(created.JoinUrl, CancellationToken.None));
    }

    [Fact]
    public async Task Invalid_request_throws_with_status_400()
    {
        var ex = await Assert.ThrowsAsync<TeamsMeetingException>(() =>
            _service.CreateMeetingAsync(new CreateMeetingRequest { Subject = "", DurationMinutes = 5 }, CancellationToken.None));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task Seeded_meeting_has_a_recording_and_a_downloadable_transcript()
    {
        var artifacts = await _service.GetArtifactsAsync(DemoTeamsMeetingService.SeededMeetingId, CancellationToken.None);
        var transcript = await _service.DownloadTranscriptAsync(
            DemoTeamsMeetingService.SeededMeetingId, artifacts.Transcripts[0].Id, CancellationToken.None);

        Assert.Single(artifacts.Recordings);
        Assert.NotNull(transcript);
        Assert.Equal("text/vtt", transcript.ContentType);
        using var reader = new StreamReader(transcript.Content);
        Assert.StartsWith("WEBVTT", await reader.ReadToEndAsync());
    }

    [Fact]
    public async Task Unknown_meeting_returns_null_and_no_artifacts()
    {
        Assert.Null(await _service.GetMeetingAsync("missing", CancellationToken.None));
        Assert.Empty((await _service.GetArtifactsAsync("missing", CancellationToken.None)).Recordings);
        Assert.Null(await _service.DownloadRecordingAsync("missing", "rec-1", CancellationToken.None));
    }
}
