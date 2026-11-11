using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Identity.Client;

using TeamsMeetings.Graph;
using TeamsMeetings.Web;

namespace TeamsMeetings.Tests;

public class GraphMapperTests
{
    [Fact]
    public void Maps_online_meeting_including_dial_in_details()
    {
        var start = new DateTimeOffset(2026, 11, 12, 11, 0, 0, TimeSpan.Zero);
        var meeting = new OnlineMeeting
        {
            Id = "MSo1N2Y5",
            Subject = "Client demo",
            StartDateTime = start,
            EndDateTime = start.AddHours(1),
            JoinWebUrl = "https://teams.microsoft.com/l/meetup-join/abc",
            AudioConferencing = new AudioConferencing { TollNumber = "+971 4 123 4567", ConferenceId = "987654" },
        };

        var dto = GraphMapper.ToDto(meeting);

        Assert.Equal("MSo1N2Y5", dto.Id);
        Assert.Equal("Client demo", dto.Subject);
        Assert.Equal(start.AddHours(1), dto.EndUtc);
        Assert.Equal("+971 4 123 4567", dto.DialInNumber);
        Assert.Equal("987654", dto.ConferenceId);
    }

    [Fact]
    public void Single_quotes_are_escaped_for_odata_filters()
    {
        Assert.Equal("O''Brien''s", GraphMapper.EscapeODataString("O'Brien's"));
    }

    [Theory]
    [InlineData(403, "administrator needs to grant consent")]
    [InlineData(404, "only available to the meeting organiser")]
    [InlineData(429, "throttling")]
    public void Graph_errors_become_actionable_messages(int status, string expectedFragment)
    {
        var error = new ODataError { ResponseStatusCode = status, Error = new MainError { Message = "graph detail" } };

        var ex = GraphMapper.ToException(error);

        Assert.Equal(status, ex.StatusCode);
        Assert.Contains(expectedFragment, ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Consent_challenge_is_detected_through_wrapping_exceptions()
    {
        var wrapped = new InvalidOperationException("outer", new MsalUiRequiredException("invalid_grant", "consent required"));

        Assert.True(ConsentChallenge.IsRequired(wrapped));
        Assert.False(ConsentChallenge.IsRequired(new InvalidOperationException("unrelated")));
    }
}
