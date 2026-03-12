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
    public string TeamGamesPlayed { get; set; } = "Games Played: --";
    public string TeamRecord { get; set; } = "Record: --";
    public string TeamAvgPoints { get; set; } = "Avg Points: --";
    public string TeamAvgAllowed { get; set; } = "Avg Allowed: --";

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
            var teams = await _databaseService.GetTeamsAsync();
            var games = await _databaseService.GetGamesAsync();
            var teamNameById = teams.ToDictionary(t => t.TeamId, t => t.Name);

            RecentGames.Clear();

            foreach (var game in games.OrderByDescending(g => g.GameDate).Take(4))
            {
                var result = BuildResultTag(game);
                var homeName = teamNameById.TryGetValue(game.HomeTeamId, out var home) ? home : $"Team {game.HomeTeamId}";
                var awayName = teamNameById.TryGetValue(game.AwayTeamId, out var away) ? away : $"Team {game.AwayTeamId}";
                RecentGames.Add($"{game.GameDate:MMM d} {homeName} vs {awayName} ({result})");
            }

            var selectedTeamId = TeamSelectionState.SelectedTeamId;
            var nextGamePool = selectedTeamId.HasValue
                ? games.Where(g => g.GameDate >= DateTime.Now && (g.HomeTeamId == selectedTeamId.Value || g.AwayTeamId == selectedTeamId.Value))
                : games.Where(g => g.GameDate >= DateTime.Now);

            var nextGame = nextGamePool.OrderBy(g => g.GameDate).FirstOrDefault();

            if (nextGame is not null)
            {
                var homeName = teamNameById.TryGetValue(nextGame.HomeTeamId, out var home) ? home : $"Team {nextGame.HomeTeamId}";
                var awayName = teamNameById.TryGetValue(nextGame.AwayTeamId, out var away) ? away : $"Team {nextGame.AwayTeamId}";
                NextGameText = $"Next Game: {nextGame.GameDate:MMM d} {homeName} vs {awayName}";
            }
            else
            {
                NextGameText = "Next Game: none scheduled";
            }

            BuildSelectedTeamStats(games);

            OnPropertyChanged(nameof(NextGameText));
            OnPropertyChanged(nameof(TeamGamesPlayed));
            OnPropertyChanged(nameof(TeamRecord));
            OnPropertyChanged(nameof(TeamAvgPoints));
            OnPropertyChanged(nameof(TeamAvgAllowed));
        }
        catch
        {
            RecentGames.Clear();
            RecentGames.Add("Could not load dashboard data. Start API and refresh.");
            NextGameText = "Next Game: unavailable";
            TeamGamesPlayed = "Games Played: --";
            TeamRecord = "Record: --";
            TeamAvgPoints = "Avg Points: --";
            TeamAvgAllowed = "Avg Allowed: --";
            OnPropertyChanged(nameof(NextGameText));
            OnPropertyChanged(nameof(TeamGamesPlayed));
            OnPropertyChanged(nameof(TeamRecord));
            OnPropertyChanged(nameof(TeamAvgPoints));
            OnPropertyChanged(nameof(TeamAvgAllowed));
        }
    }

    private void BuildSelectedTeamStats(List<GameItem> games)
    {
        var selectedTeamId = TeamSelectionState.SelectedTeamId;
        if (!selectedTeamId.HasValue)
        {
            TeamGamesPlayed = "Games Played: --";
            TeamRecord = "Record: --";
            TeamAvgPoints = "Avg Points: --";
            TeamAvgAllowed = "Avg Allowed: --";
            return;
        }

        var teamGames = games.Where(g => g.HomeTeamId == selectedTeamId.Value || g.AwayTeamId == selectedTeamId.Value).ToList();
        TeamGamesPlayed = $"Games Played: {teamGames.Count}";

        var scoredGames = teamGames.Where(g => g.HomeScore.HasValue && g.AwayScore.HasValue).ToList();
        var wins = scoredGames.Count(g => IsTeamWin(g, selectedTeamId.Value));
        var losses = scoredGames.Count - wins;

        TeamRecord = scoredGames.Count == 0 ? "Record: --" : $"Record: {wins}-{losses}";

        if (scoredGames.Count == 0)
        {
            TeamAvgPoints = "Avg Points: --";
            TeamAvgAllowed = "Avg Allowed: --";
            return;
        }

        var pointsFor = scoredGames.Sum(g => GetTeamScore(g, selectedTeamId.Value));
        var pointsAgainst = scoredGames.Sum(g => GetOpponentScore(g, selectedTeamId.Value));

        TeamAvgPoints = $"Avg Points: {pointsFor / (double)scoredGames.Count:F1}";
        TeamAvgAllowed = $"Avg Allowed: {pointsAgainst / (double)scoredGames.Count:F1}";
    }

    private static bool IsTeamWin(GameItem game, int teamId)
    {
        var teamScore = GetTeamScore(game, teamId);
        var opponentScore = GetOpponentScore(game, teamId);
        return teamScore > opponentScore;
    }

    private static int GetTeamScore(GameItem game, int teamId)
    {
        return game.HomeTeamId == teamId
            ? game.HomeScore ?? 0
            : game.AwayScore ?? 0;
    }

    private static int GetOpponentScore(GameItem game, int teamId)
    {
        return game.HomeTeamId == teamId
            ? game.AwayScore ?? 0
            : game.HomeScore ?? 0;
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
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            OnPropertyChanged(nameof(TeamHeader));
            await LoadDashboardAsync();
        });
    }
}
