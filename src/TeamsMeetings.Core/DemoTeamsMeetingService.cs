using System.Collections.Concurrent;
using System.Text;

namespace TeamsMeetings.Core;

/// <summary>
/// In-memory implementation used when no Entra ID app registration is configured,
/// so the sample runs with a single <c>dotnet run</c>. Seeded with one past meeting
/// that has a recording and a transcript.
/// </summary>
public sealed class DemoTeamsMeetingService : ITeamsMeetingService
{
    private readonly ConcurrentDictionary<string, MeetingDto> _meetings = new();
    private readonly TimeProvider _time;

    public DemoTeamsMeetingService(TimeProvider time)
    {
        _time = time;
        var start = _time.GetUtcNow().AddDays(-1).AddHours(-2);
        Add(new MeetingDto("demo-weekly-sync", "Weekly delivery sync", start, start.AddMinutes(45),
            JoinUrlFor("demo-weekly-sync"), "+971 4 000 0000", "123 456 789#"));
    }

    public const string SeededMeetingId = "demo-weekly-sync";

    public Task<MeetingDto> CreateMeetingAsync(CreateMeetingRequest request, CancellationToken cancellationToken)
    {
        var errors = MeetingScheduler.Validate(request, _time.GetUtcNow());
        if (errors.Count > 0)
        {
            throw new TeamsMeetingException(string.Join(" ", errors), 400);
        }

        var (start, end) = MeetingScheduler.GetUtcWindow(request);
        var id = "demo-" + Guid.NewGuid().ToString("N")[..12];
        var meeting = new MeetingDto(id, request.Subject.Trim(), start, end, JoinUrlFor(id), null, null);
        Add(meeting);
        return Task.FromResult(meeting);
    }

    public Task<MeetingDto?> GetMeetingAsync(string meetingId, CancellationToken cancellationToken) =>
        Task.FromResult(_meetings.TryGetValue(meetingId, out var m) ? m : null);

    public Task<MeetingDto?> FindByJoinUrlAsync(string joinUrl, CancellationToken cancellationToken) =>
        Task.FromResult(_meetings.Values.FirstOrDefault(m =>
            string.Equals(m.JoinUrl, joinUrl.Trim(), StringComparison.OrdinalIgnoreCase)));

    public Task<MeetingArtifacts> GetArtifactsAsync(string meetingId, CancellationToken cancellationToken)
    {
        if (meetingId != SeededMeetingId || !_meetings.TryGetValue(meetingId, out var m))
        {
            return Task.FromResult(new MeetingArtifacts([], []));
        }

        return Task.FromResult(new MeetingArtifacts(
            [new RecordingDto("rec-1", m.EndUtc, m.EndUtc)],
            [new TranscriptDto("tr-1", m.EndUtc)]));
    }

    public Task<FileContent?> DownloadRecordingAsync(string meetingId, string recordingId, CancellationToken cancellationToken)
    {
        if (meetingId != SeededMeetingId || recordingId != "rec-1")
        {
            return Task.FromResult<FileContent?>(null);
        }

        // Real recordings are MP4 files; the demo returns a small text placeholder instead.
        var bytes = Encoding.UTF8.GetBytes("Demo mode: connect an Entra ID app registration to download real MP4 recordings.\n");
        return Task.FromResult<FileContent?>(new FileContent(new MemoryStream(bytes), "text/plain", "recording-demo.txt"));
    }

    public Task<FileContent?> DownloadTranscriptAsync(string meetingId, string transcriptId, CancellationToken cancellationToken)
    {
        if (meetingId != SeededMeetingId || transcriptId != "tr-1")
        {
            return Task.FromResult<FileContent?>(null);
        }

        const string vtt = """
            WEBVTT

            00:00:00.000 --> 00:00:04.000
            <v Roshan>Morning everyone, let's start with the release status.

            00:00:04.500 --> 00:00:09.000
            <v Sara>The Blazor dashboard passed QA yesterday and is ready for Thursday.

            """;
        var bytes = Encoding.UTF8.GetBytes(vtt);
        return Task.FromResult<FileContent?>(new FileContent(new MemoryStream(bytes), "text/vtt", "transcript.vtt"));
    }

    private static string JoinUrlFor(string id) => $"https://teams.microsoft.com/l/meetup-join/{id}";

    private void Add(MeetingDto meeting) => _meetings[meeting.Id] = meeting;
}
