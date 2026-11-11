namespace TeamsMeetings.Graph;

/// <summary>Delegated Microsoft Graph permissions this sample needs.</summary>
public static class GraphScopes
{
    /// <summary>Create and read online meetings the signed-in user organises.</summary>
    public const string OnlineMeetingsReadWrite = "OnlineMeetings.ReadWrite";

    /// <summary>List and download recordings of the user's meetings. Requires admin consent.</summary>
    public const string RecordingsRead = "OnlineMeetingRecording.Read.All";

    /// <summary>List and download transcripts of the user's meetings. Requires admin consent.</summary>
    public const string TranscriptsRead = "OnlineMeetingTranscript.Read.All";

    public static readonly string[] All = ["User.Read", OnlineMeetingsReadWrite, RecordingsRead, TranscriptsRead];
}
