using TeamsMeetings.Core;

namespace TeamsMeetings.Web.Endpoints;

/// <summary>
/// JSON API over the same service the Blazor pages use, so a Blazor WebAssembly client
/// (or any SPA) can call Graph through this backend without ever holding a Graph token.
/// </summary>
internal static class MeetingEndpoints
{
    public static IEndpointRouteBuilder MapMeetingEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/meetings").WithTags("Meetings");

        api.MapPost("/", (CreateMeetingRequest request, ITeamsMeetingService meetings, CancellationToken ct) =>
            Handle(async () =>
            {
                var meeting = await meetings.CreateMeetingAsync(request, ct);
                return Results.Created($"/api/meetings/{Uri.EscapeDataString(meeting.Id)}", meeting);
            }));

        api.MapGet("/", (string joinUrl, ITeamsMeetingService meetings, CancellationToken ct) =>
            Handle(async () => await meetings.FindByJoinUrlAsync(joinUrl, ct) is { } m ? Results.Ok(m) : Results.NotFound()));

        api.MapGet("/{meetingId}", (string meetingId, ITeamsMeetingService meetings, CancellationToken ct) =>
            Handle(async () => await meetings.GetMeetingAsync(meetingId, ct) is { } m ? Results.Ok(m) : Results.NotFound()));

        api.MapGet("/{meetingId}/artifacts", (string meetingId, ITeamsMeetingService meetings, CancellationToken ct) =>
            Handle(async () => Results.Ok(await meetings.GetArtifactsAsync(meetingId, ct))));

        // Download links are used by the pages too, so they sit outside /api.
        app.MapGet("/meetings/{meetingId}/recordings/{recordingId}/content",
            (string meetingId, string recordingId, ITeamsMeetingService meetings, CancellationToken ct) =>
                Handle(async () => ToFile(await meetings.DownloadRecordingAsync(meetingId, recordingId, ct))));

        app.MapGet("/meetings/{meetingId}/transcripts/{transcriptId}/content",
            (string meetingId, string transcriptId, ITeamsMeetingService meetings, CancellationToken ct) =>
                Handle(async () => ToFile(await meetings.DownloadTranscriptAsync(meetingId, transcriptId, ct))));

        return app;
    }

    private static IResult ToFile(FileContent? file) =>
        file is null ? Results.NotFound() : Results.Stream(file.Content, file.ContentType, file.FileName);

    private static async Task<IResult> Handle(Func<Task<IResult>> action)
    {
        try
        {
            return await action();
        }
        catch (TeamsMeetingException ex)
        {
            return Results.Problem(ex.Message, statusCode: ex.StatusCode);
        }
    }
}
