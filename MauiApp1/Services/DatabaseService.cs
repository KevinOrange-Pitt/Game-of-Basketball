using MauiApp1.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace MauiApp1.Services;

public class DatabaseService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public DatabaseService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<TeamItem>> GetTeamsAsync() => await GetListAsync<TeamItem>("api/teams");
    public async Task<List<PlayerItem>> GetPlayersAsync() => await GetListAsync<PlayerItem>("api/players");
    public async Task<List<GameItem>> GetGamesAsync() => await GetListAsync<GameItem>("api/games");
    public async Task<List<ScheduleItem>> GetSchedulesAsync() => await GetListAsync<ScheduleItem>("api/schedules");
    public async Task<List<StatItem>> GetStatsAsync() => await GetListAsync<StatItem>("api/stats");

    public async Task<TeamItem> CreateTeamAsync(string name, string city, string coach)
        => await PostAsync<TeamItem>("api/teams", new TeamWriteRequest(name, city, coach));

    public async Task UpdateTeamAsync(int id, string name, string city, string coach)
        => await PutAsync($"api/teams/{id}", new TeamWriteRequest(name, city, coach));

    public async Task DeleteTeamAsync(int id)
        => await DeleteAsync($"api/teams/{id}");

    public async Task<PlayerItem> CreatePlayerAsync(int teamId, string firstName, string lastName, int? jerseyNumber, string position)
        => await PostAsync<PlayerItem>("api/players", new PlayerWriteRequest(teamId, firstName, lastName, jerseyNumber, position));

    public async Task UpdatePlayerAsync(int id, int teamId, string firstName, string lastName, int? jerseyNumber, string position)
        => await PutAsync($"api/players/{id}", new PlayerWriteRequest(teamId, firstName, lastName, jerseyNumber, position));

    public async Task DeletePlayerAsync(int id)
        => await DeleteAsync($"api/players/{id}");

    public async Task<GameItem> CreateGameAsync(int homeTeamId, int awayTeamId, DateTime gameDate, string location, int? homeScore, int? awayScore, string status)
        => await PostAsync<GameItem>("api/games", new GameWriteRequest(homeTeamId, awayTeamId, gameDate, location, homeScore, awayScore, status));

    public async Task UpdateGameAsync(int id, int homeTeamId, int awayTeamId, DateTime gameDate, string location, int? homeScore, int? awayScore, string status)
        => await PutAsync($"api/games/{id}", new GameWriteRequest(homeTeamId, awayTeamId, gameDate, location, homeScore, awayScore, status));

    public async Task DeleteGameAsync(int id)
        => await DeleteAsync($"api/games/{id}");

    public async Task<ScheduleItem> CreateScheduleAsync(int teamId, int gameId, bool isHome)
        => await PostAsync<ScheduleItem>("api/schedules", new ScheduleWriteRequest(teamId, gameId, isHome));

    public async Task UpdateScheduleAsync(int id, int teamId, int gameId, bool isHome)
        => await PutAsync($"api/schedules/{id}", new ScheduleWriteRequest(teamId, gameId, isHome));

    public async Task DeleteScheduleAsync(int id)
        => await DeleteAsync($"api/schedules/{id}");

    public async Task<StatItem> CreateStatAsync(
        int gameId,
        int playerId,
        int twoPtMiss,
        int twoPtMade,
        int threePtMiss,
        int threePtMade,
        int steals,
        int turnovers,
        int assists,
        int blocks,
        int fouls,
        int offensiveRebounds,
        int defensiveRebounds)
        => await PostAsync<StatItem>("api/stats", new StatWriteRequest(
            gameId,
            playerId,
            twoPtMiss,
            twoPtMade,
            threePtMiss,
            threePtMade,
            steals,
            turnovers,
            assists,
            blocks,
            fouls,
            offensiveRebounds,
            defensiveRebounds));

    public async Task UpdateStatAsync(
        int id,
        int gameId,
        int playerId,
        int twoPtMiss,
        int twoPtMade,
        int threePtMiss,
        int threePtMade,
        int steals,
        int turnovers,
        int assists,
        int blocks,
        int fouls,
        int offensiveRebounds,
        int defensiveRebounds)
        => await PutAsync($"api/stats/{id}", new StatWriteRequest(
            gameId,
            playerId,
            twoPtMiss,
            twoPtMade,
            threePtMiss,
            threePtMade,
            steals,
            turnovers,
            assists,
            blocks,
            fouls,
            offensiveRebounds,
            defensiveRebounds));

    public async Task DeleteStatAsync(int id)
        => await DeleteAsync($"api/stats/{id}");

    private async Task<List<T>> GetListAsync<T>(string endpoint)
    {
        using var response = await _httpClient.GetAsync(endpoint);
        var body = await response.Content.ReadAsStringAsync();
        EnsureSuccess(response, body);
        return JsonSerializer.Deserialize<List<T>>(body, _jsonOptions) ?? new List<T>();
    }

    private async Task<T> PostAsync<T>(string endpoint, object payload)
    {
        using var response = await _httpClient.PostAsJsonAsync(endpoint, payload);
        var body = await response.Content.ReadAsStringAsync();
        EnsureSuccess(response, body);

        var item = JsonSerializer.Deserialize<T>(body, _jsonOptions);
        if (item is null)
        {
            throw new InvalidOperationException($"API returned an unexpected response for {endpoint}.");
        }

        return item;
    }

    private async Task PutAsync(string endpoint, object payload)
    {
        using var response = await _httpClient.PutAsJsonAsync(endpoint, payload);
        var body = await response.Content.ReadAsStringAsync();
        EnsureSuccess(response, body);
    }

    private async Task DeleteAsync(string endpoint)
    {
        using var response = await _httpClient.DeleteAsync(endpoint);
        var body = await response.Content.ReadAsStringAsync();
        EnsureSuccess(response, body);
    }

    private static void EnsureSuccess(HttpResponseMessage response, string body)
    {
        if (!response.IsSuccessStatusCode)
        {
            var detail = TryExtractProblemDetail(body);
            throw new HttpRequestException($"API returned {(int)response.StatusCode} {response.ReasonPhrase}. {detail}");
        }
    }

    private static string TryExtractProblemDetail(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return string.Empty;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("detail", out var detailElement))
            {
                var detail = detailElement.GetString();
                return string.IsNullOrWhiteSpace(detail) ? body : detail;
            }

            if (document.RootElement.TryGetProperty("message", out var messageElement))
            {
                var message = messageElement.GetString();
                return string.IsNullOrWhiteSpace(message) ? body : message;
            }
        }
        catch
        {
        }

        return body;
    }

    private record TeamWriteRequest(string Name, string City, string Coach);
    private record PlayerWriteRequest(int TeamId, string FirstName, string LastName, int? JerseyNumber, string Position);
    private record GameWriteRequest(int HomeTeamId, int AwayTeamId, DateTime GameDate, string Location, int? HomeScore, int? AwayScore, string Status);
    private record ScheduleWriteRequest(int TeamId, int GameId, bool IsHome);
    private record StatWriteRequest(
        int GameId,
        int PlayerId,
        int TwoPtMiss,
        int TwoPtMade,
        int ThreePtMiss,
        int ThreePtMade,
        int Steals,
        int Turnovers,
        int Assists,
        int Blocks,
        int Fouls,
        int OffensiveRebounds,
        int DefensiveRebounds);
}
