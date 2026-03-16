using MauiApp1.Models;
using MauiApp1.Services;
using System.Collections.ObjectModel;

namespace MauiApp1.Pages;

public partial class GamesPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private GameItem? _selectedGame;

    public ObservableCollection<GameItem> Games { get; } = new();
    public ObservableCollection<GameListRow> GameRows { get; } = new();
    public ObservableCollection<TeamItem> Teams { get; } = new();

    public string GameHomeTeamIdInput { get; set; } = string.Empty;
    public string GameAwayTeamIdInput { get; set; } = string.Empty;
    public string GameDateInput { get; set; } = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd HH:mm");
    public string GameLocationInput { get; set; } = string.Empty;
    public string GameHomeScoreInput { get; set; } = string.Empty;
    public string GameAwayScoreInput { get; set; } = string.Empty;
    public string GameStatusInput { get; set; } = "Scheduled";
    public string SelectedGameLabel { get; set; } = "Selected Game Id: none";

    private TeamItem? _selectedHomeTeam;
    public TeamItem? SelectedHomeTeam
    {
        get => _selectedHomeTeam;
        set
        {
            _selectedHomeTeam = value;
            if (value is not null)
            {
                GameHomeTeamIdInput = value.TeamId.ToString();
                OnPropertyChanged(nameof(GameHomeTeamIdInput));
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedHomeTeamNameDisplay));
            OnPropertyChanged(nameof(SelectedHomeTeamIdDisplay));
        }
    }

    private TeamItem? _selectedAwayTeam;
    public TeamItem? SelectedAwayTeam
    {
        get => _selectedAwayTeam;
        set
        {
            _selectedAwayTeam = value;
            if (value is not null)
            {
                GameAwayTeamIdInput = value.TeamId.ToString();
                OnPropertyChanged(nameof(GameAwayTeamIdInput));
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedAwayTeamNameDisplay));
            OnPropertyChanged(nameof(SelectedAwayTeamIdDisplay));
        }
    }

    public string SelectedHomeTeamNameDisplay => _selectedHomeTeam?.Name ?? "No home team selected";
    public string SelectedHomeTeamIdDisplay => _selectedHomeTeam is null ? "Id: --" : $"Id: {_selectedHomeTeam.TeamId}";
    public string SelectedAwayTeamNameDisplay => _selectedAwayTeam?.Name ?? "No away team selected";
    public string SelectedAwayTeamIdDisplay => _selectedAwayTeam is null ? "Id: --" : $"Id: {_selectedAwayTeam.TeamId}";

    public GamesPage()
    {
        InitializeComponent();
        BindingContext = this;

        _databaseService = new DatabaseService(new HttpClient
        {
            BaseAddress = new Uri("http://127.0.0.1:5117/")
        });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadReferenceDataAsync();
    }

    private async Task LoadReferenceDataAsync()
    {
        try
        {
            var teams = await _databaseService.GetTeamsAsync();
            Teams.Clear();
            foreach (var team in teams.OrderBy(t => t.TeamId))
            {
                Teams.Add(team);
            }

            SyncSelectedTeamsFromInputs();

            var teamNameById = teams.ToDictionary(t => t.TeamId, t => t.Name);

            var games = await _databaseService.GetGamesAsync();
            Games.Clear();
            GameRows.Clear();
            foreach (var game in games.OrderByDescending(g => g.GameDate))
            {
                Games.Add(game);

                var homeName = teamNameById.TryGetValue(game.HomeTeamId, out var homeTeamName)
                    ? homeTeamName
                    : $"Team {game.HomeTeamId}";

                var awayName = teamNameById.TryGetValue(game.AwayTeamId, out var awayTeamName)
                    ? awayTeamName
                    : $"Team {game.AwayTeamId}";

                GameRows.Add(new GameListRow
                {
                    Game = game,
                    MatchupText = $"{homeName} vs {awayName}",
                    DateText = game.GameDate.ToString("MMM d, yyyy h:mm tt"),
                    Status = game.Status,
                    GameId = game.GameId
                });
            }
        }
        catch
        {
            await DisplayAlert("API Error", "Could not load games. Start API and try again.", "OK");
        }
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        await LoadReferenceDataAsync();
    }

    private async void OnCreateGameClicked(object sender, EventArgs e)
    {
        if (!TryReadGameInput(out var homeTeamId, out var awayTeamId, out var gameDate, out var location, out var homeScore, out var awayScore, out var status))
        {
            return;
        }

        try
        {
            await _databaseService.CreateGameAsync(homeTeamId, awayTeamId, gameDate, location, homeScore, awayScore, status);
            ClearGameForm();
            await LoadReferenceDataAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Create Game Failed", ex.Message, "OK");
        }
    }

    private async void OnUpdateGameClicked(object sender, EventArgs e)
    {
        if (_selectedGame is null)
        {
            await DisplayAlert("Update Game", "Select a game first.", "OK");
            return;
        }

        if (!TryReadGameInput(out var homeTeamId, out var awayTeamId, out var gameDate, out var location, out var homeScore, out var awayScore, out var status))
        {
            return;
        }

        try
        {
            await _databaseService.UpdateGameAsync(_selectedGame.GameId, homeTeamId, awayTeamId, gameDate, location, homeScore, awayScore, status);
            await LoadReferenceDataAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Update Game Failed", ex.Message, "OK");
        }
    }

    private async void OnDeleteGameClicked(object sender, EventArgs e)
    {
        if (_selectedGame is null)
        {
            await DisplayAlert("Delete Game", "Select a game first.", "OK");
            return;
        }

        var confirm = await DisplayAlert("Delete Game", $"Delete game Id {_selectedGame.GameId}?", "Delete", "Cancel");
        if (!confirm)
        {
            return;
        }

        try
        {
            await _databaseService.DeleteGameAsync(_selectedGame.GameId);
            _selectedGame = null;
            ClearGameForm();
            await LoadReferenceDataAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Delete Game Failed", ex.Message, "OK");
        }
    }

    private void OnClearGameClicked(object sender, EventArgs e)
    {
        _selectedGame = null;
        ClearGameForm();
    }

    private void OnGameSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not GameListRow row)
        {
            return;
        }

        var game = row.Game;
        if (game is null)
        {
            return;
        }

        _selectedGame = game;
        GameHomeTeamIdInput = game.HomeTeamId.ToString();
        GameAwayTeamIdInput = game.AwayTeamId.ToString();
        GameDateInput = game.GameDate.ToString("yyyy-MM-dd HH:mm");
        GameLocationInput = game.Location;
        GameHomeScoreInput = game.HomeScore?.ToString() ?? string.Empty;
        GameAwayScoreInput = game.AwayScore?.ToString() ?? string.Empty;
        GameStatusInput = game.Status;
        SelectedGameLabel = $"Selected Game Id: {game.GameId}";
        SyncSelectedTeamsFromInputs();

        OnPropertyChanged(nameof(GameHomeTeamIdInput));
        OnPropertyChanged(nameof(GameAwayTeamIdInput));
        OnPropertyChanged(nameof(GameDateInput));
        OnPropertyChanged(nameof(GameLocationInput));
        OnPropertyChanged(nameof(GameHomeScoreInput));
        OnPropertyChanged(nameof(GameAwayScoreInput));
        OnPropertyChanged(nameof(GameStatusInput));
        OnPropertyChanged(nameof(SelectedGameLabel));
    }

    private bool TryReadGameInput(
        out int homeTeamId,
        out int awayTeamId,
        out DateTime gameDate,
        out string location,
        out int? homeScore,
        out int? awayScore,
        out string status)
    {
        homeTeamId = 0;
        awayTeamId = 0;
        gameDate = DateTime.Now;
        location = GameLocationInput.Trim();
        homeScore = null;
        awayScore = null;
        status = GameStatusInput.Trim();

        if (!int.TryParse(GameHomeTeamIdInput, out homeTeamId) || homeTeamId <= 0)
        {
            _ = DisplayAlert("Validation", "Home Team Id must be a number greater than zero.", "OK");
            return false;
        }

        if (!int.TryParse(GameAwayTeamIdInput, out awayTeamId) || awayTeamId <= 0)
        {
            _ = DisplayAlert("Validation", "Away Team Id must be a number greater than zero.", "OK");
            return false;
        }

        if (homeTeamId == awayTeamId)
        {
            _ = DisplayAlert("Validation", "Home and Away team must be different.", "OK");
            return false;
        }

        var homeTeamIdValue = homeTeamId;
        var awayTeamIdValue = awayTeamId;

        if (Teams.Count > 0 && !Teams.Any(t => t.TeamId == homeTeamIdValue))
        {
            _ = DisplayAlert("Validation", "Home Team Id was not found.", "OK");
            return false;
        }

        if (Teams.Count > 0 && !Teams.Any(t => t.TeamId == awayTeamIdValue))
        {
            _ = DisplayAlert("Validation", "Away Team Id was not found.", "OK");
            return false;
        }

        if (!DateTime.TryParse(GameDateInput, out gameDate))
        {
            _ = DisplayAlert("Validation", "Game date must be valid (example: 2026-03-20 19:30).", "OK");
            return false;
        }

        if (string.IsNullOrWhiteSpace(location))
        {
            _ = DisplayAlert("Validation", "Location is required.", "OK");
            return false;
        }

        if (string.IsNullOrWhiteSpace(status))
        {
            _ = DisplayAlert("Validation", "Status is required.", "OK");
            return false;
        }

        if (!string.IsNullOrWhiteSpace(GameHomeScoreInput))
        {
            if (!int.TryParse(GameHomeScoreInput, out var parsedHomeScore) || parsedHomeScore < 0)
            {
                _ = DisplayAlert("Validation", "Home score must be a non-negative number.", "OK");
                return false;
            }

            homeScore = parsedHomeScore;
        }

        if (!string.IsNullOrWhiteSpace(GameAwayScoreInput))
        {
            if (!int.TryParse(GameAwayScoreInput, out var parsedAwayScore) || parsedAwayScore < 0)
            {
                _ = DisplayAlert("Validation", "Away score must be a non-negative number.", "OK");
                return false;
            }

            awayScore = parsedAwayScore;
        }

        return true;
    }

    private void ClearGameForm()
    {
        GameHomeTeamIdInput = string.Empty;
        GameAwayTeamIdInput = string.Empty;
        GameDateInput = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd HH:mm");
        GameLocationInput = string.Empty;
        GameHomeScoreInput = string.Empty;
        GameAwayScoreInput = string.Empty;
        GameStatusInput = "Scheduled";
        SelectedGameLabel = "Selected Game Id: none";
        SelectedHomeTeam = null;
        SelectedAwayTeam = null;

        OnPropertyChanged(nameof(GameHomeTeamIdInput));
        OnPropertyChanged(nameof(GameAwayTeamIdInput));
        OnPropertyChanged(nameof(GameDateInput));
        OnPropertyChanged(nameof(GameLocationInput));
        OnPropertyChanged(nameof(GameHomeScoreInput));
        OnPropertyChanged(nameof(GameAwayScoreInput));
        OnPropertyChanged(nameof(GameStatusInput));
        OnPropertyChanged(nameof(SelectedGameLabel));
        OnPropertyChanged(nameof(SelectedHomeTeamNameDisplay));
        OnPropertyChanged(nameof(SelectedHomeTeamIdDisplay));
        OnPropertyChanged(nameof(SelectedAwayTeamNameDisplay));
        OnPropertyChanged(nameof(SelectedAwayTeamIdDisplay));
    }

    private void SyncSelectedTeamsFromInputs()
    {
        if (int.TryParse(GameHomeTeamIdInput, out var homeId))
        {
            _selectedHomeTeam = Teams.FirstOrDefault(t => t.TeamId == homeId);
        }
        else
        {
            _selectedHomeTeam = null;
        }

        if (int.TryParse(GameAwayTeamIdInput, out var awayId))
        {
            _selectedAwayTeam = Teams.FirstOrDefault(t => t.TeamId == awayId);
        }
        else
        {
            _selectedAwayTeam = null;
        }

        OnPropertyChanged(nameof(SelectedHomeTeam));
        OnPropertyChanged(nameof(SelectedAwayTeam));
        OnPropertyChanged(nameof(SelectedHomeTeamNameDisplay));
        OnPropertyChanged(nameof(SelectedHomeTeamIdDisplay));
        OnPropertyChanged(nameof(SelectedAwayTeamNameDisplay));
        OnPropertyChanged(nameof(SelectedAwayTeamIdDisplay));
    }
}

public class GameListRow
{
    public GameItem? Game { get; set; }
    public int GameId { get; set; }
    public string MatchupText { get; set; } = string.Empty;
    public string DateText { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
