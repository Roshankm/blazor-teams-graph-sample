using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;

using TeamsMeetings.Core;

namespace TeamsMeetings.Graph;

internal static class GraphMapper
{
    public static MeetingDto ToDto(OnlineMeeting meeting) => new(
        meeting.Id ?? string.Empty,
        meeting.Subject ?? "(no subject)",
        meeting.StartDateTime ?? DateTimeOffset.MinValue,
        meeting.EndDateTime ?? DateTimeOffset.MinValue,
        meeting.JoinWebUrl ?? string.Empty,
        meeting.AudioConferencing?.TollNumber,
        meeting.AudioConferencing?.ConferenceId);

    public static RecordingDto ToDto(CallRecording recording) =>
        new(recording.Id!, recording.CreatedDateTime, recording.EndDateTime);

    public static TranscriptDto ToDto(CallTranscript transcript) =>
        new(transcript.Id!, transcript.CreatedDateTime);

    /// <summary>OData string literals escape a single quote by doubling it.</summary>
    public static string EscapeODataString(string value) => value.Replace("'", "''", StringComparison.Ordinal);

    /// <summary>Turns Graph errors into messages that tell the user (or their admin) what to fix.</summary>
    public static TeamsMeetingException ToException(ODataError error)
    {
        var status = error.ResponseStatusCode;
        var graphMessage = error.Error?.Message ?? error.Message;

        var message = status switch
        {
            401 => "Your sign-in has expired. Sign out and sign in again.",
            403 => "Microsoft Graph denied access. An administrator needs to grant consent for the "
                   + "OnlineMeetings, recording and transcript permissions in Entra ID. "
                   + $"Graph said: {graphMessage}",
            404 => "The meeting, recording or transcript was not found. Recordings are only available "
                   + "to the meeting organiser, and can take a few minutes to appear after the meeting ends.",
            429 => "Microsoft Graph is throttling requests. Wait a moment and try again.",
            _ => $"Microsoft Graph returned an error ({status}): {graphMessage}",
        };

        return new TeamsMeetingException(message, status == 0 ? 502 : status, error);
    }
}
