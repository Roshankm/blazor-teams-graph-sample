namespace TeamsMeetings.Core;

/// <summary>What the user fills in to schedule a Teams meeting.</summary>
public sealed class CreateMeetingRequest
{
    public string Subject { get; set; } = string.Empty;

    /// <summary>Start time as the user typed it, in <see cref="TimeZoneId"/>.</summary>
    public DateTime StartLocal { get; set; }

    public int DurationMinutes { get; set; } = 30;

    /// <summary>IANA time zone, for example <c>Asia/Dubai</c>.</summary>
    public string TimeZoneId { get; set; } = "Asia/Dubai";

    /// <summary>When true, people outside the organisation skip the lobby.</summary>
    public bool EveryoneBypassesLobby { get; set; }
}

public sealed record MeetingDto(
    string Id,
    string Subject,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    string JoinUrl,
    string? DialInNumber,
    string? ConferenceId);

public sealed record RecordingDto(
    string Id,
    DateTimeOffset? CreatedUtc,
    DateTimeOffset? EndedUtc);

public sealed record TranscriptDto(
    string Id,
    DateTimeOffset? CreatedUtc);

public sealed record MeetingArtifacts(
    IReadOnlyList<RecordingDto> Recordings,
    IReadOnlyList<TranscriptDto> Transcripts);

/// <summary>A downloadable file returned by the service.</summary>
public sealed record FileContent(Stream Content, string ContentType, string FileName);
