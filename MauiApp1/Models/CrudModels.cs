namespace MauiApp1.Models;

public class TeamItem
{
    public int TeamId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Coach { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Display => $"#{TeamId} {Name} ({City})";
}

public class PlayerItem
{
    public int PlayerId { get; set; }
    public int TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int? JerseyNumber { get; set; }
    public string Position { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string TeamDisplay => string.IsNullOrWhiteSpace(TeamName) ? $"Team {TeamId}" : TeamName;
    public string Display => $"#{PlayerId} {FullName} (Team {TeamId})";
}

public class GameItem
{
    public int GameId { get; set; }
    public int HomeTeamId { get; set; }
    public int AwayTeamId { get; set; }
    public DateTime GameDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public int? HomeScore { get; set; }
    public int? AwayScore { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Matchup => $"T{HomeTeamId} vs T{AwayTeamId}";
    public string Score => HomeScore.HasValue && AwayScore.HasValue ? $"{HomeScore}-{AwayScore}" : "TBD";
}

public class ScheduleItem
{
    public int ScheduleId { get; set; }
    public int TeamId { get; set; }
    public int GameId { get; set; }
    public bool IsHome { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Side => IsHome ? "Home" : "Away";
    public string Display => $"#{ScheduleId} Team {TeamId} | Game {GameId} | {Side}";
}

public class StatItem
{
    public int StatId { get; set; }
    public int GameId { get; set; }
    public int PlayerId { get; set; }
    public int TwoPtMiss { get; set; }
    public int TwoPtMade { get; set; }
    public int ThreePtMiss { get; set; }
    public int ThreePtMade { get; set; }
    public int Steals { get; set; }
    public int Turnovers { get; set; }
    public int Assists { get; set; }
    public int Blocks { get; set; }
    public int Fouls { get; set; }
    public int OffensiveRebounds { get; set; }
    public int DefensiveRebounds { get; set; }
    public DateTime CreatedAt { get; set; }

    public int TotalPoints => (TwoPtMade * 2) + (ThreePtMade * 3);
    public string Display => $"Stat {StatId}: G{GameId} P{PlayerId} {TotalPoints} pts";
}

public class GamePickerItem
{
    public GameItem? Game { get; set; }
    public string DisplayText { get; set; } = string.Empty;
}
