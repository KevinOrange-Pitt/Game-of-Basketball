using MauiApp1.Models;
using MauiApp1.Services;
using System.Collections.ObjectModel;

namespace MauiApp1;

public partial class MainPage : ContentPage
{
    private readonly DatabaseService _databaseService;

    private TeamItem? _selectedTeam;
    private PlayerItem? _selectedPlayer;
    private GameItem? _selectedGame;
    private ScheduleItem? _selectedSchedule;
    private bool _isWorking;

    public ObservableCollection<TeamItem> Teams { get; } = new();
    public ObservableCollection<PlayerItem> Players { get; } = new();
    public ObservableCollection<GameItem> Games { get; } = new();
    public ObservableCollection<ScheduleItem> Schedules { get; } = new();

    public string StatusMessage { get; set; } = "Load data to begin.";

    public bool IsWorking
    {
        get => _isWorking;
        set
        {
            _isWorking = value;
            OnPropertyChanged();
            RefreshActionStates();
        }
    }

    public bool CanCreate => !IsWorking;
    public bool CanRefreshAll => !IsWorking;
    public bool CanUpdateTeam => !IsWorking && _selectedTeam is not null;
    public bool CanUpdatePlayer => !IsWorking && _selectedPlayer is not null;
    public bool CanUpdateGame => !IsWorking && _selectedGame is not null;
    public bool CanUpdateSchedule => !IsWorking && _selectedSchedule is not null;

    private string _teamNameInput = string.Empty;
    public string TeamNameInput
    {
        get => _teamNameInput;
        set { _teamNameInput = value; OnPropertyChanged(); }
    }

    private string _teamCityInput = string.Empty;
    public string TeamCityInput
    {
        get => _teamCityInput;
        set { _teamCityInput = value; OnPropertyChanged(); }
    }

    private string _teamCoachInput = string.Empty;
    public string TeamCoachInput
    {
        get => _teamCoachInput;
        set { _teamCoachInput = value; OnPropertyChanged(); }
    }

    private string _selectedTeamIdDisplay = "Selected Team Id: none";
    public string SelectedTeamIdDisplay
    {
        get => _selectedTeamIdDisplay;
        set { _selectedTeamIdDisplay = value; OnPropertyChanged(); }
    }

    private string _playerTeamIdInput = string.Empty;
    public string PlayerTeamIdInput
    {
        get => _playerTeamIdInput;
        set { _playerTeamIdInput = value; OnPropertyChanged(); }
    }

    private string _playerFirstNameInput = string.Empty;
    public string PlayerFirstNameInput
    {
        get => _playerFirstNameInput;
        set { _playerFirstNameInput = value; OnPropertyChanged(); }
    }

    private string _playerLastNameInput = string.Empty;
    public string PlayerLastNameInput
    {
        get => _playerLastNameInput;
        set { _playerLastNameInput = value; OnPropertyChanged(); }
    }

    private string _playerJerseyInput = string.Empty;
    public string PlayerJerseyInput
    {
        get => _playerJerseyInput;
        set { _playerJerseyInput = value; OnPropertyChanged(); }
    }

    private string _playerPositionInput = string.Empty;
    public string PlayerPositionInput
    {
        get => _playerPositionInput;
        set { _playerPositionInput = value; OnPropertyChanged(); }
    }

    private string _gameHomeTeamIdInput = string.Empty;
    public string GameHomeTeamIdInput
    {
        get => _gameHomeTeamIdInput;
        set { _gameHomeTeamIdInput = value; OnPropertyChanged(); }
    }

    private string _gameAwayTeamIdInput = string.Empty;
    public string GameAwayTeamIdInput
    {
        get => _gameAwayTeamIdInput;
        set { _gameAwayTeamIdInput = value; OnPropertyChanged(); }
    }

    private string _gameDateInput = string.Empty;
    public string GameDateInput
    {
        get => _gameDateInput;
        set { _gameDateInput = value; OnPropertyChanged(); }
    }

    private string _gameLocationInput = string.Empty;
    public string GameLocationInput
    {
        get => _gameLocationInput;
        set { _gameLocationInput = value; OnPropertyChanged(); }
    }

    private string _gameHomeScoreInput = string.Empty;
    public string GameHomeScoreInput
    {
        get => _gameHomeScoreInput;
        set { _gameHomeScoreInput = value; OnPropertyChanged(); }
    }

    private string _gameAwayScoreInput = string.Empty;
    public string GameAwayScoreInput
    {
        get => _gameAwayScoreInput;
        set { _gameAwayScoreInput = value; OnPropertyChanged(); }
    }

