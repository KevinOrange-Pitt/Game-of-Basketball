using Microsoft.Extensions.Logging;
using MauiApp1.Services;

namespace MauiApp1;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<HttpClient>(_ =>
            new HttpClient { BaseAddress = new Uri("http://127.0.0.1:5117/") });
        builder.Services.AddSingleton<DatabaseService>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<ScoreKeepingPage>();
        builder.Services.AddTransient<GameViewPage>();
        builder.Services.AddTransient<StatModePage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}