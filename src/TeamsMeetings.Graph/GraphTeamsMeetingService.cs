using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;

using TeamsMeetings.Core;

namespace TeamsMeetings.Graph;

/// <summary>
/// Calls Microsoft Graph <c>/me/onlineMeetings</c> on behalf of the signed-in user.
/// Each method maps Graph errors to <see cref="TeamsMeetingException"/> with an actionable message.
/// </summary>
public sealed class GraphTeamsMeetingService(GraphServiceClient graph, TimeProvider time) : ITeamsMeetingService
{
    public async Task<MeetingDto> CreateMeetingAsync(CreateMeetingRequest request, CancellationToken cancellationToken)
    {
        var errors = MeetingScheduler.Validate(request, time.GetUtcNow());
        if (errors.Count > 0)
        {
            throw new TeamsMeetingException(string.Join(" ", errors), 400);
        }

        var (start, end) = MeetingScheduler.GetUtcWindow(request);
        var body = new OnlineMeeting
        {
            Subject = request.Subject.Trim(),
            StartDateTime = start,
            EndDateTime = end,
            LobbyBypassSettings = new LobbyBypassSettings
            {
                Scope = request.EveryoneBypassesLobby ? LobbyBypassScope.Everyone : LobbyBypassScope.Organization,
            },
        };

        var created = await Call(() => graph.Me.OnlineMeetings.PostAsync(body, cancellationToken: cancellationToken));
        return GraphMapper.ToDto(created ?? throw new TeamsMeetingException("Graph returned no meeting.", 502));
    }

    public async Task<MeetingDto?> GetMeetingAsync(string meetingId, CancellationToken cancellationToken)
    {
        try
        {
            var meeting = await graph.Me.OnlineMeetings[meetingId].GetAsync(cancellationToken: cancellationToken);
            return meeting is null ? null : GraphMapper.ToDto(meeting);
        }
        catch (ODataError ex) when (ex.ResponseStatusCode == 404)
        {
            return null;
        }
        catch (ODataError ex)
        {
            throw GraphMapper.ToException(ex);
        }
    }

    public async Task<MeetingDto?> FindByJoinUrlAsync(string joinUrl, CancellationToken cancellationToken)
    {
        if (!MeetingScheduler.IsTeamsJoinUrl(joinUrl))
        {
            throw new TeamsMeetingException("That is not a Teams meeting link.", 400);
        }

        var filter = $"JoinWebUrl eq '{GraphMapper.EscapeODataString(joinUrl.Trim())}'";
        var page = await Call(() => graph.Me.OnlineMeetings.GetAsync(
            config => config.QueryParameters.Filter = filter, cancellationToken));

        var meeting = page?.Value?.FirstOrDefault();
        return meeting is null ? null : GraphMapper.ToDto(meeting);
    }

    public async Task<MeetingArtifacts> GetArtifactsAsync(string meetingId, CancellationToken cancellationToken)
    {
        var meetingRequests = graph.Me.OnlineMeetings[meetingId];

        // Recordings and transcripts are independent calls; run them together.
        var recordingsTask = Call(() => meetingRequests.Recordings.GetAsync(cancellationToken: cancellationToken));
        var transcriptsTask = Call(() => meetingRequests.Transcripts.GetAsync(cancellationToken: cancellationToken));
        await Task.WhenAll(recordingsTask, transcriptsTask);

        var recordings = (recordingsTask.Result?.Value ?? [])
            .Where(r => r.Id is not null)
            .Select(r => GraphMapper.ToDto(r))
            .OrderByDescending(r => r.CreatedUtc)
            .ToList();

        var transcripts = (transcriptsTask.Result?.Value ?? [])
            .Where(t => t.Id is not null)
            .Select(t => GraphMapper.ToDto(t))
            .OrderByDescending(t => t.CreatedUtc)
            .ToList();

        return new MeetingArtifacts(recordings, transcripts);
    }

    public async Task<FileContent?> DownloadRecordingAsync(string meetingId, string recordingId, CancellationToken cancellationToken)
    {
        var stream = await Call(() => graph.Me.OnlineMeetings[meetingId].Recordings[recordingId].Content
            .GetAsync(cancellationToken: cancellationToken));
        return stream is null ? null : new FileContent(stream, "video/mp4", $"recording-{recordingId[..Math.Min(8, recordingId.Length)]}.mp4");
    }

    public async Task<FileContent?> DownloadTranscriptAsync(string meetingId, string transcriptId, CancellationToken cancellationToken)
    {
        var stream = await Call(() => graph.Me.OnlineMeetings[meetingId].Transcripts[transcriptId].Content
            .GetAsync(cancellationToken: cancellationToken));
        return stream is null ? null : new FileContent(stream, "text/vtt", $"transcript-{transcriptId[..Math.Min(8, transcriptId.Length)]}.vtt");
    }

    private static async Task<T?> Call<T>(Func<Task<T?>> action)
    {
        try
        {
            return await action();
        }
        catch (ODataError ex)
        {
            throw GraphMapper.ToException(ex);
        }
    }
}
