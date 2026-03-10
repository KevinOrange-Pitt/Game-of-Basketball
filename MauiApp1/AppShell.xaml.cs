namespace MauiApp1;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute("ScoreKeepingPage", typeof(ScoreKeepingPage));
        Routing.RegisterRoute("GameViewPage", typeof(GameViewPage));
        Routing.RegisterRoute("StatModePage", typeof(StatModePage));
    }
}