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
    public ObservableCollection<ScheduleItem> Schedule { get; } = new();

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
        TeamSelectionState.SelectedTeamName = "Select a team";
        ClearTeamForm();
    }

    private async void OnTeamSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not TeamItem team)
        {
            return;
        }

        _selectedTeam = team;
        TeamSelectionState.SelectedTeamName = team.Name;
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

            Roster.Clear();
            foreach (var player in players.Where(p => p.TeamId == team.TeamId).OrderBy(p => p.JerseyNumber ?? 999))
            {
                Roster.Add(player);
            }

            Schedule.Clear();
            foreach (var schedule in schedules.Where(s => s.TeamId == team.TeamId)
                                              .OrderByDescending(s => s.ScheduleId)
                                              .Take(8))
            {
                Schedule.Add(schedule);
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
        TeamSelectionState.SelectedTeamName = "Select a team";

        OnPropertyChanged(nameof(TeamNameInput));
        OnPropertyChanged(nameof(TeamCityInput));
        OnPropertyChanged(nameof(TeamCoachInput));
        OnPropertyChanged(nameof(SelectedTeamHeader));
        OnPropertyChanged(nameof(SelectedTeamDetail));
    }
}
