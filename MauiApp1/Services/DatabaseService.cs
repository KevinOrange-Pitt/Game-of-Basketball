using MauiApp1.Models;
using System.Net.Http.Json;

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
        var response = await _httpClient.GetFromJsonAsync<List<DatabaseItem>>("api/players");
        return response ?? new List<DatabaseItem>();
    }
}
