using MauiApp1.Models;
using MauiApp1.Services;
using System.Collections.ObjectModel;

namespace MauiApp1;

public partial class StatModePage : ContentPage
{
    private readonly DatabaseService _db;
    private bool _isWorking;
    private bool _showingPlayers = true;

    private List<PlayerStatSummary> _allPlayerStats = new();
    private List<TeamStatSummary> _allTeamStats = new();

    public ObservableCollection<PlayerStatSummary> FilteredPlayerStats { get; } = new();
    public ObservableCollection<TeamStatSummary> FilteredTeamStats { get; } = new();

    public bool ShowingPlayers => _showingPlayers;
    public bool ShowingTeams => !_showingPlayers;
    public string PlayerTabColor => _showingPlayers ? "#2F80ED" : "#B7CAE3";
    public string TeamTabColor => !_showingPlayers ? "#2F80ED" : "#B7CAE3";

    public bool IsWorking
    {
        get => _isWorking;
        set { _isWorking = value; OnPropertyChanged(); }
    }

    private string _statusMessage = "Loading stats...";
    public string StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(); } }

    private string _searchQuery = string.Empty;
    public string SearchQuery
    {
        get => _searchQuery;
        set { _searchQuery = value; OnPropertyChanged(); }
    }

    public StatModePage()
    {
        _db = new DatabaseService(new HttpClient { BaseAddress = new Uri("http://127.0.0.1:5117/") });
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAllStatsAsync();
    }

    private async Task LoadAllStatsAsync()
    {
        try
        {
            IsWorking = true;

            var players = await _db.GetPlayersAsync();
            var teams = await _db.GetTeamsAsync();
            var stats = await _db.GetStatsAsync();
            var games = await _db.GetGamesAsync();

            // Build player summaries
            _allPlayerStats = players.Select(p =>
            {
                var playerStats = stats.Where(s => s.PlayerId == p.PlayerId).ToList();
                var teamName = teams.FirstOrDefault(t => t.TeamId == p.TeamId)?.Name ?? $"Team {p.TeamId}";
                return new PlayerStatSummary
                {
                    PlayerId = p.PlayerId,
                    PlayerName = p.FullName,
                    TeamName = teamName,
                    GamesPlayed = playerStats.Select(s => s.GameId).Distinct().Count(),
                    TotalPoints = playerStats.Sum(s => s.TotalPoints),
                    TotalRebounds = playerStats.Sum(s => s.TotalRebounds),
                    TotalAssists = playerStats.Sum(s => s.Assists),
                    TotalSteals = playerStats.Sum(s => s.Steals),
                    TotalBlocks = playerStats.Sum(s => s.Blocks),
                    TotalTurnovers = playerStats.Sum(s => s.Turnovers)
                };
            }).OrderByDescending(p => p.TotalPoints).ToList();

            // Build team summaries
            _allTeamStats = teams.Select(t =>
            {
                var teamPlayerIds = players.Where(p => p.TeamId == t.TeamId).Select(p => p.PlayerId).ToHashSet();
                var teamStats = stats.Where(s => teamPlayerIds.Contains(s.PlayerId)).ToList();
                var gamesPlayed = games.Count(g => g.HomeTeamId == t.TeamId || g.AwayTeamId == t.TeamId);
                return new TeamStatSummary
                {
                    TeamId = t.TeamId,
                    TeamName = t.Name,
                    City = t.City,
                    GamesPlayed = gamesPlayed,
                    TotalPoints = teamStats.Sum(s => s.TotalPoints),
                    TotalRebounds = teamStats.Sum(s => s.TotalRebounds),
                    TotalAssists = teamStats.Sum(s => s.Assists),
                    TotalSteals = teamStats.Sum(s => s.Steals)
                };
            }).OrderByDescending(t => t.TotalPoints).ToList();

            ApplyFilter();
            StatusMessage = $"Loaded {players.Count} players across {teams.Count} teams.";
        }
        catch (Exception ex)
        {
            StatusMessage = "Could not load stats.";
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally { IsWorking = false; }
    }

    private void ApplyFilter()
    {
        var query = SearchQuery.Trim().ToLowerInvariant();

        FilteredPlayerStats.Clear();
        var filteredPlayers = string.IsNullOrEmpty(query)
            ? _allPlayerStats
            : _allPlayerStats.Where(p =>
                p.PlayerName.ToLowerInvariant().Contains(query) ||
                p.TeamName.ToLowerInvariant().Contains(query));
        foreach (var p in filteredPlayers) FilteredPlayerStats.Add(p);

        FilteredTeamStats.Clear();
        var filteredTeams = string.IsNullOrEmpty(query)
            ? _allTeamStats
            : _allTeamStats.Where(t =>
                t.TeamName.ToLowerInvariant().Contains(query) ||
                t.City.ToLowerInvariant().Contains(query));
        foreach (var t in filteredTeams) FilteredTeamStats.Add(t);
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchQuery = e.NewTextValue ?? string.Empty;
        ApplyFilter();
    }

    private void OnClearSearchClicked(object sender, EventArgs e)
    {
        SearchQuery = string.Empty;
        ApplyFilter();
    }

    private void OnShowPlayersClicked(object sender, EventArgs e)
    {
        _showingPlayers = true;
        OnPropertyChanged(nameof(ShowingPlayers));
        OnPropertyChanged(nameof(ShowingTeams));
        OnPropertyChanged(nameof(PlayerTabColor));
        OnPropertyChanged(nameof(TeamTabColor));
    }

    private void OnShowTeamsClicked(object sender, EventArgs e)
    {
        _showingPlayers = false;
        OnPropertyChanged(nameof(ShowingPlayers));
        OnPropertyChanged(nameof(ShowingTeams));
        OnPropertyChanged(nameof(PlayerTabColor));
        OnPropertyChanged(nameof(TeamTabColor));
    }
}

public class PlayerStatSummary
{
    public int PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public int GamesPlayed { get; set; }
    public int TotalPoints { get; set; }
    public int TotalRebounds { get; set; }
    public int TotalAssists { get; set; }
    public int TotalSteals { get; set; }
    public int TotalBlocks { get; set; }
    public int TotalTurnovers { get; set; }
}

public class TeamStatSummary
{
    public int TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int GamesPlayed { get; set; }
    public int TotalPoints { get; set; }
    public int TotalRebounds { get; set; }
    public int TotalAssists { get; set; }
    public int TotalSteals { get; set; }
}