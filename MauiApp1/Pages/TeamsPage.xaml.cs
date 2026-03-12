using MauiApp1.Models;
using MauiApp1.Services;
using System.Collections.ObjectModel;

namespace MauiApp1.Pages;

public partial class TeamsPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private TeamItem? _selectedTeam;

    public ObservableCollection<TeamItem> Teams { get; } = new();
    public ObservableCollection<PlayerItem> Roster { get; } = new();
    public ObservableCollection<TeamScheduleRow> Schedule { get; } = new();

    public string SelectedTeamHeader { get; set; } = "Select a team";
    public string SelectedTeamDetail { get; set; } = "Choose a team to view roster and schedule.";
    public string TeamNameInput { get; set; } = string.Empty;
    public string TeamCityInput { get; set; } = string.Empty;
    public string TeamCoachInput { get; set; } = string.Empty;

    public TeamsPage()
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
        if (Teams.Count == 0)
        {
            await LoadTeamsAsync();
        }
    }

    private async Task LoadTeamsAsync()
    {
        try
        {
            var teams = await _databaseService.GetTeamsAsync();
            Teams.Clear();
            foreach (var team in teams)
            {
                Teams.Add(team);
            }
        }
        catch
        {
            await DisplayAlertAsync("API Error", "Could not load teams. Start API and try again.", "OK");
        }
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        await LoadTeamsAsync();
    }

    private async void OnCreateTeamClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TeamNameInput) || string.IsNullOrWhiteSpace(TeamCityInput))
        {
            await DisplayAlertAsync("Validation", "Team name and city are required.", "OK");
            return;
        }

        try
        {
            await _databaseService.CreateTeamAsync(TeamNameInput, TeamCityInput, TeamCoachInput);
            await LoadTeamsAsync();
            ClearTeamForm();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Create Team Failed", ex.Message, "OK");
        }
    }

    private async void OnUpdateTeamClicked(object sender, EventArgs e)
    {
        if (_selectedTeam is null)
        {
            await DisplayAlertAsync("Update Team", "Select a team first.", "OK");
            return;
        }

        try
        {
            await _databaseService.UpdateTeamAsync(_selectedTeam.TeamId, TeamNameInput, TeamCityInput, TeamCoachInput);
            await LoadTeamsAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Update Team Failed", ex.Message, "OK");
        }
    }

    private async void OnDeleteTeamClicked(object sender, EventArgs e)
    {
        if (_selectedTeam is null)
        {
            await DisplayAlertAsync("Delete Team", "Select a team first.", "OK");
            return;
        }

        var confirm = await DisplayAlertAsync("Delete Team", $"Delete {_selectedTeam.Name}?", "Delete", "Cancel");
        if (!confirm)
        {
            return;
        }

        try
        {
            await _databaseService.DeleteTeamAsync(_selectedTeam.TeamId);
            _selectedTeam = null;
            await LoadTeamsAsync();
            Roster.Clear();
            Schedule.Clear();
            ClearTeamForm();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Delete Team Failed", ex.Message, "OK");
        }
    }

    private void OnClearTeamClicked(object sender, EventArgs e)
    {
        _selectedTeam = null;
        TeamSelectionState.SetSelectedTeam(null, "Select a team");
        ClearTeamForm();
    }

    private async void OnTeamSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not TeamItem team)
        {
            return;
        }

        _selectedTeam = team;
        TeamSelectionState.SetSelectedTeam(team.TeamId, team.Name);
        TeamNameInput = team.Name;
        TeamCityInput = team.City;
        TeamCoachInput = team.Coach;

        SelectedTeamHeader = $"Selected Team Id: {team.TeamId}";
        SelectedTeamDetail = $"Team: {team.Name} ({team.City})";

        OnPropertyChanged(nameof(TeamNameInput));
        OnPropertyChanged(nameof(TeamCityInput));
        OnPropertyChanged(nameof(TeamCoachInput));
        OnPropertyChanged(nameof(SelectedTeamHeader));
        OnPropertyChanged(nameof(SelectedTeamDetail));

        try
        {
            var players = await _databaseService.GetPlayersAsync();
            var schedules = await _databaseService.GetSchedulesAsync();
            var games = await _databaseService.GetGamesAsync();
            var teams = await _databaseService.GetTeamsAsync();

            var teamNameById = teams.ToDictionary(t => t.TeamId, t => t.Name);

            Roster.Clear();
            foreach (var player in players.Where(p => p.TeamId == team.TeamId).OrderBy(p => p.JerseyNumber ?? 999))
            {
                Roster.Add(player);
            }

            Schedule.Clear();

            var teamSchedules = schedules.Where(s => s.TeamId == team.TeamId)
                                         .OrderByDescending(s => s.ScheduleId)
                                         .ToList();

            if (teamSchedules.Count == 0)
            {
                // Fallback: show games involving the selected team even if Schedule records were never created.
                foreach (var game in games.Where(g => g.HomeTeamId == team.TeamId || g.AwayTeamId == team.TeamId)
                                          .OrderByDescending(g => g.GameDate)
                                          .Take(8))
                {
                    var homeName = teamNameById.TryGetValue(game.HomeTeamId, out var homeTeamName)
                        ? homeTeamName
                        : $"Team {game.HomeTeamId}";

                    var awayName = teamNameById.TryGetValue(game.AwayTeamId, out var awayTeamName)
                        ? awayTeamName
                        : $"Team {game.AwayTeamId}";

                    var side = game.HomeTeamId == team.TeamId ? "Home" : "Away";
                    Schedule.Add(new TeamScheduleRow
                    {
                        PrimaryText = $"{homeName} vs {awayName}",
                        SecondaryText = $"{game.GameDate:MMM d, h:mm tt} | {side} | {game.Status}"
                    });
                }

                return;
            }

            foreach (var schedule in teamSchedules.Take(8))
            {
                var game = games.FirstOrDefault(g => g.GameId == schedule.GameId);
                if (game is null)
                {
                    Schedule.Add(new TeamScheduleRow
                    {
                        PrimaryText = $"Game {schedule.GameId}",
                        SecondaryText = schedule.IsHome ? "Home" : "Away"
                    });
                    continue;
                }

                var homeName = teamNameById.TryGetValue(game.HomeTeamId, out var homeTeamName)
                    ? homeTeamName
                    : $"Team {game.HomeTeamId}";

                var awayName = teamNameById.TryGetValue(game.AwayTeamId, out var awayTeamName)
                    ? awayTeamName
                    : $"Team {game.AwayTeamId}";

                var side = schedule.IsHome ? "Home" : "Away";
                Schedule.Add(new TeamScheduleRow
                {
                    PrimaryText = $"{homeName} vs {awayName}",
                    SecondaryText = $"{game.GameDate:MMM d, h:mm tt} | {side} | {game.Status}"
                });
            }
        }
        catch
        {
            await DisplayAlertAsync("API Error", "Could not load roster/schedule for selected team.", "OK");
        }
    }

    private void ClearTeamForm()
    {
        TeamNameInput = string.Empty;
        TeamCityInput = string.Empty;
        TeamCoachInput = string.Empty;
        SelectedTeamHeader = "Select a team";
        SelectedTeamDetail = "Choose a team to view roster and schedule.";
        TeamSelectionState.SetSelectedTeam(null, "Select a team");

        OnPropertyChanged(nameof(TeamNameInput));
        OnPropertyChanged(nameof(TeamCityInput));
        OnPropertyChanged(nameof(TeamCoachInput));
        OnPropertyChanged(nameof(SelectedTeamHeader));
        OnPropertyChanged(nameof(SelectedTeamDetail));
    }
}

public class TeamScheduleRow
{
    public string PrimaryText { get; set; } = string.Empty;
    public string SecondaryText { get; set; } = string.Empty;
}
