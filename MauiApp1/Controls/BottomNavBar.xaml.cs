namespace MauiApp1.Controls;

public partial class BottomNavBar : ContentView
{
    public static readonly BindableProperty CurrentRouteProperty = BindableProperty.Create(
        nameof(CurrentRoute),
        typeof(string),
        typeof(BottomNavBar),
        string.Empty,
        propertyChanged: OnCurrentRouteChanged);

    public string CurrentRoute
    {
        get => (string)GetValue(CurrentRouteProperty);
        set => SetValue(CurrentRouteProperty, value);
    }

    public BottomNavBar()
    {
        InitializeComponent();
        ApplyVisualState();
    }

    private static void OnCurrentRouteChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is BottomNavBar navBar)
        {
            navBar.ApplyVisualState();
        }
    }

    private async Task NavigateToAsync(string route)
    {
        if (string.Equals(CurrentRoute, route, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await Shell.Current.GoToAsync($"//{route}");
    }

    private void ApplyVisualState()
    {
        SetButtonState(HomeButton, "dashboard");
        SetButtonState(LiveButton, "livegame");
        SetButtonState(GamesButton, "games");
        SetButtonState(TeamsButton, "teams");
        SetButtonState(PlayersButton, "players");
        SetButtonState(StatsButton, "stats");
    }

    private void SetButtonState(Button button, string route)
    {
        var selected = string.Equals(CurrentRoute, route, StringComparison.OrdinalIgnoreCase);
        button.BackgroundColor = selected ? Color.FromArgb("#2563EB") : Color.FromArgb("#1E293B");
        button.TextColor = Colors.White;
        button.FontAttributes = selected ? FontAttributes.Bold : FontAttributes.None;
    }

    private async void OnHomeClicked(object sender, EventArgs e) => await NavigateToAsync("dashboard");
    private async void OnLiveClicked(object sender, EventArgs e) => await NavigateToAsync("livegame");
    private async void OnGamesClicked(object sender, EventArgs e) => await NavigateToAsync("games");
    private async void OnTeamsClicked(object sender, EventArgs e) => await NavigateToAsync("teams");
    private async void OnPlayersClicked(object sender, EventArgs e) => await NavigateToAsync("players");
    private async void OnStatsClicked(object sender, EventArgs e) => await NavigateToAsync("stats");
}
