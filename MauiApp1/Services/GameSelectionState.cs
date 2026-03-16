namespace MauiApp1.Services;

public static class GameSelectionState
{
    private static int? _selectedGameId;

    public static int? SelectedGameId => _selectedGameId;

    public static void SetSelectedGame(int? gameId)
    {
        _selectedGameId = gameId;
    }
}
