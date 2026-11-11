using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

using TeamsMeetings.Core;
using TeamsMeetings.Graph;
using TeamsMeetings.Web;
using TeamsMeetings.Web.Components;
using TeamsMeetings.Web.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Graph mode needs an Entra ID app registration; without one the app runs on demo data.
var useGraph = !string.IsNullOrWhiteSpace(builder.Configuration["AzureAd:ClientId"]);
var defaultTimeZone = builder.Configuration["App:DefaultTimeZone"] ?? "Asia/Dubai";

builder.Services.AddRazorComponents();
builder.Services.AddProblemDetails();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(new AppMode(useGraph, defaultTimeZone));

if (useGraph)
{
    builder.Services
        .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
        .EnableTokenAcquisitionToCallDownstreamApi(GraphScopes.All)
        .AddMicrosoftGraph(builder.Configuration.GetSection("MicrosoftGraph"))
        .AddInMemoryTokenCaches();

    builder.Services.AddControllersWithViews().AddMicrosoftIdentityUI();

    // Every page and API call requires a signed-in user unless marked [AllowAnonymous].
    builder.Services.AddAuthorizationBuilder()
        .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

    builder.Services.AddScoped<ITeamsMeetingService, GraphTeamsMeetingService>();
}
else
{
    builder.Services.AddSingleton<ITeamsMeetingService, DemoTeamsMeetingService>();
}

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

if (useGraph)
{
    app.UseAuthentication();

    // If a Graph token can't be acquired silently, send the user back through sign-in
    // instead of showing an error page.
    app.Use(async (context, next) =>
    {
        try
        {
            await next(context);
        }
        catch (Exception ex) when (ConsentChallenge.IsRequired(ex) && !context.Response.HasStarted)
        {
            await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties
            {
                RedirectUri = context.Request.Path + context.Request.QueryString,
            });
        }
    });

    app.UseAuthorization();
}

app.UseAntiforgery();

app.MapStaticAssets().AllowAnonymous();
app.MapGet("/health", () => Results.Ok("healthy")).AllowAnonymous();
app.MapMeetingEndpoints();
app.MapRazorComponents<App>();

if (useGraph)
{
    app.MapControllers();
}

await app.RunAsync();

/// <summary>Exposed for <c>WebApplicationFactory</c> in the tests.</summary>
public partial class Program;
