namespace MauiApp1.Services;

public static class TeamSelectionState
{
    private static string _selectedTeamName = "Select a team";
    private static int? _selectedTeamId;

    public static int? SelectedTeamId => _selectedTeamId;

    public static string SelectedTeamName
    {
        get => _selectedTeamName;
        set => SetSelectedTeam(_selectedTeamId, value);
    }

    public static void SetSelectedTeam(int? teamId, string? teamName)
    {
        var normalized = string.IsNullOrWhiteSpace(teamName) ? "Select a team" : teamName.Trim();
        var idChanged = _selectedTeamId != teamId;
        var nameChanged = !string.Equals(_selectedTeamName, normalized, StringComparison.Ordinal);

        if (!idChanged && !nameChanged)
        {
            return;
        }

        _selectedTeamId = teamId;
        _selectedTeamName = normalized;
        TeamChanged?.Invoke(_selectedTeamName);
    }

    public static event Action<string>? TeamChanged;
}