    private string _gameStatusInput = "Scheduled";
    public string GameStatusInput
    {
        get => _gameStatusInput;
        set { _gameStatusInput = value; OnPropertyChanged(); }
    }

    private string _scheduleTeamIdInput = string.Empty;
    public string ScheduleTeamIdInput
    {
        get => _scheduleTeamIdInput;
        set { _scheduleTeamIdInput = value; OnPropertyChanged(); }
    }

    private string _scheduleGameIdInput = string.Empty;
    public string ScheduleGameIdInput
    {
        get => _scheduleGameIdInput;
        set { _scheduleGameIdInput = value; OnPropertyChanged(); }
    }

    private string _scheduleIsHomeInput = "true";
    public string ScheduleIsHomeInput
    {
        get => _scheduleIsHomeInput;
        set { _scheduleIsHomeInput = value; OnPropertyChanged(); }
    }

    public MainPage()
    {
        _databaseService = new DatabaseService(new HttpClient
        {
            BaseAddress = new Uri("http://127.0.0.1:5117/")
        });

        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (Teams.Count == 0 && Players.Count == 0 && Games.Count == 0 && Schedules.Count == 0)
        {
            await LoadAllAsync();
        }
    }

    private async Task LoadAllAsync(bool manageBusyState = true)
    {
        if (manageBusyState && IsWorking)
        {
            return;
        }

        try
        {
            if (manageBusyState)
            {
                IsWorking = true;
            }

            var teams = await _databaseService.GetTeamsAsync();
            var players = await _databaseService.GetPlayersAsync();
            var games = await _databaseService.GetGamesAsync();
            var schedules = await _databaseService.GetSchedulesAsync();

            Refill(Teams, teams);
            Refill(Players, players);
            Refill(Games, games);
            Refill(Schedules, schedules);

            StatusMessage = $"Loaded {teams.Count} teams, {players.Count} players, {games.Count} games, {schedules.Count} schedules.";
        }
        catch (Exception ex)
        {
            StatusMessage = "Could not load dashboard data.";
            await DisplayAlert("API Error", ex.Message, "OK");
        }
        finally
        {
            if (manageBusyState)
            {
                IsWorking = false;
            }

            OnPropertyChanged(nameof(StatusMessage));
        }
    }

    private static void Refill<T>(ObservableCollection<T> target, IEnumerable<T> source)
    {
        target.Clear();
        foreach (var item in source)
        {
            target.Add(item);
        }
    }

    private async void OnRefreshAllClicked(object sender, EventArgs e) => await LoadAllAsync();
    private async void OnRefreshTeamsClicked(object sender, EventArgs e) => await RefreshTeamsOnlyAsync();
    private async void OnRefreshPlayersClicked(object sender, EventArgs e) => await RefreshPlayersOnlyAsync();
    private async void OnRefreshGamesClicked(object sender, EventArgs e) => await RefreshGamesOnlyAsync();
    private async void OnRefreshSchedulesClicked(object sender, EventArgs e) => await RefreshSchedulesOnlyAsync();

    private async Task RefreshTeamsOnlyAsync()
    {
        await RunBusyAsync(async () => Refill(Teams, await _databaseService.GetTeamsAsync()), "Teams refreshed.");
    }

    private async Task RefreshPlayersOnlyAsync()
    {
        await RunBusyAsync(async () => Refill(Players, await _databaseService.GetPlayersAsync()), "Players refreshed.");
    }

    private async Task RefreshGamesOnlyAsync()
    {
        await RunBusyAsync(async () => Refill(Games, await _databaseService.GetGamesAsync()), "Games refreshed.");
    }

    private async Task RefreshSchedulesOnlyAsync()
    {
        await RunBusyAsync(async () => Refill(Schedules, await _databaseService.GetSchedulesAsync()), "Schedules refreshed.");
    }

    private async Task RunBusyAsync(Func<Task> work, string successMessage)
    {
        try
        {
            IsWorking = true;
            await work();
            StatusMessage = successMessage;
        }
        catch (Exception ex)
        {
            StatusMessage = "Operation failed.";
            await DisplayAlert("API Error", ex.Message, "OK");
        }
        finally
        {
            IsWorking = false;
            OnPropertyChanged(nameof(StatusMessage));
        }
    }

