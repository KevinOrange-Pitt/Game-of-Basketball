using MauiApp1.Models;
using MauiApp1.Services;
using System.Collections.ObjectModel;

namespace MauiApp1.Pages;

public partial class PlayersPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private readonly List<PlayerItem> _allPlayers = new();
    private PlayerItem? _selectedPlayer;

    public ObservableCollection<PlayerItem> FilteredPlayers { get; } = new();

    public string SearchText { get; set; } = string.Empty;
    public string PlayerTeamIdInput { get; set; } = string.Empty;
    public string PlayerFirstNameInput { get; set; } = string.Empty;
    public string PlayerLastNameInput { get; set; } = string.Empty;
    public string PlayerJerseyInput { get; set; } = string.Empty;
    public string PlayerPositionInput { get; set; } = string.Empty;
    public string SelectedPlayerIdLabel { get; set; } = "Selected Player Id: none";

    public string SelectedPlayerName { get; set; } = "Name: -";
    public string SelectedPlayerTeam { get; set; } = "Team: -";
    public string SelectedPlayerPosition { get; set; } = "Position: -";
    public string StatPPG { get; set; } = "PPG: --";
    public string StatAssists { get; set; } = "Assists: --";
    public string StatRebounds { get; set; } = "Rebounds: --";
    public string StatSteals { get; set; } = "Steals: --";

    public PlayersPage()
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
        if (_allPlayers.Count == 0)
        {
            await LoadPlayersAsync();
        }
    }

    private async Task LoadPlayersAsync()
    {
        try
        {
            var players = await _databaseService.GetPlayersAsync();
            _allPlayers.Clear();
            _allPlayers.AddRange(players);
            ApplyFilter();
        }
        catch
        {
            await DisplayAlertAsync("API Error", "Could not load players. Start API and try again.", "OK");
        }
    }

    private void ApplyFilter()
    {
        var query = SearchText?.Trim() ?? string.Empty;

        var filtered = _allPlayers
            .Where(p => string.IsNullOrWhiteSpace(query)
                        || p.FullName.Contains(query, StringComparison.OrdinalIgnoreCase)
                        || p.TeamDisplay.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToList();

        FilteredPlayers.Clear();
        foreach (var player in filtered)
        {
            FilteredPlayers.Add(player);
        }
    }

    private void OnSearchChanged(object sender, TextChangedEventArgs e)
    {
        SearchText = e.NewTextValue ?? string.Empty;
        ApplyFilter();
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        await LoadPlayersAsync();
    }

    private async void OnCreatePlayerClicked(object sender, EventArgs e)
    {
        if (!TryGetPlayerInput(out var teamId, out var firstName, out var lastName, out var jersey, out var position))
        {
            return;
        }

        try
        {
            await _databaseService.CreatePlayerAsync(teamId, firstName, lastName, jersey, position);
            await LoadPlayersAsync();
            ClearPlayerForm();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Create Player Failed", ex.Message, "OK");
        }
    }

    private async void OnUpdatePlayerClicked(object sender, EventArgs e)
    {
        if (_selectedPlayer is null)
        {
            await DisplayAlertAsync("Update Player", "Select a player first.", "OK");
            return;
        }

        if (!TryGetPlayerInput(out var teamId, out var firstName, out var lastName, out var jersey, out var position))
        {
            return;
        }

        try
        {
            await _databaseService.UpdatePlayerAsync(_selectedPlayer.PlayerId, teamId, firstName, lastName, jersey, position);
            await LoadPlayersAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Update Player Failed", ex.Message, "OK");
        }
    }

    private async void OnDeletePlayerClicked(object sender, EventArgs e)
    {
        if (_selectedPlayer is null)
        {
            await DisplayAlertAsync("Delete Player", "Select a player first.", "OK");
            return;
        }

        var confirm = await DisplayAlertAsync("Delete Player", $"Delete {_selectedPlayer.FullName}?", "Delete", "Cancel");
        if (!confirm)
        {
            return;
        }

        try
        {
            await _databaseService.DeletePlayerAsync(_selectedPlayer.PlayerId);
            await LoadPlayersAsync();
            _selectedPlayer = null;
            ClearPlayerForm();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Delete Player Failed", ex.Message, "OK");
        }
    }

    private void OnClearPlayerClicked(object sender, EventArgs e)
    {
        _selectedPlayer = null;
        ClearPlayerForm();
    }

    private void OnPlayerSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not PlayerItem player)
        {
            return;
        }

        _selectedPlayer = player;
        PlayerTeamIdInput = player.TeamId.ToString();
        PlayerFirstNameInput = player.FirstName;
        PlayerLastNameInput = player.LastName;
        PlayerJerseyInput = player.JerseyNumber?.ToString() ?? string.Empty;
        PlayerPositionInput = player.Position;
        SelectedPlayerIdLabel = $"Selected Player Id: {player.PlayerId}";

        OnPropertyChanged(nameof(PlayerTeamIdInput));
        OnPropertyChanged(nameof(PlayerFirstNameInput));
        OnPropertyChanged(nameof(PlayerLastNameInput));
        OnPropertyChanged(nameof(PlayerJerseyInput));
        OnPropertyChanged(nameof(PlayerPositionInput));
        OnPropertyChanged(nameof(SelectedPlayerIdLabel));

        SelectedPlayerName = $"Name: {player.FullName}";
        SelectedPlayerTeam = $"Team: {player.TeamDisplay}";
        SelectedPlayerPosition = $"Position: {player.Position}";

        // Placeholder stat values until detailed per-player Stats data is implemented.
        StatPPG = $"PPG: {(player.JerseyNumber.HasValue ? player.JerseyNumber.Value / 2.0 + 8 : 10):F1}";
        StatAssists = "Assists: 4.2";
        StatRebounds = "Rebounds: 3.7";
        StatSteals = "Steals: 1.4";

        OnPropertyChanged(nameof(SelectedPlayerName));
        OnPropertyChanged(nameof(SelectedPlayerTeam));
        OnPropertyChanged(nameof(SelectedPlayerPosition));
        OnPropertyChanged(nameof(StatPPG));
        OnPropertyChanged(nameof(StatAssists));
        OnPropertyChanged(nameof(StatRebounds));
        OnPropertyChanged(nameof(StatSteals));
    }

    private bool TryGetPlayerInput(out int teamId, out string firstName, out string lastName, out int? jersey, out string position)
    {
        teamId = 0;
        firstName = PlayerFirstNameInput.Trim();
        lastName = PlayerLastNameInput.Trim();
        jersey = null;
        position = PlayerPositionInput.Trim();

        if (!int.TryParse(PlayerTeamIdInput, out teamId) || teamId <= 0)
        {
            _ = DisplayAlertAsync("Validation", "Team Id must be a number greater than zero.", "OK");
            return false;
        }

        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
        {
            _ = DisplayAlertAsync("Validation", "First and last name are required.", "OK");
            return false;
        }

        if (!string.IsNullOrWhiteSpace(PlayerJerseyInput))
        {
            if (!int.TryParse(PlayerJerseyInput, out var parsedJersey) || parsedJersey < 0)
            {
                _ = DisplayAlertAsync("Validation", "Jersey number must be a non-negative number.", "OK");
                return false;
            }

            jersey = parsedJersey;
        }

        return true;
    }

    private void ClearPlayerForm()
    {
        PlayerTeamIdInput = string.Empty;
        PlayerFirstNameInput = string.Empty;
        PlayerLastNameInput = string.Empty;
        PlayerJerseyInput = string.Empty;
        PlayerPositionInput = string.Empty;
        SelectedPlayerIdLabel = "Selected Player Id: none";

        OnPropertyChanged(nameof(PlayerTeamIdInput));
        OnPropertyChanged(nameof(PlayerFirstNameInput));
        OnPropertyChanged(nameof(PlayerLastNameInput));
        OnPropertyChanged(nameof(PlayerJerseyInput));
        OnPropertyChanged(nameof(PlayerPositionInput));
        OnPropertyChanged(nameof(SelectedPlayerIdLabel));
    }
}
