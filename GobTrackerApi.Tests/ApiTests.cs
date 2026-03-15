using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GobTrackerApi.Tests;

public class ApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    // ── Health ──

    [Fact]
    public async Task HealthEndpoint_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── Teams ──

    [Fact]
    public async Task PostTeam_MissingName_ReturnsBadRequest()
    {
        var team = new { Name = "", City = "Pittsburgh", Coach = "Coach" };
        var response = await _client.PostAsJsonAsync("/api/teams", team);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostTeam_MissingCity_ReturnsBadRequest()
    {
        var team = new { Name = "Steelers", City = "", Coach = "Coach" };
        var response = await _client.PostAsJsonAsync("/api/teams", team);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── Players ──

    [Fact]
    public async Task PostPlayer_InvalidTeamId_ReturnsBadRequest()
    {
        var player = new { TeamId = 0, FirstName = "John", LastName = "Doe", JerseyNumber = 10, Position = "PG" };
        var response = await _client.PostAsJsonAsync("/api/players", player);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostPlayer_MissingFirstName_ReturnsBadRequest()
    {
        var player = new { TeamId = 1, FirstName = "", LastName = "Doe", JerseyNumber = 10, Position = "PG" };
        var response = await _client.PostAsJsonAsync("/api/players", player);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostPlayer_MissingLastName_ReturnsBadRequest()
    {
        var player = new { TeamId = 1, FirstName = "John", LastName = "", JerseyNumber = 10, Position = "PG" };
        var response = await _client.PostAsJsonAsync("/api/players", player);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── Games ──

    [Fact]
    public async Task PostGame_InvalidHomeTeamId_ReturnsBadRequest()
    {
        var game = new { HomeTeamId = 0, AwayTeamId = 2, GameDate = DateTime.Now, Location = "Arena", Status = "Scheduled" };
        var response = await _client.PostAsJsonAsync("/api/games", game);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostGame_SameHomeAndAway_ReturnsBadRequest()
    {
        var game = new { HomeTeamId = 1, AwayTeamId = 1, GameDate = DateTime.Now, Location = "Arena", Status = "Scheduled" };
        var response = await _client.PostAsJsonAsync("/api/games", game);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostGame_MissingLocation_ReturnsBadRequest()
    {
        var game = new { HomeTeamId = 1, AwayTeamId = 2, GameDate = DateTime.Now, Location = "", Status = "Scheduled" };
        var response = await _client.PostAsJsonAsync("/api/games", game);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostGame_MissingStatus_ReturnsBadRequest()
    {
        var game = new { HomeTeamId = 1, AwayTeamId = 2, GameDate = DateTime.Now, Location = "Arena", Status = "" };
        var response = await _client.PostAsJsonAsync("/api/games", game);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── Schedules ──

    [Fact]
    public async Task PostSchedule_InvalidTeamId_ReturnsBadRequest()
    {
        var schedule = new { TeamId = 0, GameId = 1, IsHome = true };
        var response = await _client.PostAsJsonAsync("/api/schedules", schedule);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostSchedule_InvalidGameId_ReturnsBadRequest()
    {
        var schedule = new { TeamId = 1, GameId = 0, IsHome = true };
        var response = await _client.PostAsJsonAsync("/api/schedules", schedule);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── Stats ──

    [Fact]
    public async Task PostStat_InvalidGameId_ReturnsBadRequest()
    {
        var stat = new { GameId = 0, PlayerId = 1, TwoPtMiss = 0, TwoPtMade = 0, ThreePtMiss = 0, ThreePtMade = 0, Steals = 0, Turnovers = 0, Assists = 0, Blocks = 0, Fouls = 0, OffensiveRebounds = 0, DefensiveRebounds = 0 };
        var response = await _client.PostAsJsonAsync("/api/stats", stat);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostStat_InvalidPlayerId_ReturnsBadRequest()
    {
        var stat = new { GameId = 1, PlayerId = 0, TwoPtMiss = 0, TwoPtMade = 0, ThreePtMiss = 0, ThreePtMade = 0, Steals = 0, Turnovers = 0, Assists = 0, Blocks = 0, Fouls = 0, OffensiveRebounds = 0, DefensiveRebounds = 0 };
        var response = await _client.PostAsJsonAsync("/api/stats", stat);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}