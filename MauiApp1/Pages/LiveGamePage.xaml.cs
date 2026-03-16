using MauiApp1.Models;
using MauiApp1.Services;
using System.Collections.ObjectModel;

namespace MauiApp1.Pages;

public partial class LiveGamePage : ContentPage
{
    private readonly DatabaseService _db;
    private readonly IDispatcherTimer _autoRefreshTimer;
    private bool _isWorking;

    private int _twoPtMade;
    private int _twoPtMiss;
    private int _threePtMade;
    private int _threePtMiss;
    private int _steals;
    private int _turnovers;
    private int _assists;
    private int _blocks;
    private int _fouls;
    private int _offReb;
    private int _defReb;

    private readonly Stack<Action> _undoStack = new();

    public ObservableCollection<GameItem> Games { get; } = new();
    public ObservableCollection<GamePickerItem> GameOptions { get; } = new();
    public ObservableCollection<PlayerItem> GamePlayers { get; } = new();
    public ObservableCollection<PlayerItem> BenchPlayers { get; } = new();

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
            OnPropertyChanged(nameof(SelectedGameInSession));
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(SelectedGameDisplay));
            OnPropertyChanged(nameof(ShowNoGameInSessionMessage));
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
            OnPropertyChanged(nameof(SelectedGameDisplay));
        }
    }

    private PlayerItem? _selectedPlayer;
    public PlayerItem? SelectedPlayer
    {
        get => _selectedPlayer;
        set
        {
            _selectedPlayer = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PlayerSelected));
            OnPropertyChanged(nameof(SelectedPlayerDisplay));
            ResetSessionStats();
        }
    }

    private PlayerItem? _selectedOnCourtPlayer;
    public PlayerItem? SelectedOnCourtPlayer
    {
        get => _selectedOnCourtPlayer;
        set
        {
            _selectedOnCourtPlayer = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanSubstitute));
        }
    }

    private PlayerItem? _selectedBenchPlayer;
    public PlayerItem? SelectedBenchPlayer
    {
        get => _selectedBenchPlayer;
        set
        {
            _selectedBenchPlayer = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanSubstitute));
        }
    }

    public bool GameSelected => _selectedGame is not null;
    public string SelectedGameDisplay => _selectedGameOption?.DisplayText ?? "No game selected";
    public bool PlayerSelected => _selectedPlayer is not null;
    public bool SelectedGameInSession => _selectedGame is not null && IsGameInSession(_selectedGame.Status);
    public bool CanUndo => _undoStack.Count > 0;
    public bool CanSave => _selectedPlayer is not null && _selectedGame is not null && !IsWorking;
    public bool CanSubstitute => _selectedGame is not null && _selectedOnCourtPlayer is not null && _selectedBenchPlayer is not null && !IsWorking;

    private bool _hasGameInSession;
    public bool HasGameInSession
    {
        get => _hasGameInSession;
        set
        {
            _hasGameInSession = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(NoGameInSession));
            OnPropertyChanged(nameof(ShowNoGameInSessionMessage));
        }
    }

    public bool NoGameInSession => !HasGameInSession;
    public bool ShowNoGameInSessionMessage => NoGameInSession && !GameSelected;

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
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(CanSubstitute));
        }
    }

    private string _statusMessage = "Select a game to begin.";
    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public string SelectedPlayerDisplay => _selectedPlayer is null
        ? string.Empty
        : $"Logging stats for #{_selectedPlayer.JerseyNumber}: {_selectedPlayer.FullName}";

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

    public string SessionStatsDisplay =>
        $"2PT {_twoPtMade}M/{_twoPtMiss}X   3PT {_threePtMade}M/{_threePtMiss}X\n" +
        $"STL {_steals}   AST {_assists}   BLK {_blocks}\n" +
        $"REB {_offReb} OR / {_defReb} DR   TO {_turnovers}   FOUL {_fouls}";

    public LiveGamePage()
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
                ? "No game is currently in session. Select a game to review scores only."
                : "Live game detected. Scores auto-refresh every 5 seconds.";

            if (selectedGameId.HasValue)
            {
                SelectedGameOption = GameOptions.FirstOrDefault(o => o.Game?.GameId == selectedGameId.Value);
            }
            else if (inSessionGame is not null)
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
        if (sender is Picker picker && picker.SelectedItem is GamePickerItem selected)
        {
            // Ensure selection is synchronized even if event fires before binding updates.
            SelectedGameOption = selected;
        }

        if (_selectedGame is null)
        {
            return;
        }

        try
        {
            IsWorking = true;

            var allPlayers = await _db.GetPlayersAsync();
            var homeStarterIds = (await _db.GetTeamStarterIdsAsync(_selectedGame.HomeTeamId)).ToHashSet();
            var awayStarterIds = (await _db.GetTeamStarterIdsAsync(_selectedGame.AwayTeamId)).ToHashSet();

            var eligiblePlayers = allPlayers
                .Where(p => p.TeamId == _selectedGame.HomeTeamId || p.TeamId == _selectedGame.AwayTeamId)
                .OrderBy(p => p.TeamId)
                .ThenBy(p => p.JerseyNumber)
                .ThenBy(p => p.LastName)
                .ToList();

            var configuredStarterIds = homeStarterIds.Union(awayStarterIds).ToHashSet();

            List<PlayerItem> gamePlayers;
            if (configuredStarterIds.Count == 0)
            {
                gamePlayers = eligiblePlayers;
            }
            else
            {
                gamePlayers = eligiblePlayers.Where(p => configuredStarterIds.Contains(p.PlayerId)).ToList();
            }

            var benchPlayers = eligiblePlayers
                .Where(p => !gamePlayers.Any(gp => gp.PlayerId == p.PlayerId))
                .ToList();

            GamePlayers.Clear();
            foreach (var player in gamePlayers)
            {
                GamePlayers.Add(player);
            }

            BenchPlayers.Clear();
            foreach (var player in benchPlayers)
            {
                BenchPlayers.Add(player);
            }

            var teams = await _db.GetTeamsAsync();
            HomeTeamName = teams.FirstOrDefault(t => t.TeamId == _selectedGame.HomeTeamId)?.Name ?? $"Team {_selectedGame.HomeTeamId}";
            AwayTeamName = teams.FirstOrDefault(t => t.TeamId == _selectedGame.AwayTeamId)?.Name ?? $"Team {_selectedGame.AwayTeamId}";

            await RefreshScoresAsync();

            SelectedPlayer = null;
            SelectedOnCourtPlayer = null;
            SelectedBenchPlayer = null;
            var benchNote = benchPlayers.Count > 0 ? $", {benchPlayers.Count} on bench" : string.Empty;
            StatusMessage = $"Game loaded: {gamePlayers.Count} active players{benchNote}.";
        }
        catch (Exception ex)
        {
            StatusMessage = "Could not load game details.";
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsWorking = false;
        }
    }

    private async Task RefreshScoresAsync()
    {
        if (_selectedGame is null)
        {
            return;
        }

        var stats = await _db.GetStatsByGameAsync(_selectedGame.GameId);
        var players = GamePlayers.Concat(BenchPlayers).DistinctBy(p => p.PlayerId).ToList();

        var persistedHome = stats
            .Where(s => players.Any(p => p.PlayerId == s.PlayerId && p.TeamId == _selectedGame.HomeTeamId))
            .Sum(s => s.TotalPoints);

        var persistedAway = stats
            .Where(s => players.Any(p => p.PlayerId == s.PlayerId && p.TeamId == _selectedGame.AwayTeamId))
            .Sum(s => s.TotalPoints);

        // Keep in-memory point taps visible even before Save.
        var pendingPoints = (_twoPtMade * 2) + (_threePtMade * 3);
        if (_selectedPlayer is not null)
        {
            if (_selectedPlayer.TeamId == _selectedGame.HomeTeamId)
            {
                persistedHome += pendingPoints;
            }
            else if (_selectedPlayer.TeamId == _selectedGame.AwayTeamId)
            {
                persistedAway += pendingPoints;
            }
        }

        HomeScore = persistedHome;
        AwayScore = persistedAway;
    }

    private async void OnSubstituteClicked(object sender, EventArgs e)
    {
        if (!CanSubstitute || _selectedOnCourtPlayer is null || _selectedBenchPlayer is null)
        {
            return;
        }

        if (_selectedOnCourtPlayer.TeamId != _selectedBenchPlayer.TeamId)
        {
            await DisplayAlert("Substitution", "Players must be on the same team to substitute.", "OK");
            return;
        }

        var onCourt = _selectedOnCourtPlayer;
        var bench = _selectedBenchPlayer;

        GamePlayers.Remove(onCourt);
        BenchPlayers.Remove(bench);

        InsertSorted(GamePlayers, bench);
        InsertSorted(BenchPlayers, onCourt);

        if (_selectedPlayer?.PlayerId == onCourt.PlayerId)
        {
            SelectedPlayer = null;
        }

        SelectedOnCourtPlayer = null;
        SelectedBenchPlayer = null;

        StatusMessage = $"Substitution: {onCourt.FullName} out, {bench.FullName} in.";
        await RefreshScoresAsync();
    }

    private static void InsertSorted(ObservableCollection<PlayerItem> target, PlayerItem player)
    {
        var index = 0;
        while (index < target.Count)
        {
            var current = target[index];
            var teamCompare = current.TeamId.CompareTo(player.TeamId);
            if (teamCompare > 0)
            {
                break;
            }

            if (teamCompare == 0)
            {
                var currentJersey = current.JerseyNumber ?? int.MaxValue;
                var playerJersey = player.JerseyNumber ?? int.MaxValue;
                var jerseyCompare = currentJersey.CompareTo(playerJersey);
                if (jerseyCompare > 0)
                {
                    break;
                }

                if (jerseyCompare == 0 && string.Compare(current.LastName, player.LastName, StringComparison.OrdinalIgnoreCase) > 0)
                {
                    break;
                }
            }

            index++;
        }

        target.Insert(index, player);
    }

    private void On2ptMadeClicked(object sender, EventArgs e) =>
        LogStat(() =>
        {
            _twoPtMade++;
            UpdateLiveScore(2);
        },
        () =>
        {
            _twoPtMade--;
            UpdateLiveScore(-2);
        },
        "2pt Made");

    private void On2ptMissClicked(object sender, EventArgs e) =>
        LogStat(() => _twoPtMiss++, () => _twoPtMiss--, "2pt Miss");

    private void On3ptMadeClicked(object sender, EventArgs e) =>
        LogStat(() =>
        {
            _threePtMade++;
            UpdateLiveScore(3);
        },
        () =>
        {
            _threePtMade--;
            UpdateLiveScore(-3);
        },
        "3pt Made");

    private void On3ptMissClicked(object sender, EventArgs e) =>
        LogStat(() => _threePtMiss++, () => _threePtMiss--, "3pt Miss");

    private void OnStealClicked(object sender, EventArgs e) => LogStat(() => _steals++, () => _steals--, "Steal");
    private void OnAssistClicked(object sender, EventArgs e) => LogStat(() => _assists++, () => _assists--, "Assist");
    private void OnBlockClicked(object sender, EventArgs e) => LogStat(() => _blocks++, () => _blocks--, "Block");
    private void OnOffRebClicked(object sender, EventArgs e) => LogStat(() => _offReb++, () => _offReb--, "Off Reb");
    private void OnDefRebClicked(object sender, EventArgs e) => LogStat(() => _defReb++, () => _defReb--, "Def Reb");
    private void OnTurnoverClicked(object sender, EventArgs e) => LogStat(() => _turnovers++, () => _turnovers--, "Turnover");
    private void OnFoulClicked(object sender, EventArgs e) => LogStat(() => _fouls++, () => _fouls--, "Foul");

    private void LogStat(Action apply, Action undo, string label)
    {
        if (!PlayerSelected || IsWorking)
        {
            StatusMessage = "Select a player first.";
            return;
        }

        apply();
        _undoStack.Push(undo);
        StatusMessage = $"Logged {label}.";
        OnPropertyChanged(nameof(SessionStatsDisplay));
        OnPropertyChanged(nameof(CanUndo));
    }

    private void UpdateLiveScore(int points)
    {
        if (_selectedPlayer is null || _selectedGame is null)
        {
            return;
        }

        if (_selectedPlayer.TeamId == _selectedGame.HomeTeamId)
        {
            HomeScore += points;
        }
        else
        {
            AwayScore += points;
        }
    }

    private void OnUndoClicked(object sender, EventArgs e)
    {
        if (_undoStack.TryPop(out var undo))
        {
            undo();
            StatusMessage = "Last stat undone.";
            OnPropertyChanged(nameof(SessionStatsDisplay));
            OnPropertyChanged(nameof(CanUndo));
        }
    }

    private async void OnSaveStatsClicked(object sender, EventArgs e)
    {
        if (_selectedPlayer is null || _selectedGame is null)
        {
            return;
        }

        try
        {
            IsWorking = true;

            var gameStats = await _db.GetStatsByGameAsync(_selectedGame.GameId);
            var existing = gameStats.FirstOrDefault(s => s.PlayerId == _selectedPlayer.PlayerId);

            if (existing is null)
            {
                await _db.CreateStatAsync(
                    _selectedGame.GameId,
                    _selectedPlayer.PlayerId,
                    _twoPtMiss,
                    _twoPtMade,
                    _threePtMiss,
                    _threePtMade,
                    _steals,
                    _turnovers,
                    _assists,
                    _blocks,
                    _fouls,
                    _offReb,
                    _defReb);
            }
            else
            {
                await _db.UpdateStatAsync(
                    existing.StatId,
                    existing.GameId,
                    existing.PlayerId,
                    existing.TwoPtMiss + _twoPtMiss,
                    existing.TwoPtMade + _twoPtMade,
                    existing.ThreePtMiss + _threePtMiss,
                    existing.ThreePtMade + _threePtMade,
                    existing.Steals + _steals,
                    existing.Turnovers + _turnovers,
                    existing.Assists + _assists,
                    existing.Blocks + _blocks,
                    existing.Fouls + _fouls,
                    existing.OffensiveRebounds + _offReb,
                    existing.DefensiveRebounds + _defReb);
            }

            var statusToPersist = _selectedGame.Status?.Trim();
            if (string.IsNullOrWhiteSpace(statusToPersist))
            {
                statusToPersist = "In Progress";
            }

            _selectedGame.HomeScore = HomeScore;
            _selectedGame.AwayScore = AwayScore;
            _selectedGame.Status = statusToPersist;

            await _db.UpdateGameAsync(
                _selectedGame.GameId,
                _selectedGame.HomeTeamId,
                _selectedGame.AwayTeamId,
                _selectedGame.GameDate,
                _selectedGame.Location,
                HomeScore,
                AwayScore,
                statusToPersist);

            StatusMessage = $"Saved stats for {_selectedPlayer.FullName}.";
            ResetSessionStats();
            await RefreshScoresAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Save Error", ex.Message, "OK");
        }
        finally
        {
            IsWorking = false;
        }
    }

    private void ResetSessionStats()
    {
        _twoPtMade = 0;
        _twoPtMiss = 0;
        _threePtMade = 0;
        _threePtMiss = 0;
        _steals = 0;
        _turnovers = 0;
        _assists = 0;
        _blocks = 0;
        _fouls = 0;
        _offReb = 0;
        _defReb = 0;
        _undoStack.Clear();

        OnPropertyChanged(nameof(SessionStatsDisplay));
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanSave));
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

        if (SelectedGameInSession)
        {
            await RefreshScoresAsync();
            await LoadGamesAsync();
        }
    }
}