    private async void OnCreateTeamClicked(object sender, EventArgs e)
    {
        if (!TryReadTeamForm(out var name, out var city, out var coach, out var validation))
        {
            await DisplayAlert("Validation", validation, "OK");
            return;
        }

        await RunBusyAsync(async () =>
        {
            var created = await _databaseService.CreateTeamAsync(name, city, coach);
            PlayerTeamIdInput = created.TeamId.ToString();
            ScheduleTeamIdInput = created.TeamId.ToString();
            SelectedTeamIdDisplay = $"Selected Team Id: {created.TeamId}";
            ClearTeamForm();
            await RefreshTeamsOnlyAsync();
        }, "Team created.");
    }

    private async void OnUpdateTeamClicked(object sender, EventArgs e)
    {
        if (_selectedTeam is null)
        {
            await DisplayAlert("Selection Required", "Select a team first.", "OK");
            return;
        }

        if (!TryReadTeamForm(out var name, out var city, out var coach, out var validation))
        {
            await DisplayAlert("Validation", validation, "OK");
            return;
        }

        await RunBusyAsync(async () =>
        {
            await _databaseService.UpdateTeamAsync(_selectedTeam.TeamId, name, city, coach);
            await RefreshTeamsOnlyAsync();
        }, "Team updated.");
    }

    private async void OnDeleteTeamClicked(object sender, EventArgs e)
    {
        if (_selectedTeam is null)
        {
            await DisplayAlert("Selection Required", "Select a team first.", "OK");
            return;
        }

        var confirmed = await DisplayAlert("Delete Team", $"Delete team '{_selectedTeam.Name}'?", "Delete", "Cancel");
        if (!confirmed)
        {
            return;
        }

        await RunBusyAsync(async () =>
        {
            await _databaseService.DeleteTeamAsync(_selectedTeam.TeamId);
            ClearTeamForm();
            await LoadAllAsync(manageBusyState: false);
        }, "Team deleted.");
    }

    private void OnClearTeamClicked(object sender, EventArgs e)
    {
        ClearTeamForm();
        StatusMessage = "Team form cleared.";
        OnPropertyChanged(nameof(StatusMessage));
    }

    private async void OnCreatePlayerClicked(object sender, EventArgs e)
    {
        if (!TryReadPlayerForm(out var teamId, out var firstName, out var lastName, out var jersey, out var position, out var validation))
        {
            await DisplayAlert("Validation", validation, "OK");
            return;
        }

        await RunBusyAsync(async () =>
        {
            await _databaseService.CreatePlayerAsync(teamId, firstName, lastName, jersey, position);
            ClearPlayerForm();
            await RefreshPlayersOnlyAsync();
        }, "Player created.");
    }

    private async void OnUpdatePlayerClicked(object sender, EventArgs e)
    {
        if (_selectedPlayer is null)
        {
            await DisplayAlert("Selection Required", "Select a player first.", "OK");
            return;
        }

        if (!TryReadPlayerForm(out var teamId, out var firstName, out var lastName, out var jersey, out var position, out var validation))
        {
            await DisplayAlert("Validation", validation, "OK");
            return;
        }

        await RunBusyAsync(async () =>
        {
            await _databaseService.UpdatePlayerAsync(_selectedPlayer.PlayerId, teamId, firstName, lastName, jersey, position);
            await RefreshPlayersOnlyAsync();
        }, "Player updated.");
    }

    private async void OnDeletePlayerClicked(object sender, EventArgs e)
    {
        if (_selectedPlayer is null)
        {
            await DisplayAlert("Selection Required", "Select a player first.", "OK");
            return;
        }

        var confirmed = await DisplayAlert("Delete Player", $"Delete '{_selectedPlayer.FullName}'?", "Delete", "Cancel");
        if (!confirmed)
        {
            return;
        }

        await RunBusyAsync(async () =>
        {
            await _databaseService.DeletePlayerAsync(_selectedPlayer.PlayerId);
            ClearPlayerForm();
            await RefreshPlayersOnlyAsync();
        }, "Player deleted.");
    }

    private void OnClearPlayerClicked(object sender, EventArgs e)
    {
        ClearPlayerForm();
        StatusMessage = "Player form cleared.";
        OnPropertyChanged(nameof(StatusMessage));
    }

    private async void OnCreateGameClicked(object sender, EventArgs e)
    {
        if (!TryReadGameForm(out var homeTeamId, out var awayTeamId, out var gameDate, out var location, out var homeScore, out var awayScore, out var status, out var validation))
        {
            await DisplayAlert("Validation", validation, "OK");
            return;
        }

        await RunBusyAsync(async () =>
        {
            await _databaseService.CreateGameAsync(homeTeamId, awayTeamId, gameDate, location, homeScore, awayScore, status);
            ClearGameForm();
            await RefreshGamesOnlyAsync();
        }, "Game created.");
    }

