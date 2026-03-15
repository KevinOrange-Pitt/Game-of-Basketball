using MauiApp1.Models;
using MauiApp1.Services;
using System.Collections.ObjectModel;

namespace MauiApp1;

public partial class ScoreKeepingPage : ContentPage
{
    private readonly DatabaseService _db;
    private bool _isWorking;

    // Stat counters for current session
    private int _twoPtMade, _twoPtMiss, _threePtMade, _threePtMiss;
    private int _steals, _turnovers, _assists, _blocks, _fouls;
    private int _offReb, _defReb;

    // Undo stack — each entry is a lambda that decrements the right counter
    private readonly Stack<Action> _undoStack = new();

    public ObservableCollection<GameItem> Games { get; } = new();
    public ObservableCollection<PlayerItem> GamePlayers { get; } = new();

    private GameItem? _selectedGame;
    public GameItem? SelectedGame
    {
        get => _selectedGame;
        set { _selectedGame = value; OnPropertyChanged(); OnPropertyChanged(nameof(GameSelected)); }
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

    public bool GameSelected => _selectedGame is not null;
    public bool PlayerSelected => _selectedPlayer is not null;
    public bool CanUndo => _undoStack.Count > 0;
    public bool CanSave => _selectedPlayer is not null && !IsWorking;

    public bool IsWorking
    {
        get => _isWorking;
        set { _isWorking = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanSave)); }
    }

    private string _statusMessage = "Select a game to begin.";
    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    public string SelectedPlayerDisplay => _selectedPlayer is null
        ? string.Empty
        : $"Logging stats for: #{_selectedPlayer.JerseyNumber} {_selectedPlayer.FullName}";

    private int _homeScore;
    public int HomeScore { get => _homeScore; set { _homeScore = value; OnPropertyChanged(); } }

    private int _awayScore;
    public int AwayScore { get => _awayScore; set { _awayScore = value; OnPropertyChanged(); } }

    private string _homeTeamName = "Home";
    public string HomeTeamName { get => _homeTeamName; set { _homeTeamName = value; OnPropertyChanged(); } }

    private string _awayTeamName = "Away";
    public string AwayTeamName { get => _awayTeamName; set { _awayTeamName = value; OnPropertyChanged(); } }

    public string SessionStatsDisplay =>
        $"2pt: {_twoPtMade}M / {_twoPtMiss}X   3pt: {_threePtMade}M / {_threePtMiss}X\n" +
        $"Steals: {_steals}   Assists: {_assists}   Blocks: {_blocks}\n" +
        $"Reb: {_offReb} Off / {_defReb} Def   TO: {_turnovers}   Fouls: {_fouls}";

    public ScoreKeepingPage()
    {
        _db = new DatabaseService(new HttpClient { BaseAddress = new Uri("http://127.0.0.1:5117/") });
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
            var games = await _db.GetGamesAsync();
            Games.Clear();
            foreach (var g in games) Games.Add(g);
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

        try
        {
            IsWorking = true;
            var allPlayers = await _db.GetPlayersAsync();
            var gamePlayers = allPlayers
                .Where(p => p.TeamId == _selectedGame.HomeTeamId || p.TeamId == _selectedGame.AwayTeamId)
                .ToList();

            GamePlayers.Clear();
            foreach (var p in gamePlayers) GamePlayers.Add(p);

            // Load team names
            var teams = await _db.GetTeamsAsync();
            HomeTeamName = teams.FirstOrDefault(t => t.TeamId == _selectedGame.HomeTeamId)?.Name ?? $"Team {_selectedGame.HomeTeamId}";
            AwayTeamName = teams.FirstOrDefault(t => t.TeamId == _selectedGame.AwayTeamId)?.Name ?? $"Team {_selectedGame.AwayTeamId}";

            // Load existing scores from stats
            await RefreshScoresAsync();

            SelectedPlayer = null;
            StatusMessage = $"Game loaded — {gamePlayers.Count} players available.";
        }
        catch (Exception ex)
        {
            StatusMessage = "Could not load game data.";
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally { IsWorking = false; }
    }

    private async Task RefreshScoresAsync()
    {
        if (_selectedGame is null) return;
        var stats = await _db.GetStatsByGameAsync(_selectedGame.GameId);

        HomeScore = stats
            .Where(s => GamePlayers.Any(p => p.PlayerId == s.PlayerId && p.TeamId == _selectedGame.HomeTeamId))
            .Sum(s => s.TotalPoints);

        AwayScore = stats
            .Where(s => GamePlayers.Any(p => p.PlayerId == s.PlayerId && p.TeamId == _selectedGame.AwayTeamId))
            .Sum(s => s.TotalPoints);
    }

    // ── Stat button handlers 

    private void On2ptMadeClicked(object s, EventArgs e)  => LogStat(() => { _twoPtMade++;  HomeOrAwayScore(2); }, () => { _twoPtMade--;  HomeOrAwayScore(-2); }, "2pt Made");
    private void On2ptMissClicked(object s, EventArgs e)  => LogStat(() => _twoPtMiss++,  () => _twoPtMiss--,  "2pt Miss");
    private void On3ptMadeClicked(object s, EventArgs e)  => LogStat(() => { _threePtMade++; HomeOrAwayScore(3); }, () => { _threePtMade--; HomeOrAwayScore(-3); }, "3pt Made");
    private void On3ptMissClicked(object s, EventArgs e)  => LogStat(() => _threePtMiss++, () => _threePtMiss--, "3pt Miss");
    private void OnStealClicked(object s, EventArgs e)    => LogStat(() => _steals++,     () => _steals--,     "Steal");
    private void OnAssistClicked(object s, EventArgs e)   => LogStat(() => _assists++,    () => _assists--,    "Assist");
    private void OnBlockClicked(object s, EventArgs e)    => LogStat(() => _blocks++,     () => _blocks--,     "Block");
    private void OnOffRebClicked(object s, EventArgs e)   => LogStat(() => _offReb++,     () => _offReb--,     "Off Reb");
    private void OnDefRebClicked(object s, EventArgs e)   => LogStat(() => _defReb++,     () => _defReb--,     "Def Reb");
    private void OnTurnoverClicked(object s, EventArgs e) => LogStat(() => _turnovers++,  () => _turnovers--,  "Turnover");
    private void OnFoulClicked(object s, EventArgs e)     => LogStat(() => _fouls++,      () => _fouls--,      "Foul");

    private void LogStat(Action apply, Action undo, string label)
    {
        apply();
        _undoStack.Push(undo);
        StatusMessage = $"Logged: {label}";
        OnPropertyChanged(nameof(SessionStatsDisplay));
        OnPropertyChanged(nameof(CanUndo));
    }

    private void HomeOrAwayScore(int pts)
    {
        if (_selectedPlayer is null || _selectedGame is null) return;
        if (_selectedPlayer.TeamId == _selectedGame.HomeTeamId) HomeScore += pts;
        else AwayScore += pts;
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
        if (_selectedPlayer is null || _selectedGame is null) return;

        try
        {
            IsWorking = true;
            await _db.CreateStatAsync(
                _selectedGame.GameId, _selectedPlayer.PlayerId,
                _twoPtMiss, _twoPtMade, _threePtMiss, _threePtMade,
                _steals, _turnovers, _assists, _blocks, _fouls,
                _offReb, _defReb);

            // Update game scores in the database
            await _db.UpdateGameAsync(
                _selectedGame.GameId,
                _selectedGame.HomeTeamId, _selectedGame.AwayTeamId,
                _selectedGame.GameDate, _selectedGame.Location,
                HomeScore, AwayScore, "In Progress");

            StatusMessage = $"Stats saved for {_selectedPlayer.FullName}!";
            ResetSessionStats();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Save Error", ex.Message, "OK");
        }
        finally { IsWorking = false; }
    }

    private void ResetSessionStats()
    {
        _twoPtMade = _twoPtMiss = _threePtMade = _threePtMiss = 0;
        _steals = _turnovers = _assists = _blocks = _fouls = 0;
        _offReb = _defReb = 0;
        _undoStack.Clear();
        OnPropertyChanged(nameof(SessionStatsDisplay));
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanSave));
    }
}