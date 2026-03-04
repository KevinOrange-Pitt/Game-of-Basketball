using MauiApp1.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace MauiApp1.Services;

public class DatabaseService
{
    private readonly HttpClient _httpClient;

    public DatabaseService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<DatabaseItem>> GetPlayersAsync()
    {
        using var response = await _httpClient.GetAsync("api/players");
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var detail = TryExtractProblemDetail(body);
            throw new HttpRequestException($"API returned {(int)response.StatusCode} {response.ReasonPhrase}. {detail}");
        }

        var players = JsonSerializer.Deserialize<List<DatabaseItem>>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return players ?? new List<DatabaseItem>();
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
        }
        catch
        {
        }

        return body;
    }
}
