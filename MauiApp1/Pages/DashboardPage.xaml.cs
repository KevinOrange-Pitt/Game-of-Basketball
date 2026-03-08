using MauiApp1.Models;
using MauiApp1.Services;
using System.Collections.ObjectModel;

namespace MauiApp1.Pages;

public partial class DashboardPage : ContentPage
{
    private readonly DatabaseService _databaseService;

    public ObservableCollection<string> RecentGames { get; } = new();

    public string TeamHeader { get; set; } = "Team: Select a team";
    public string NextGameText { get; set; } = "Next Game: loading...";

    public DashboardPage()
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

        TeamSelectionState.TeamChanged -= OnTeamChanged;
        TeamSelectionState.TeamChanged += OnTeamChanged;

        TeamHeader = $"Team: {TeamSelectionState.SelectedTeamName}";
        OnPropertyChanged(nameof(TeamHeader));

        await LoadDashboardAsync();
    }

    protected override void OnDisappearing()
    {
        TeamSelectionState.TeamChanged -= OnTeamChanged;
        base.OnDisappearing();
    }

    private async Task LoadDashboardAsync()
    {
        try
        {
            var games = await _databaseService.GetGamesAsync();
            RecentGames.Clear();

            foreach (var game in games.OrderByDescending(g => g.GameDate).Take(4))
            {
                var result = BuildResultTag(game);
                RecentGames.Add($"{game.GameDate:MMM d} T{game.HomeTeamId} vs T{game.AwayTeamId} ({result})");
            }

            var nextGame = games.Where(g => g.GameDate >= DateTime.Now)
                                .OrderBy(g => g.GameDate)
                                .FirstOrDefault();

            NextGameText = nextGame is null
                ? "Next Game: none scheduled"
                : $"Next Game: {nextGame.GameDate:MMM d} vs Team {nextGame.AwayTeamId}";

            OnPropertyChanged(nameof(NextGameText));
        }
        catch
        {
            RecentGames.Clear();
            RecentGames.Add("Could not load dashboard data. Start API and refresh.");
            NextGameText = "Next Game: unavailable";
            OnPropertyChanged(nameof(NextGameText));
        }
    }

    private static string BuildResultTag(GameItem game)
    {
        if (!game.HomeScore.HasValue || !game.AwayScore.HasValue)
        {
            return game.Status;
        }

        return game.HomeScore > game.AwayScore
            ? $"W {game.HomeScore}-{game.AwayScore}"
            : $"L {game.HomeScore}-{game.AwayScore}";
    }

    private async void OnStartLiveGameClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//livegame");
    }

    private void OnTeamChanged(string teamName)
    {
        TeamHeader = $"Team: {teamName}";
        MainThread.BeginInvokeOnMainThread(() => OnPropertyChanged(nameof(TeamHeader)));
    }
}
