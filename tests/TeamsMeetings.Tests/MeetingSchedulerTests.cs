using TeamsMeetings.Core;

namespace TeamsMeetings.Tests;

public class MeetingSchedulerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 6, 0, 0, TimeSpan.Zero); // 10:00 in Dubai

    private static CreateMeetingRequest Valid() => new()
    {
        Subject = "Sprint review",
        StartLocal = new DateTime(2026, 11, 10, 11, 0, 0),
        DurationMinutes = 60,
        TimeZoneId = "Asia/Dubai",
    };

    [Fact]
    public void Valid_request_has_no_errors()
    {
        Assert.Empty(MeetingScheduler.Validate(Valid(), Now));
    }

    [Fact]
    public void Converts_local_time_in_the_chosen_zone_to_utc()
    {
        var (start, end) = MeetingScheduler.GetUtcWindow(Valid());

        Assert.Equal(new DateTimeOffset(2026, 11, 10, 7, 0, 0, TimeSpan.Zero), start);
        Assert.Equal(TimeSpan.Zero, start.Offset);
        Assert.Equal(start.AddMinutes(60), end);
    }

    [Fact]
    public void Daylight_saving_is_applied_for_zones_that_use_it()
    {
        var summer = Valid();
        summer.TimeZoneId = "Europe/London";
        summer.StartLocal = new DateTime(2027, 7, 1, 9, 0, 0);

        var (start, _) = MeetingScheduler.GetUtcWindow(summer);

        Assert.Equal(new DateTimeOffset(2027, 7, 1, 8, 0, 0, TimeSpan.Zero), start); // BST is UTC+1
    }

    [Theory]
    [InlineData("", "Subject is required.")]
    [InlineData("   ", "Subject is required.")]
    public void Subject_is_required(string subject, string expected)
    {
        var request = Valid();
        request.Subject = subject;

        Assert.Contains(expected, MeetingScheduler.Validate(request, Now));
    }

    [Theory]
    [InlineData(10)]
    [InlineData(481)]
    public void Duration_must_be_within_limits(int minutes)
    {
        var request = Valid();
        request.DurationMinutes = minutes;

        Assert.Single(MeetingScheduler.Validate(request, Now));
    }

    [Fact]
    public void Start_in_the_past_is_rejected_but_small_clock_skew_is_allowed()
    {
        var past = Valid();
        past.StartLocal = new DateTime(2026, 11, 10, 9, 0, 0);
        var justNow = Valid();
        justNow.StartLocal = new DateTime(2026, 11, 10, 9, 58, 0);

        Assert.Contains("Start time is in the past.", MeetingScheduler.Validate(past, Now));
        Assert.Empty(MeetingScheduler.Validate(justNow, Now));
    }

    [Fact]
    public void Missing_start_time_is_reported_instead_of_crashing()
    {
        var request = Valid();
        request.StartLocal = default;

        Assert.Contains("Start time is required.", MeetingScheduler.Validate(request, Now));
    }

    [Fact]
    public void Unknown_time_zone_is_rejected()
    {
        var request = Valid();
        request.TimeZoneId = "Mars/Olympus_Mons";

        Assert.Contains("Unknown time zone 'Mars/Olympus_Mons'.", MeetingScheduler.Validate(request, Now));
    }

    [Theory]
    [InlineData("https://teams.microsoft.com/l/meetup-join/19%3ameeting_abc%40thread.v2/0", true)]
    [InlineData("https://teams.live.com/l/meetup-join/abc", true)]
    [InlineData("http://teams.microsoft.com/l/meetup-join/abc", false)]
    [InlineData("https://evil.example.com/l/meetup-join/abc", false)]
    [InlineData("https://teams.microsoft.com/settings", false)]
    [InlineData("not a url", false)]
    [InlineData(null, false)]
    public void Only_https_teams_join_links_are_accepted(string? url, bool expected)
    {
        Assert.Equal(expected, MeetingScheduler.IsTeamsJoinUrl(url));
    }
}
