using MauiApp1.Models;
using MauiApp1.Services;
using System.Collections.ObjectModel;

namespace MauiApp1.Pages;

public partial class StatsPage : ContentPage
{
    private readonly DatabaseService _db;
    private readonly IDispatcherTimer _autoRefreshTimer;
    private bool _isWorking;
    private readonly List<PlayerStatRow> _allHomeRows = new();
    private readonly List<PlayerStatRow> _allAwayRows = new();

    public ObservableCollection<GameItem> Games { get; } = new();
    public ObservableCollection<GamePickerItem> GameOptions { get; } = new();
    public ObservableCollection<PlayerStatRow> HomePlayerStats { get; } = new();
    public ObservableCollection<PlayerStatRow> AwayPlayerStats { get; } = new();

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            _searchText = value ?? string.Empty;
            OnPropertyChanged();
            ApplySearchFilter();
        }
    }

    private GameItem? _selectedGame;
    public GameItem? SelectedGame
    {
        get => _selectedGame;
        set
        {
            _selectedGame = value;
            GameSelectionState.SetSelectedGame(value?.GameId);
            OnPropertyChanged();
            OnPropertyChanged(nameof(GameSelected));
        }
    }

    private GamePickerItem? _selectedGameOption;
    public GamePickerItem? SelectedGameOption
    {
        get => _selectedGameOption;
        set
        {
            _selectedGameOption = value;
            SelectedGame = value?.Game;
            OnPropertyChanged();
        }
    }

    public bool GameSelected => _selectedGame is not null;
    public bool HasStats => HomePlayerStats.Count > 0 || AwayPlayerStats.Count > 0;
    public bool CanRefresh => !IsWorking;

    private bool _hasGameInSession;
    public bool HasGameInSession
    {
        get => _hasGameInSession;
        set
        {
            _hasGameInSession = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(NoGameInSession));
        }
    }

    public bool NoGameInSession => !HasGameInSession;

    private string _liveStateMessage = "Checking live game status...";
    public string LiveStateMessage
    {
        get => _liveStateMessage;
        set
        {
            _liveStateMessage = value;
            OnPropertyChanged();
        }
    }

    public bool IsWorking
    {
        get => _isWorking;
        set
        {
            _isWorking = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanRefresh));
        }
    }

    private string _statusMessage = "Select a game to view stats.";
    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    private string _homeTeamName = "Home";
    public string HomeTeamName
    {
        get => _homeTeamName;
        set
        {
            _homeTeamName = value;
            OnPropertyChanged();
        }
    }

    private string _awayTeamName = "Away";
    public string AwayTeamName
    {
        get => _awayTeamName;
        set
        {
            _awayTeamName = value;
            OnPropertyChanged();
        }
    }

    private int _homeScore;
    public int HomeScore
    {
        get => _homeScore;
        set
        {
            _homeScore = value;
            OnPropertyChanged();
        }
    }

    private int _awayScore;
    public int AwayScore
    {
        get => _awayScore;
        set
        {
            _awayScore = value;
            OnPropertyChanged();
        }
    }

    private string _gameStatus = string.Empty;
    public string GameStatus
    {
        get => _gameStatus;
        set
        {
            _gameStatus = value;
            OnPropertyChanged();
        }
    }

    private string _location = string.Empty;
    public string Location
    {
        get => _location;
        set
        {
            _location = value;
            OnPropertyChanged();
        }
    }

    private string _gameDateDisplay = string.Empty;
    public string GameDateDisplay
    {
        get => _gameDateDisplay;
        set
        {
            _gameDateDisplay = value;
            OnPropertyChanged();
        }
    }

    public StatsPage()
    {
        _db = new DatabaseService(new HttpClient
        {
            BaseAddress = new Uri("http://127.0.0.1:5117/")
        });

        _autoRefreshTimer = Dispatcher.CreateTimer();
        _autoRefreshTimer.Interval = TimeSpan.FromSeconds(5);
        _autoRefreshTimer.Tick += OnAutoRefreshTick;

        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!_autoRefreshTimer.IsRunning)
        {
            _autoRefreshTimer.Start();
        }

        await LoadGamesAsync();
    }

    protected override void OnDisappearing()
    {
        if (_autoRefreshTimer.IsRunning)
        {
            _autoRefreshTimer.Stop();
        }

        base.OnDisappearing();
    }

    private async Task LoadGamesAsync()
    {
        try
        {
            IsWorking = true;
            var selectedGameId = SelectedGame?.GameId ?? GameSelectionState.SelectedGameId;
            var teams = await _db.GetTeamsAsync();
            var games = await _db.GetGamesAsync();

            var teamNameById = teams.ToDictionary(t => t.TeamId, t => t.Name);

            Games.Clear();
            GameOptions.Clear();
            foreach (var game in games.OrderByDescending(g => g.GameDate))
            {
                Games.Add(game);

                var homeName = teamNameById.TryGetValue(game.HomeTeamId, out var homeTeamName)
                    ? homeTeamName
                    : $"Team {game.HomeTeamId}";
                var awayName = teamNameById.TryGetValue(game.AwayTeamId, out var awayTeamName)
                    ? awayTeamName
                    : $"Team {game.AwayTeamId}";

                GameOptions.Add(new GamePickerItem
                {
                    Game = game,
                    DisplayText = BuildGameOptionLabel(homeName, awayName, game.GameDate)
                });
            }

            var inSessionGame = games
                .OrderByDescending(g => g.GameDate)
                .FirstOrDefault(g => IsGameInSession(g.Status));

            HasGameInSession = inSessionGame is not null;
            LiveStateMessage = inSessionGame is null
                ? "No game is currently in session. You can still review saved game stats."
                : "Live game detected. Stats auto-refresh every 5 seconds.";

            if (selectedGameId.HasValue)
            {
                SelectedGameOption = GameOptions.FirstOrDefault(o => o.Game?.GameId == selectedGameId.Value);
            }
            else if (SelectedGame is null && inSessionGame is not null)
            {
                SelectedGameOption = GameOptions.FirstOrDefault(o => o.Game?.GameId == inSessionGame.GameId);
            }

            StatusMessage = $"Loaded {games.Count} games.";
        }
        catch (Exception ex)
        {
            StatusMessage = "Could not load games.";
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsWorking = false;
        }
    }

    private static string BuildGameOptionLabel(string homeName, string awayName, DateTime gameDate)
    {
        var home = ShortenName(homeName, 12);
        var away = ShortenName(awayName, 12);
        return $"{home} vs {away} {gameDate:M/d}";
    }

    private static string ShortenName(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private async void OnGamePickerChanged(object sender, EventArgs e)
    {
        if (_selectedGame is null)
        {
            return;
        }

        await LoadGameDataAsync();
    }

    private void OnSearchChanged(object sender, TextChangedEventArgs e)
    {
        SearchText = e.NewTextValue ?? string.Empty;
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        if (_selectedGame is null)
        {
            await LoadGamesAsync();
            return;
        }

        await LoadGameDataAsync();
    }

    private async Task LoadGameDataAsync()
    {
        if (_selectedGame is null)
        {
            return;
        }

        try
        {
            IsWorking = true;

            var teams = await _db.GetTeamsAsync();
            var allPlayers = await _db.GetPlayersAsync();
            var stats = await _db.GetStatsByGameAsync(_selectedGame.GameId);

            var homeTeam = teams.FirstOrDefault(t => t.TeamId == _selectedGame.HomeTeamId);
            var awayTeam = teams.FirstOrDefault(t => t.TeamId == _selectedGame.AwayTeamId);

            HomeTeamName = homeTeam?.Name ?? $"Team {_selectedGame.HomeTeamId}";
            AwayTeamName = awayTeam?.Name ?? $"Team {_selectedGame.AwayTeamId}";
            GameStatus = _selectedGame.Status;
            Location = _selectedGame.Location;
            GameDateDisplay = _selectedGame.GameDate.ToString("MMM dd, yyyy h:mm tt");

            var homePlayers = allPlayers.Where(p => p.TeamId == _selectedGame.HomeTeamId).OrderBy(p => p.JerseyNumber).ThenBy(p => p.LastName).ToList();
            var awayPlayers = allPlayers.Where(p => p.TeamId == _selectedGame.AwayTeamId).OrderBy(p => p.JerseyNumber).ThenBy(p => p.LastName).ToList();

            _allHomeRows.Clear();
            _allAwayRows.Clear();

            foreach (var player in homePlayers)
            {
                var stat = stats.FirstOrDefault(x => x.PlayerId == player.PlayerId);
                _allHomeRows.Add(PlayerStatRow.From(player, stat, HomeTeamName));
            }

            foreach (var player in awayPlayers)
            {
                var stat = stats.FirstOrDefault(x => x.PlayerId == player.PlayerId);
                _allAwayRows.Add(PlayerStatRow.From(player, stat, AwayTeamName));
            }

            ApplySearchFilter();

            HomeScore = _selectedGame.HomeScore ?? HomePlayerStats.Sum(r => r.Points);
            AwayScore = _selectedGame.AwayScore ?? AwayPlayerStats.Sum(r => r.Points);

            OnPropertyChanged(nameof(HasStats));

            var liveNote = IsGameInSession(_selectedGame.Status)
                ? "(live auto-refresh active)"
                : "(not in session, showing latest saved data)";
            StatusMessage = $"Showing {stats.Count} stat records for this game {liveNote}.";
        }
        catch (Exception ex)
        {
            StatusMessage = "Could not load game stats.";
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsWorking = false;
        }
    }

    private void ApplySearchFilter()
    {
        var query = SearchText.Trim();

        var homeRows = _allHomeRows
            .Where(r => MatchesSearch(r, query))
            .ToList();
        var awayRows = _allAwayRows
            .Where(r => MatchesSearch(r, query))
            .ToList();

        HomePlayerStats.Clear();
        AwayPlayerStats.Clear();

        foreach (var row in homeRows)
        {
            HomePlayerStats.Add(row);
        }

        foreach (var row in awayRows)
        {
            AwayPlayerStats.Add(row);
        }

        OnPropertyChanged(nameof(HasStats));
    }

    private static bool MatchesSearch(PlayerStatRow row, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        return row.PlayerName.Contains(query, StringComparison.OrdinalIgnoreCase)
            || row.TeamName.Contains(query, StringComparison.OrdinalIgnoreCase)
            || row.Points.ToString().Contains(query, StringComparison.OrdinalIgnoreCase)
            || row.Rebounds.ToString().Contains(query, StringComparison.OrdinalIgnoreCase)
            || row.Assists.ToString().Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsGameInSession(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return false;
        }

        var normalized = status.Trim();
        return normalized.Equals("InProgress", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("In Progress", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Live", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("In Session", StringComparison.OrdinalIgnoreCase);
    }

    private async void OnAutoRefreshTick(object? sender, EventArgs e)
    {
        if (IsWorking)
        {
            return;
        }

        if (SelectedGame is null)
        {
            await LoadGamesAsync();
            return;
        }

        await LoadGameDataAsync();
    }
}

public class PlayerStatRow
{
    public string PlayerName { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public int Points { get; set; }
    public int Rebounds { get; set; }
    public int Assists { get; set; }
    public int Steals { get; set; }
    public int Blocks { get; set; }
    public int Turnovers { get; set; }

    public static PlayerStatRow From(PlayerItem player, StatItem? stat, string teamName) => new()
    {
        PlayerName = $"#{player.JerseyNumber} {player.FullName}",
        TeamName = teamName,
        Points = stat?.TotalPoints ?? 0,
        Rebounds = (stat?.OffensiveRebounds ?? 0) + (stat?.DefensiveRebounds ?? 0),
        Assists = stat?.Assists ?? 0,
        Steals = stat?.Steals ?? 0,
        Blocks = stat?.Blocks ?? 0,
        Turnovers = stat?.Turnovers ?? 0
    };
}