    private async void OnUpdateGameClicked(object sender, EventArgs e)
    {
        if (_selectedGame is null)
        {
            await DisplayAlert("Selection Required", "Select a game first.", "OK");
            return;
        }

        if (!TryReadGameForm(out var homeTeamId, out var awayTeamId, out var gameDate, out var location, out var homeScore, out var awayScore, out var status, out var validation))
        {
            await DisplayAlert("Validation", validation, "OK");
            return;
        }

        await RunBusyAsync(async () =>
        {
            await _databaseService.UpdateGameAsync(_selectedGame.GameId, homeTeamId, awayTeamId, gameDate, location, homeScore, awayScore, status);
            await RefreshGamesOnlyAsync();
        }, "Game updated.");
    }

    private async void OnDeleteGameClicked(object sender, EventArgs e)
    {
        if (_selectedGame is null)
        {
            await DisplayAlert("Selection Required", "Select a game first.", "OK");
            return;
        }

        var confirmed = await DisplayAlert("Delete Game", $"Delete game Id {_selectedGame.GameId}?", "Delete", "Cancel");
        if (!confirmed)
        {
            return;
        }

        await RunBusyAsync(async () =>
        {
            await _databaseService.DeleteGameAsync(_selectedGame.GameId);
            ClearGameForm();
            await LoadAllAsync(manageBusyState: false);
        }, "Game deleted.");
    }

    private void OnClearGameClicked(object sender, EventArgs e)
    {
        ClearGameForm();
        StatusMessage = "Game form cleared.";
        OnPropertyChanged(nameof(StatusMessage));
    }

    private async void OnCreateScheduleClicked(object sender, EventArgs e)
    {
        if (!TryReadScheduleForm(out var teamId, out var gameId, out var isHome, out var validation))
        {
            await DisplayAlert("Validation", validation, "OK");
            return;
        }

        await RunBusyAsync(async () =>
        {
            await _databaseService.CreateScheduleAsync(teamId, gameId, isHome);
            ClearScheduleForm();
            await RefreshSchedulesOnlyAsync();
        }, "Schedule created.");
    }

    private async void OnUpdateScheduleClicked(object sender, EventArgs e)
    {
        if (_selectedSchedule is null)
        {
            await DisplayAlert("Selection Required", "Select a schedule first.", "OK");
            return;
        }

        if (!TryReadScheduleForm(out var teamId, out var gameId, out var isHome, out var validation))
        {
            await DisplayAlert("Validation", validation, "OK");
            return;
        }

        await RunBusyAsync(async () =>
        {
            await _databaseService.UpdateScheduleAsync(_selectedSchedule.ScheduleId, teamId, gameId, isHome);
            await RefreshSchedulesOnlyAsync();
        }, "Schedule updated.");
    }

    private async void OnDeleteScheduleClicked(object sender, EventArgs e)
    {
        if (_selectedSchedule is null)
        {
            await DisplayAlert("Selection Required", "Select a schedule first.", "OK");
            return;
        }

        var confirmed = await DisplayAlert("Delete Schedule", $"Delete schedule Id {_selectedSchedule.ScheduleId}?", "Delete", "Cancel");
        if (!confirmed)
        {
            return;
        }

        await RunBusyAsync(async () =>
        {
            await _databaseService.DeleteScheduleAsync(_selectedSchedule.ScheduleId);
            ClearScheduleForm();
            await RefreshSchedulesOnlyAsync();
        }, "Schedule deleted.");
    }

    private void OnClearScheduleClicked(object sender, EventArgs e)
    {
        ClearScheduleForm();
        StatusMessage = "Schedule form cleared.";
        OnPropertyChanged(nameof(StatusMessage));
    }

    private void OnTeamSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedTeam = e.CurrentSelection.FirstOrDefault() as TeamItem;
        if (_selectedTeam is null)
        {
            RefreshActionStates();
            return;
        }

        TeamNameInput = _selectedTeam.Name;
        TeamCityInput = _selectedTeam.City;
        TeamCoachInput = _selectedTeam.Coach;
        SelectedTeamIdDisplay = $"Selected Team Id: {_selectedTeam.TeamId}";

