namespace TeamsMeetings.Core;

/// <summary>
/// Teams meeting operations used by the UI and the API. The Graph implementation calls
/// Microsoft Graph on behalf of the signed-in user; the demo implementation runs in memory.
/// </summary>
public interface ITeamsMeetingService
{
    Task<MeetingDto> CreateMeetingAsync(CreateMeetingRequest request, CancellationToken cancellationToken);

    Task<MeetingDto?> GetMeetingAsync(string meetingId, CancellationToken cancellationToken);

    /// <summary>
    /// Graph has no "list my meetings" endpoint for online meetings, so existing meetings are
    /// looked up by the join link that people already have in their calendar invite.
    /// </summary>
    Task<MeetingDto?> FindByJoinUrlAsync(string joinUrl, CancellationToken cancellationToken);

    Task<MeetingArtifacts> GetArtifactsAsync(string meetingId, CancellationToken cancellationToken);

    Task<FileContent?> DownloadRecordingAsync(string meetingId, string recordingId, CancellationToken cancellationToken);

    Task<FileContent?> DownloadTranscriptAsync(string meetingId, string transcriptId, CancellationToken cancellationToken);
}

/// <summary>Raised when Graph refuses a request, with a message the user can act on.</summary>
public sealed class TeamsMeetingException(string message, int statusCode, Exception? inner = null)
    : Exception(message, inner)
{
    public int StatusCode { get; } = statusCode;
}
