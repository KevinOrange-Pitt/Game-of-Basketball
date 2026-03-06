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
}
