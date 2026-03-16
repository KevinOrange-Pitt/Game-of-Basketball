using MauiApp1.Models;
using MauiApp1.Services;
using System.Collections.ObjectModel;

namespace MauiApp1;

public partial class GameViewPage : ContentPage
{
    private readonly DatabaseService _db;
    private bool _isWorking;
    private int? _selectedGameId;

    public ObservableCollection<GameItem> Games { get; } = new();
    public ObservableCollection<PlayerStatRow> HomePlayerStats { get; } = new();
    public ObservableCollection<PlayerStatRow> AwayPlayerStats { get; } = new();

    private GameItem? _selectedGame;
    public GameItem? SelectedGame
    {
        get => _selectedGame;
        set
        {
            _selectedGame = value;
            _selectedGameId = value?.GameId;
            OnPropertyChanged();
            OnPropertyChanged(nameof(GameSelected));
        }
    }

    public bool GameSelected => _selectedGame is not null;
    public bool HasStats => HomePlayerStats.Count > 0 || AwayPlayerStats.Count > 0;
    public bool CanRefresh => !IsWorking;

    public bool IsWorking
    {
        get => _isWorking;
        set { _isWorking = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanRefresh)); }
    }

    private string _statusMessage = "Select a game to view.";
    public string StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(); } }

    private string _homeTeamName = "Home";
    public string HomeTeamName { get => _homeTeamName; set { _homeTeamName = value; OnPropertyChanged(); } }

    private string _awayTeamName = "Away";
    public string AwayTeamName { get => _awayTeamName; set { _awayTeamName = value; OnPropertyChanged(); } }

    private int _homeScore;
    public int HomeScore { get => _homeScore; set { _homeScore = value; OnPropertyChanged(); } }

    private int _awayScore;
    public int AwayScore { get => _awayScore; set { _awayScore = value; OnPropertyChanged(); } }

    private string _gameStatus = string.Empty;
    public string GameStatus { get => _gameStatus; set { _gameStatus = value; OnPropertyChanged(); } }

    private string _location = string.Empty;
    public string Location { get => _location; set { _location = value; OnPropertyChanged(); } }

    private string _gameDateDisplay = string.Empty;
    public string GameDateDisplay { get => _gameDateDisplay; set { _gameDateDisplay = value; OnPropertyChanged(); } }

    public GameViewPage()
    {
        _db = new DatabaseService(new HttpClient { BaseAddress = new Uri("http://127.0.0.1:5117/") });
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadGamesAsync();

        if (SelectedGame is not null)
        {
            await LoadGameDataAsync();
        }
    }

    private async Task LoadGamesAsync()
    {
        try
        {
            IsWorking = true;
            var games = await _db.GetGamesAsync();

            // Keep the currently selected game whenever we refresh game list data.
            var selectedGameId = _selectedGameId;

            Games.Clear();
            foreach (var g in games) Games.Add(g);

            if (selectedGameId.HasValue)
            {
                SelectedGame = Games.FirstOrDefault(g => g.GameId == selectedGameId.Value);
            }

            StatusMessage = $"Loaded {games.Count} games.";
        }
        catch (Exception ex)
        {
            StatusMessage = "Could not load games.";
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally { IsWorking = false; }
    }

    private async void OnGamePickerChanged(object sender, EventArgs e)
    {
        if (_selectedGame is null) return;
        await LoadGameDataAsync();
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        await LoadGamesAsync();
        if (_selectedGame is not null)
        {
            await LoadGameDataAsync();
        }
    }

    private async Task LoadGameDataAsync()
    {
        if (_selectedGame is null) return;

        try
        {
            IsWorking = true;

            // Pull latest game snapshot so status, date, and persisted scores do not stay stale.
            var games = await _db.GetGamesAsync();
            var latestGame = games.FirstOrDefault(g => g.GameId == _selectedGame.GameId);
            if (latestGame is not null)
            {
                _selectedGame = latestGame;
                _selectedGameId = latestGame.GameId;
                OnPropertyChanged(nameof(SelectedGame));
            }

            var teams = await _db.GetTeamsAsync();
            var allPlayers = await _db.GetPlayersAsync();
            var stats = await _db.GetStatsByGameAsync(_selectedGame.GameId);

            var homeTeam = teams.FirstOrDefault(t => t.TeamId == _selectedGame.HomeTeamId);
            var awayTeam = teams.FirstOrDefault(t => t.TeamId == _selectedGame.AwayTeamId);

            HomeTeamName = homeTeam?.Name ?? $"Team {_selectedGame.HomeTeamId}";
            AwayTeamName = awayTeam?.Name ?? $"Team {_selectedGame.AwayTeamId}";
            GameStatus = _selectedGame.Status;
            Location = _selectedGame.Location;
            GameDateDisplay = _selectedGame.GameDate.ToString("MMM dd, yyyy  h:mm tt");

            // Build stat rows per player
            var homePlayers = allPlayers.Where(p => p.TeamId == _selectedGame.HomeTeamId).ToList();
            var awayPlayers = allPlayers.Where(p => p.TeamId == _selectedGame.AwayTeamId).ToList();

            HomePlayerStats.Clear();
            AwayPlayerStats.Clear();

            foreach (var p in homePlayers)
            {
                var s = stats.FirstOrDefault(x => x.PlayerId == p.PlayerId);
                HomePlayerStats.Add(PlayerStatRow.From(p, s));
            }

            foreach (var p in awayPlayers)
            {
                var s = stats.FirstOrDefault(x => x.PlayerId == p.PlayerId);
                AwayPlayerStats.Add(PlayerStatRow.From(p, s));
            }

            // Always derive the board score from latest stat rows for immediate in-app sync.
            HomeScore = HomePlayerStats.Sum(r => r.Points);
            AwayScore = AwayPlayerStats.Sum(r => r.Points);

            OnPropertyChanged(nameof(HasStats));
            StatusMessage = $"Showing {stats.Count} stat records for this game.";
        }
        catch (Exception ex)
        {
            StatusMessage = "Could not load game data.";
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally { IsWorking = false; }
    }
}

public class PlayerStatRow
{
    public string PlayerName { get; set; } = string.Empty;
    public int Points { get; set; }
    public int Rebounds { get; set; }
    public int Assists { get; set; }
    public int Steals { get; set; }
    public int Blocks { get; set; }
    public int Turnovers { get; set; }

    public static PlayerStatRow From(PlayerItem p, StatItem? s) => new()
    {
        PlayerName = $"#{p.JerseyNumber} {p.FullName}",
        Points = s?.TotalPoints ?? 0,
        Rebounds = s?.TotalRebounds ?? 0,
        Assists = s?.Assists ?? 0,
        Steals = s?.Steals ?? 0,
        Blocks = s?.Blocks ?? 0,
        Turnovers = s?.Turnovers ?? 0
    };
}