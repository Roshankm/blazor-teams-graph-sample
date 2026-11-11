namespace TeamsMeetings.Core;

/// <summary>Validation and time-zone handling for new meetings.</summary>
public static class MeetingScheduler
{
    public const int SubjectMaxLength = 255;
    public const int MinDurationMinutes = 15;
    public const int MaxDurationMinutes = 8 * 60;

    /// <summary>Allow a few minutes of slack so "now" is still accepted after the form is submitted.</summary>
    public static readonly TimeSpan PastTolerance = TimeSpan.FromMinutes(5);

    public static IReadOnlyList<string> Validate(CreateMeetingRequest request, DateTimeOffset nowUtc)
    {
        var errors = new List<string>();

        var subject = request.Subject?.Trim() ?? string.Empty;
        if (subject.Length == 0)
        {
            errors.Add("Subject is required.");
        }
        else if (subject.Length > SubjectMaxLength)
        {
            errors.Add($"Subject must be at most {SubjectMaxLength} characters.");
        }

        if (request.DurationMinutes is < MinDurationMinutes or > MaxDurationMinutes)
        {
            errors.Add($"Duration must be between {MinDurationMinutes} and {MaxDurationMinutes} minutes.");
        }

        if (!TryFindTimeZone(request.TimeZoneId, out var zone))
        {
            errors.Add($"Unknown time zone '{request.TimeZoneId}'.");
        }
        else if (request.StartLocal == default)
        {
            errors.Add("Start time is required.");
        }
        else if (ToUtc(request.StartLocal, zone) < nowUtc - PastTolerance)
        {
            errors.Add("Start time is in the past.");
        }

        return errors;
    }

    /// <summary>Converts the start time the user typed (in their zone) to UTC, and adds the duration.</summary>
    public static (DateTimeOffset StartUtc, DateTimeOffset EndUtc) GetUtcWindow(CreateMeetingRequest request)
    {
        if (!TryFindTimeZone(request.TimeZoneId, out var zone))
        {
            throw new ArgumentException($"Unknown time zone '{request.TimeZoneId}'.", nameof(request));
        }

        var start = ToUtc(request.StartLocal, zone);
        return (start, start.AddMinutes(request.DurationMinutes));
    }

    public static DateTimeOffset ToUtc(DateTime local, TimeZoneInfo zone)
    {
        var unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        var offset = zone.GetUtcOffset(unspecified);
        return new DateTimeOffset(unspecified, offset).ToUniversalTime();
    }

    public static bool TryFindTimeZone(string? id, out TimeZoneInfo zone)
    {
        zone = TimeZoneInfo.Utc;
        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        try
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById(id);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }

    /// <summary>
    /// Only Teams join links are accepted, so the lookup cannot be used to probe arbitrary URLs.
    /// </summary>
    public static bool IsTeamsJoinUrl(string? url) =>
        Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps
        && (uri.Host.Equals("teams.microsoft.com", StringComparison.OrdinalIgnoreCase)
            || uri.Host.Equals("teams.live.com", StringComparison.OrdinalIgnoreCase))
        && uri.AbsolutePath.StartsWith("/l/meetup-join/", StringComparison.OrdinalIgnoreCase);
}
