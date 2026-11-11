namespace TeamsMeetings.Web;

/// <summary>Whether the app talks to Microsoft Graph or runs on in-memory demo data.</summary>
public sealed record AppMode(bool UsesGraph, string DefaultTimeZone);