        // Use selected team to prefill dependent CRUD forms.
        PlayerTeamIdInput = _selectedTeam.TeamId.ToString();
        ScheduleTeamIdInput = _selectedTeam.TeamId.ToString();
        if (string.IsNullOrWhiteSpace(GameHomeTeamIdInput))
        {
            GameHomeTeamIdInput = _selectedTeam.TeamId.ToString();
        }

        if (string.IsNullOrWhiteSpace(GameAwayTeamIdInput))
        {
            GameAwayTeamIdInput = _selectedTeam.TeamId.ToString();
        }

        StatusMessage = $"Editing team Id {_selectedTeam.TeamId}.";
        OnPropertyChanged(nameof(StatusMessage));
        RefreshActionStates();
    }

    private void OnPlayerSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedPlayer = e.CurrentSelection.FirstOrDefault() as PlayerItem;
        if (_selectedPlayer is null)
        {
            RefreshActionStates();
            return;
        }

        PlayerTeamIdInput = _selectedPlayer.TeamId.ToString();
        PlayerFirstNameInput = _selectedPlayer.FirstName;
        PlayerLastNameInput = _selectedPlayer.LastName;
        PlayerJerseyInput = _selectedPlayer.JerseyNumber?.ToString() ?? string.Empty;
        PlayerPositionInput = _selectedPlayer.Position;
        StatusMessage = $"Editing player Id {_selectedPlayer.PlayerId}.";
        OnPropertyChanged(nameof(StatusMessage));
        RefreshActionStates();
    }

    private void OnGameSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedGame = e.CurrentSelection.FirstOrDefault() as GameItem;
        if (_selectedGame is null)
        {
            RefreshActionStates();
            return;
        }

        GameHomeTeamIdInput = _selectedGame.HomeTeamId.ToString();
        GameAwayTeamIdInput = _selectedGame.AwayTeamId.ToString();
        GameDateInput = _selectedGame.GameDate.ToString("yyyy-MM-dd HH:mm");
        GameLocationInput = _selectedGame.Location;
        GameHomeScoreInput = _selectedGame.HomeScore?.ToString() ?? string.Empty;
        GameAwayScoreInput = _selectedGame.AwayScore?.ToString() ?? string.Empty;
        GameStatusInput = _selectedGame.Status;
        StatusMessage = $"Editing game Id {_selectedGame.GameId}.";
        OnPropertyChanged(nameof(StatusMessage));
        RefreshActionStates();
    }

    private void OnScheduleSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedSchedule = e.CurrentSelection.FirstOrDefault() as ScheduleItem;
        if (_selectedSchedule is null)
        {
            RefreshActionStates();
            return;
        }

        ScheduleTeamIdInput = _selectedSchedule.TeamId.ToString();
        ScheduleGameIdInput = _selectedSchedule.GameId.ToString();
        ScheduleIsHomeInput = _selectedSchedule.IsHome ? "true" : "false";
        StatusMessage = $"Editing schedule Id {_selectedSchedule.ScheduleId}.";
        OnPropertyChanged(nameof(StatusMessage));
        RefreshActionStates();
    }

    private bool TryReadTeamForm(out string name, out string city, out string coach, out string validation)
    {
        name = TeamNameInput.Trim();
        city = TeamCityInput.Trim();
        coach = TeamCoachInput.Trim();
        validation = string.Empty;

        if (string.IsNullOrWhiteSpace(name))
        {
            validation = "Team name is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            validation = "City is required.";
            return false;
        }

        return true;
    }

    private bool TryReadPlayerForm(out int teamId, out string firstName, out string lastName, out int? jersey, out string position, out string validation)
    {
        teamId = 0;
        firstName = PlayerFirstNameInput.Trim();
        lastName = PlayerLastNameInput.Trim();
        jersey = null;
        position = PlayerPositionInput.Trim();
        validation = string.Empty;

        if (!int.TryParse(PlayerTeamIdInput, out teamId) || teamId <= 0)
        {
            validation = "Player Team Id must be a whole number greater than zero.";
            return false;
        }

        var playerTeamId = teamId;
        if (!Teams.Any(t => t.TeamId == playerTeamId))
        {
            validation = "Player Team Id does not exist in Teams. Create/select a team first.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(firstName))
        {
            validation = "Player first name is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            validation = "Player last name is required.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(PlayerJerseyInput))
        {
            if (!int.TryParse(PlayerJerseyInput, out var jerseyValue))
            {
                validation = "Jersey must be a whole number.";
                return false;
            }

            jersey = jerseyValue;
        }

        return true;
    }

    private bool TryReadGameForm(out int homeTeamId, out int awayTeamId, out DateTime gameDate, out string location, out int? homeScore, out int? awayScore, out string status, out string validation)
    {
        homeTeamId = 0;
        awayTeamId = 0;
        gameDate = DateTime.UtcNow;
        location = GameLocationInput.Trim();
        homeScore = null;
        awayScore = null;
        status = GameStatusInput.Trim();
        validation = string.Empty;

        if (!int.TryParse(GameHomeTeamIdInput, out homeTeamId) || homeTeamId <= 0)
        {
            validation = "Home Team Id must be greater than zero.";
            return false;
        }

        if (!int.TryParse(GameAwayTeamIdInput, out awayTeamId) || awayTeamId <= 0)
        {
            validation = "Away Team Id must be greater than zero.";
            return false;
        }

        if (!DateTime.TryParse(GameDateInput, out gameDate))
        {
            validation = "Game date must be a valid date/time (example: 2026-03-07 19:30).";
            return false;
        }

        if (string.IsNullOrWhiteSpace(location))
        {
            validation = "Location is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(status))
        {
            validation = "Status is required.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(GameHomeScoreInput))
        {
            if (!int.TryParse(GameHomeScoreInput, out var hs))
            {
                validation = "Home score must be a whole number.";
                return false;
            }

            homeScore = hs;
        }

        if (!string.IsNullOrWhiteSpace(GameAwayScoreInput))
        {
            if (!int.TryParse(GameAwayScoreInput, out var aws))
            {
                validation = "Away score must be a whole number.";
                return false;
            }

            awayScore = aws;
        }

        return true;
    }

    private bool TryReadScheduleForm(out int teamId, out int gameId, out bool isHome, out string validation)
    {
        teamId = 0;
        gameId = 0;
        isHome = true;
        validation = string.Empty;

        if (!int.TryParse(ScheduleTeamIdInput, out teamId) || teamId <= 0)
        {
            validation = "Schedule Team Id must be greater than zero.";
            return false;
        }

        var scheduleTeamId = teamId;
        if (!Teams.Any(t => t.TeamId == scheduleTeamId))
        {
            validation = "Schedule Team Id does not exist in Teams. Create/select a team first.";
            return false;
        }

        if (!int.TryParse(ScheduleGameIdInput, out gameId) || gameId <= 0)
        {
            validation = "Schedule Game Id must be greater than zero.";
            return false;
        }

        if (!bool.TryParse(ScheduleIsHomeInput, out isHome))
        {
            validation = "Is Home must be 'true' or 'false'.";
            return false;
        }

        return true;
    }

    private void ClearTeamForm()
    {
        _selectedTeam = null;
        TeamsCollection.SelectedItem = null;
        TeamNameInput = string.Empty;
        TeamCityInput = string.Empty;
        TeamCoachInput = string.Empty;
        SelectedTeamIdDisplay = "Selected Team Id: none";
        RefreshActionStates();
    }

    private void ClearPlayerForm()
    {
        _selectedPlayer = null;
        PlayersCollection.SelectedItem = null;
        PlayerTeamIdInput = string.Empty;
        PlayerFirstNameInput = string.Empty;
        PlayerLastNameInput = string.Empty;
        PlayerJerseyInput = string.Empty;
        PlayerPositionInput = string.Empty;
        RefreshActionStates();
    }

    private void ClearGameForm()
    {
        _selectedGame = null;
        GamesCollection.SelectedItem = null;
        GameHomeTeamIdInput = string.Empty;
        GameAwayTeamIdInput = string.Empty;
        GameDateInput = string.Empty;
        GameLocationInput = string.Empty;
        GameHomeScoreInput = string.Empty;
        GameAwayScoreInput = string.Empty;
        GameStatusInput = "Scheduled";
        RefreshActionStates();
    }

    private void ClearScheduleForm()
    {
        _selectedSchedule = null;
        SchedulesCollection.SelectedItem = null;
        ScheduleTeamIdInput = string.Empty;
        ScheduleGameIdInput = string.Empty;
        ScheduleIsHomeInput = "true";
        RefreshActionStates();
    }

    private void RefreshActionStates()
    {
        OnPropertyChanged(nameof(CanCreate));
        OnPropertyChanged(nameof(CanRefreshAll));
        OnPropertyChanged(nameof(CanUpdateTeam));
        OnPropertyChanged(nameof(CanUpdatePlayer));
        OnPropertyChanged(nameof(CanUpdateGame));
        OnPropertyChanged(nameof(CanUpdateSchedule));
    }
}
