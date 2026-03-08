namespace MauiApp1.Services;

public static class TeamSelectionState
{
    private static string _selectedTeamName = "Select a team";

    public static string SelectedTeamName
    {
        get => _selectedTeamName;
        set
        {
            var normalized = string.IsNullOrWhiteSpace(value) ? "Select a team" : value.Trim();
            if (string.Equals(_selectedTeamName, normalized, StringComparison.Ordinal))
            {
                return;
            }

            _selectedTeamName = normalized;
            TeamChanged?.Invoke(_selectedTeamName);
        }
    }

    public static event Action<string>? TeamChanged;
}
