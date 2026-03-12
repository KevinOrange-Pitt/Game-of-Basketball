using MauiApp1.Models;
using MauiApp1.Services;
using System.Collections.ObjectModel;

namespace MauiApp1.Pages;

public partial class StatsPage : ContentPage
{
    private readonly DatabaseService _db;
    private bool _isWorking;

    public ObservableCollection<GameItem> Games { get; } = new();
    public ObservableCollection<GamePickerItem> GameOptions { get; } = new();
    public ObservableCollection<PlayerStatRow> HomePlayerStats { get; } = new();
    public ObservableCollection<PlayerStatRow> AwayPlayerStats { get; } = new();

    private GameItem? _selectedGame;
    public GameItem? SelectedGame
    {
        get => _selectedGame;
        set
        {
            _selectedGame = value;
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

        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadGamesAsync();
    }

    private async Task LoadGamesAsync()
    {
        try
        {
            IsWorking = true;
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

            HomePlayerStats.Clear();
            AwayPlayerStats.Clear();

            foreach (var player in homePlayers)
            {
                var stat = stats.FirstOrDefault(x => x.PlayerId == player.PlayerId);
                HomePlayerStats.Add(PlayerStatRow.From(player, stat));
            }

            foreach (var player in awayPlayers)
            {
                var stat = stats.FirstOrDefault(x => x.PlayerId == player.PlayerId);
                AwayPlayerStats.Add(PlayerStatRow.From(player, stat));
            }

            HomeScore = _selectedGame.HomeScore ?? HomePlayerStats.Sum(r => r.Points);
            AwayScore = _selectedGame.AwayScore ?? AwayPlayerStats.Sum(r => r.Points);

            OnPropertyChanged(nameof(HasStats));
            StatusMessage = $"Showing {stats.Count} stat records for this game.";
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

    public static PlayerStatRow From(PlayerItem player, StatItem? stat) => new()
    {
        PlayerName = $"#{player.JerseyNumber} {player.FullName}",
        Points = stat?.TotalPoints ?? 0,
        Rebounds = (stat?.OffensiveRebounds ?? 0) + (stat?.DefensiveRebounds ?? 0),
        Assists = stat?.Assists ?? 0,
        Steals = stat?.Steals ?? 0,
        Blocks = stat?.Blocks ?? 0,
        Turnovers = stat?.Turnovers ?? 0
    };
}
