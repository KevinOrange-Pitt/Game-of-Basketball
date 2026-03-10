using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowAnyOrigin();
    });
});

var app = builder.Build();
app.UseCors();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/db-health", async (IConfiguration config) =>
{
    var result = await TryExecuteWithFallbackAsync(config, GetSchemaCountsAsync);
    if (result.Attempts.Count == 0)
    {
        return Results.Problem(
            title: "Database configuration missing",
            detail: "Configure at least one connection string: SqlDatabaseSqlAuth or SqlDatabase.",
            statusCode: StatusCodes.Status500InternalServerError);
    }

    if (result.SucceededConnection is null || result.Data is null)
    {
        return Results.Problem(
            title: "Database connection failed",
            detail: BuildAttemptsDetail(result.Attempts),
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Ok(new
    {
        status = "ok",
        connection = result.SucceededConnection,
        tables = result.Data
    });
});

app.MapGet("/api/teams", async (IConfiguration config) =>
{
    var result = await TryExecuteWithFallbackAsync(config, LoadTeamsAsync);
    return ToListResult(result, "Database query failed");
});

app.MapGet("/api/teams/{id:int}", async (int id, IConfiguration config) =>
{
    var result = await TryExecuteWithFallbackAsync(config, cs => LoadTeamByIdAsync(cs, id));
    return ToSingleResult(result, $"No team found with Id {id}.");
});

app.MapPost("/api/teams", async (TeamWriteDto team, IConfiguration config) =>
{
    var validation = ValidateTeam(team);
    if (validation is not null)
    {
        return Results.BadRequest(new { message = validation });
    }

    var result = await TryExecuteWithFallbackAsync(config, cs => InsertTeamAsync(cs, team));
    if (result.Attempts.Count == 0)
    {
        return Results.Problem(
            title: "Database configuration missing",
            detail: "Configure at least one connection string: SqlDatabaseSqlAuth or SqlDatabase.",
            statusCode: StatusCodes.Status500InternalServerError);
    }

    if (result.SucceededConnection is null || result.Data is null)
    {
        return Results.Problem(
            title: "Database insert failed",
            detail: BuildAttemptsDetail(result.Attempts),
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Created($"/api/teams/{result.Data.TeamId}", result.Data);
});

app.MapPut("/api/teams/{id:int}", async (int id, TeamWriteDto team, IConfiguration config) =>
{
    var validation = ValidateTeam(team);
    if (validation is not null)
    {
        return Results.BadRequest(new { message = validation });
    }

    var result = await TryExecuteWithFallbackAsync(config, cs => UpdateTeamAsync(cs, id, team));
    if (result.Attempts.Count == 0)
    {
        return Results.Problem(
            title: "Database configuration missing",
            detail: "Configure at least one connection string: SqlDatabaseSqlAuth or SqlDatabase.",
            statusCode: StatusCodes.Status500InternalServerError);
    }

    if (result.SucceededConnection is null)
    {
        return Results.Problem(
            title: "Database update failed",
            detail: BuildAttemptsDetail(result.Attempts),
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    if (!result.Data)
    {
        return Results.NotFound(new { message = $"No team found with Id {id}." });
    }

    return Results.NoContent();
});

app.MapDelete("/api/teams/{id:int}", async (int id, IConfiguration config) =>
{
    var result = await TryExecuteWithFallbackAsync(config, cs => DeleteTeamAsync(cs, id));
    if (result.Attempts.Count == 0)
    {
        return Results.Problem(
            title: "Database configuration missing",
            detail: "Configure at least one connection string: SqlDatabaseSqlAuth or SqlDatabase.",
            statusCode: StatusCodes.Status500InternalServerError);
    }

    if (result.SucceededConnection is null)
    {
        return Results.Problem(
            title: "Database delete failed",
            detail: BuildAttemptsDetail(result.Attempts),
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    if (!result.Data)
    {
        return Results.NotFound(new { message = $"No team found with Id {id}." });
    }

    return Results.NoContent();
});

app.MapGet("/api/players", async (IConfiguration config) =>
{
    var result = await TryExecuteWithFallbackAsync(config, LoadPlayersAsync);
    return ToListResult(result, "Database query failed");
});

app.MapGet("/api/players/{id:int}", async (int id, IConfiguration config) =>
{
    var result = await TryExecuteWithFallbackAsync(config, cs => LoadPlayerByIdAsync(cs, id));
    return ToSingleResult(result, $"No player found with Id {id}.");
});

app.MapPost("/api/players", async (PlayerWriteDto player, IConfiguration config) =>
{
    var validation = ValidatePlayer(player);
    if (validation is not null)
    {
        return Results.BadRequest(new { message = validation });
    }

    var result = await TryExecuteWithFallbackAsync(config, cs => InsertPlayerAsync(cs, player));
    if (result.Attempts.Count == 0)
    {
        return Results.Problem(
            title: "Database configuration missing",
            detail: "Configure at least one connection string: SqlDatabaseSqlAuth or SqlDatabase.",
            statusCode: StatusCodes.Status500InternalServerError);
    }

    if (result.SucceededConnection is null || result.Data is null)
    {
        return Results.Problem(
            title: "Database insert failed",
            detail: BuildAttemptsDetail(result.Attempts),
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Created($"/api/players/{result.Data.PlayerId}", result.Data);
});

app.MapPut("/api/players/{id:int}", async (int id, PlayerWriteDto player, IConfiguration config) =>
{
    var validation = ValidatePlayer(player);
    if (validation is not null)
    {
        return Results.BadRequest(new { message = validation });
    }

    var result = await TryExecuteWithFallbackAsync(config, cs => UpdatePlayerAsync(cs, id, player));
    if (result.Attempts.Count == 0)
    {
        return Results.Problem(
            title: "Database configuration missing",
            detail: "Configure at least one connection string: SqlDatabaseSqlAuth or SqlDatabase.",
            statusCode: StatusCodes.Status500InternalServerError);
    }

    if (result.SucceededConnection is null)
    {
        return Results.Problem(
            title: "Database update failed",
            detail: BuildAttemptsDetail(result.Attempts),
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    if (!result.Data)
    {
        return Results.NotFound(new { message = $"No player found with Id {id}." });
    }

    return Results.NoContent();
});

app.MapDelete("/api/players/{id:int}", async (int id, IConfiguration config) =>
{
    var result = await TryExecuteWithFallbackAsync(config, cs => DeletePlayerAsync(cs, id));
    if (result.Attempts.Count == 0)
    {
        return Results.Problem(
            title: "Database configuration missing",
            detail: "Configure at least one connection string: SqlDatabaseSqlAuth or SqlDatabase.",
            statusCode: StatusCodes.Status500InternalServerError);
    }

    if (result.SucceededConnection is null)
    {
        return Results.Problem(
            title: "Database delete failed",
            detail: BuildAttemptsDetail(result.Attempts),
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    if (!result.Data)
    {
        return Results.NotFound(new { message = $"No player found with Id {id}." });
    }

    return Results.NoContent();
});

app.MapGet("/api/games", async (IConfiguration config) =>
{
    var result = await TryExecuteWithFallbackAsync(config, LoadGamesAsync);
    return ToListResult(result, "Database query failed");
});

app.MapGet("/api/games/{id:int}", async (int id, IConfiguration config) =>
{
    var result = await TryExecuteWithFallbackAsync(config, cs => LoadGameByIdAsync(cs, id));
    return ToSingleResult(result, $"No game found with Id {id}.");
});

app.MapPost("/api/games", async (GameWriteDto game, IConfiguration config) =>
{
    var validation = ValidateGame(game);
    if (validation is not null)
    {
        return Results.BadRequest(new { message = validation });
    }

    var result = await TryExecuteWithFallbackAsync(config, cs => InsertGameAsync(cs, game));
    if (result.Attempts.Count == 0)
    {
        return Results.Problem(
            title: "Database configuration missing",
            detail: "Configure at least one connection string: SqlDatabaseSqlAuth or SqlDatabase.",
            statusCode: StatusCodes.Status500InternalServerError);
    }

    if (result.SucceededConnection is null || result.Data is null)
    {
        return Results.Problem(
            title: "Database insert failed",
            detail: BuildAttemptsDetail(result.Attempts),
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Created($"/api/games/{result.Data.GameId}", result.Data);
});

app.MapPut("/api/games/{id:int}", async (int id, GameWriteDto game, IConfiguration config) =>
{
    var validation = ValidateGame(game);
    if (validation is not null)
    {
        return Results.BadRequest(new { message = validation });
    }

    var result = await TryExecuteWithFallbackAsync(config, cs => UpdateGameAsync(cs, id, game));
    if (result.Attempts.Count == 0)
    {
        return Results.Problem(
            title: "Database configuration missing",
            detail: "Configure at least one connection string: SqlDatabaseSqlAuth or SqlDatabase.",
            statusCode: StatusCodes.Status500InternalServerError);
    }

    if (result.SucceededConnection is null)
    {
        return Results.Problem(
            title: "Database update failed",
            detail: BuildAttemptsDetail(result.Attempts),
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    if (!result.Data)
    {
        return Results.NotFound(new { message = $"No game found with Id {id}." });
    }

    return Results.NoContent();
});

app.MapDelete("/api/games/{id:int}", async (int id, IConfiguration config) =>
{
    var result = await TryExecuteWithFallbackAsync(config, cs => DeleteGameAsync(cs, id));
    if (result.Attempts.Count == 0)
    {
        return Results.Problem(
            title: "Database configuration missing",
            detail: "Configure at least one connection string: SqlDatabaseSqlAuth or SqlDatabase.",
            statusCode: StatusCodes.Status500InternalServerError);
    }

    if (result.SucceededConnection is null)
    {
        return Results.Problem(
            title: "Database delete failed",
            detail: BuildAttemptsDetail(result.Attempts),
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    if (!result.Data)
    {
        return Results.NotFound(new { message = $"No game found with Id {id}." });
    }

    return Results.NoContent();
});

app.MapGet("/api/schedules", async (IConfiguration config) =>
{
    var result = await TryExecuteWithFallbackAsync(config, LoadSchedulesAsync);
    return ToListResult(result, "Database query failed");
});

app.MapGet("/api/schedules/{id:int}", async (int id, IConfiguration config) =>
{
    var result = await TryExecuteWithFallbackAsync(config, cs => LoadScheduleByIdAsync(cs, id));
    return ToSingleResult(result, $"No schedule found with Id {id}.");
});

app.MapPost("/api/schedules", async (ScheduleWriteDto schedule, IConfiguration config) =>
{
    var validation = ValidateSchedule(schedule);
    if (validation is not null)
    {
        return Results.BadRequest(new { message = validation });
    }

    var result = await TryExecuteWithFallbackAsync(config, cs => InsertScheduleAsync(cs, schedule));
    if (result.Attempts.Count == 0)
    {
        return Results.Problem(
            title: "Database configuration missing",
            detail: "Configure at least one connection string: SqlDatabaseSqlAuth or SqlDatabase.",
            statusCode: StatusCodes.Status500InternalServerError);
    }

    if (result.SucceededConnection is null || result.Data is null)
    {
        return Results.Problem(
            title: "Database insert failed",
            detail: BuildAttemptsDetail(result.Attempts),
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Created($"/api/schedules/{result.Data.ScheduleId}", result.Data);
});

app.MapPut("/api/schedules/{id:int}", async (int id, ScheduleWriteDto schedule, IConfiguration config) =>
{
    var validation = ValidateSchedule(schedule);
    if (validation is not null)
    {
        return Results.BadRequest(new { message = validation });
    }

    var result = await TryExecuteWithFallbackAsync(config, cs => UpdateScheduleAsync(cs, id, schedule));
    if (result.Attempts.Count == 0)
    {
        return Results.Problem(
            title: "Database configuration missing",
            detail: "Configure at least one connection string: SqlDatabaseSqlAuth or SqlDatabase.",
            statusCode: StatusCodes.Status500InternalServerError);
    }

    if (result.SucceededConnection is null)
    {
        return Results.Problem(
            title: "Database update failed",
            detail: BuildAttemptsDetail(result.Attempts),
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    if (!result.Data)
    {
        return Results.NotFound(new { message = $"No schedule found with Id {id}." });
    }

    return Results.NoContent();
});

app.MapDelete("/api/schedules/{id:int}", async (int id, IConfiguration config) =>
{
    var result = await TryExecuteWithFallbackAsync(config, cs => DeleteScheduleAsync(cs, id));
    if (result.Attempts.Count == 0)
    {
        return Results.Problem(
            title: "Database configuration missing",
            detail: "Configure at least one connection string: SqlDatabaseSqlAuth or SqlDatabase.",
            statusCode: StatusCodes.Status500InternalServerError);
    }

    if (result.SucceededConnection is null)
    {
        return Results.Problem(
            title: "Database delete failed",
            detail: BuildAttemptsDetail(result.Attempts),
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    if (!result.Data)
    {
        return Results.NotFound(new { message = $"No schedule found with Id {id}." });
    }

    return Results.NoContent();
});


app.MapGet("/api/stats", async (IConfiguration config) =>
{
    var result = await TryExecuteWithFallbackAsync(config, LoadStatsAsync);
    return ToListResult(result, "Database query failed");
});

app.MapGet("/api/stats/{id:int}", async (int id, IConfiguration config) =>
{
    var result = await TryExecuteWithFallbackAsync(config, cs => LoadStatByIdAsync(cs, id));
    return ToSingleResult(result, $"No stat found with Id {id}.");
});

app.MapGet("/api/stats/game/{gameId:int}", async (int gameId, IConfiguration config) =>
{
    var result = await TryExecuteWithFallbackAsync(config, cs => LoadStatsByGameAsync(cs, gameId));
    return ToListResult(result, "Database query failed");
});

app.MapGet("/api/stats/player/{playerId:int}", async (int playerId, IConfiguration config) =>
{
    var result = await TryExecuteWithFallbackAsync(config, cs => LoadStatsByPlayerAsync(cs, playerId));
    return ToListResult(result, "Database query failed");
});

app.MapPost("/api/stats", async (StatWriteDto stat, IConfiguration config) =>
{
    var validation = ValidateStat(stat);
    if (validation is not null)
        return Results.BadRequest(new { message = validation });

    var result = await TryExecuteWithFallbackAsync(config, cs => InsertStatAsync(cs, stat));
    if (result.Attempts.Count == 0)
        return Results.Problem(title: "Database configuration missing",
            detail: "Configure at least one connection string: SqlDatabaseSqlAuth or SqlDatabase.",
            statusCode: StatusCodes.Status500InternalServerError);

    if (result.SucceededConnection is null || result.Data is null)
        return Results.Problem(title: "Database insert failed",
            detail: BuildAttemptsDetail(result.Attempts),
            statusCode: StatusCodes.Status503ServiceUnavailable);

    return Results.Created($"/api/stats/{result.Data.StatId}", result.Data);
});

app.MapPut("/api/stats/{id:int}", async (int id, StatWriteDto stat, IConfiguration config) =>
{
    var validation = ValidateStat(stat);
    if (validation is not null)
        return Results.BadRequest(new { message = validation });

    var result = await TryExecuteWithFallbackAsync(config, cs => UpdateStatAsync(cs, id, stat));
    if (result.Attempts.Count == 0)
        return Results.Problem(title: "Database configuration missing",
            detail: "Configure at least one connection string: SqlDatabaseSqlAuth or SqlDatabase.",
            statusCode: StatusCodes.Status500InternalServerError);

    if (result.SucceededConnection is null)
        return Results.Problem(title: "Database update failed",
            detail: BuildAttemptsDetail(result.Attempts),
            statusCode: StatusCodes.Status503ServiceUnavailable);

    if (!result.Data)
        return Results.NotFound(new { message = $"No stat found with Id {id}." });

    return Results.NoContent();
});

app.MapDelete("/api/stats/{id:int}", async (int id, IConfiguration config) =>
{
    var result = await TryExecuteWithFallbackAsync(config, cs => DeleteStatAsync(cs, id));
    if (result.Attempts.Count == 0)
        return Results.Problem(title: "Database configuration missing",
            detail: "Configure at least one connection string: SqlDatabaseSqlAuth or SqlDatabase.",
            statusCode: StatusCodes.Status500InternalServerError);

    if (result.SucceededConnection is null)
        return Results.Problem(title: "Database delete failed",
            detail: BuildAttemptsDetail(result.Attempts),
            statusCode: StatusCodes.Status503ServiceUnavailable);

    if (!result.Data)
        return Results.NotFound(new { message = $"No stat found with Id {id}." });

    return Results.NoContent();
});


app.Run();

static IResult ToListResult<T>(DbOperationResult<List<T>> result, string title)
{
    if (result.Attempts.Count == 0)
    {
        return Results.Problem(
            title: "Database configuration missing",
            detail: "Configure at least one connection string: SqlDatabaseSqlAuth or SqlDatabase.",
            statusCode: StatusCodes.Status500InternalServerError);
    }

    if (result.SucceededConnection is null)
    {
        return Results.Problem(
            title: title,
            detail: BuildAttemptsDetail(result.Attempts),
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Ok(result.Data ?? new List<T>());
}

static IResult ToSingleResult<T>(DbOperationResult<T?> result, string notFoundMessage)
{
    if (result.Attempts.Count == 0)
    {
        return Results.Problem(
            title: "Database configuration missing",
            detail: "Configure at least one connection string: SqlDatabaseSqlAuth or SqlDatabase.",
            statusCode: StatusCodes.Status500InternalServerError);
    }

    if (result.SucceededConnection is null)
    {
        return Results.Problem(
            title: "Database query failed",
            detail: BuildAttemptsDetail(result.Attempts),
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    if (result.Data is null)
    {
        return Results.NotFound(new { message = notFoundMessage });
    }

    return Results.Ok(result.Data);
}

static async Task<DbOperationResult<T>> TryExecuteWithFallbackAsync<T>(
    IConfiguration config,
    Func<string, Task<T>> action)
{
    var attempts = new List<ConnectionAttempt>();

    foreach (var candidate in GetConnectionCandidates(config))
    {
        try
        {
            var data = await action(candidate.ConnectionString);
            attempts.Add(new ConnectionAttempt(candidate.Name, true, null));
            return new DbOperationResult<T>(data, candidate.Name, attempts);
        }
        catch (Exception ex)
        {
            attempts.Add(new ConnectionAttempt(candidate.Name, false, ex.Message));
        }
    }

    return new DbOperationResult<T>(default, null, attempts);
}

static IEnumerable<ConnectionCandidate> GetConnectionCandidates(IConfiguration config)
{
    var keys = new[] { "SqlDatabaseSqlAuth", "SqlDatabase" };
    var seen = new HashSet<string>(StringComparer.Ordinal);

    foreach (var key in keys)
    {
        var connectionString = config.GetConnectionString(key);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            continue;
        }

        if (!seen.Add(connectionString))
        {
            continue;
        }

        yield return new ConnectionCandidate(key, connectionString);
    }
}

static string BuildAttemptsDetail(IEnumerable<ConnectionAttempt> attempts)
{
    var lines = attempts.Select(attempt =>
        $"{attempt.Name}: {(attempt.Success ? "Success" : $"Failed ({attempt.Error})")}");

    return "Tried connection strings in order. " + string.Join(" | ", lines);
}

static async Task<SqlConnection> OpenConnectionAsync(string connectionString)
{
    var connection = new SqlConnection(connectionString);

    try
    {
        await connection.OpenAsync();
        return connection;
    }
    catch
    {
        await connection.DisposeAsync();
        throw;
    }
}

static async Task<SchemaCountsDto> GetSchemaCountsAsync(string connectionString)
{
    await using var connection = await OpenConnectionAsync(connectionString);

    const string sql = @"
SELECT
    (SELECT COUNT(1) FROM dbo.Teams) AS TeamsCount,
    (SELECT COUNT(1) FROM dbo.Players) AS PlayersCount,
    (SELECT COUNT(1) FROM dbo.Games) AS GamesCount,
    (SELECT COUNT(1) FROM dbo.Schedules) AS SchedulesCount;";

    await using var command = new SqlCommand(sql, connection);
    await using var reader = await command.ExecuteReaderAsync();

    if (!await reader.ReadAsync())
    {
        return new SchemaCountsDto(0, 0, 0, 0);
    }

    return new SchemaCountsDto(
        reader.GetInt32(0),
        reader.GetInt32(1),
        reader.GetInt32(2),
        reader.GetInt32(3));
}

static async Task<List<TeamDto>> LoadTeamsAsync(string connectionString)
{
    var teams = new List<TeamDto>();
    await using var connection = await OpenConnectionAsync(connectionString);

    const string sql = @"
SELECT team_id, name, city, coach, created_at
FROM dbo.Teams
ORDER BY team_id;";

    await using var command = new SqlCommand(sql, connection);
    await using var reader = await command.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        teams.Add(new TeamDto(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
            reader.GetDateTime(4)));
    }

    return teams;
}

static async Task<TeamDto?> LoadTeamByIdAsync(string connectionString, int id)
{
    await using var connection = await OpenConnectionAsync(connectionString);

    const string sql = @"
SELECT team_id, name, city, coach, created_at
FROM dbo.Teams
WHERE team_id = @id;";

    await using var command = new SqlCommand(sql, connection);
    command.Parameters.AddWithValue("@id", id);
    await using var reader = await command.ExecuteReaderAsync();

    if (!await reader.ReadAsync())
    {
        return null;
    }

    return new TeamDto(
        reader.GetInt32(0),
        reader.GetString(1),
        reader.GetString(2),
        reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
        reader.GetDateTime(4));
}

static async Task<TeamDto> InsertTeamAsync(string connectionString, TeamWriteDto team)
{
    await using var connection = await OpenConnectionAsync(connectionString);

    const string sql = @"
INSERT INTO dbo.Teams (name, city, coach)
OUTPUT inserted.team_id, inserted.created_at
VALUES (@name, @city, @coach);";

    await using var command = new SqlCommand(sql, connection);
    command.Parameters.AddWithValue("@name", team.Name.Trim());
    command.Parameters.AddWithValue("@city", team.City.Trim());
    command.Parameters.AddWithValue("@coach", string.IsNullOrWhiteSpace(team.Coach) ? DBNull.Value : team.Coach.Trim());

    await using var reader = await command.ExecuteReaderAsync();
    await reader.ReadAsync();

    return new TeamDto(
        reader.GetInt32(0),
        team.Name.Trim(),
        team.City.Trim(),
        team.Coach.Trim(),
        reader.GetDateTime(1));
}

static async Task<bool> UpdateTeamAsync(string connectionString, int id, TeamWriteDto team)
{
    await using var connection = await OpenConnectionAsync(connectionString);

    const string sql = @"
UPDATE dbo.Teams
SET name = @name,
    city = @city,
    coach = @coach
WHERE team_id = @id;";

    await using var command = new SqlCommand(sql, connection);
    command.Parameters.AddWithValue("@id", id);
    command.Parameters.AddWithValue("@name", team.Name.Trim());
    command.Parameters.AddWithValue("@city", team.City.Trim());
    command.Parameters.AddWithValue("@coach", string.IsNullOrWhiteSpace(team.Coach) ? DBNull.Value : team.Coach.Trim());

    return await command.ExecuteNonQueryAsync() > 0;
}

static async Task<bool> DeleteTeamAsync(string connectionString, int id)
{
    await using var connection = await OpenConnectionAsync(connectionString);
    await using var command = new SqlCommand("DELETE FROM dbo.Teams WHERE team_id = @id;", connection);
    command.Parameters.AddWithValue("@id", id);
    return await command.ExecuteNonQueryAsync() > 0;
}

static async Task<List<PlayerDto>> LoadPlayersAsync(string connectionString)
{
    var players = new List<PlayerDto>();
    await using var connection = await OpenConnectionAsync(connectionString);

    const string sql = @"
SELECT p.player_id, p.team_id, p.first_name, p.last_name, p.jersey_number, p.position, p.created_at, t.name AS team_name
FROM dbo.Players p
INNER JOIN dbo.Teams t ON t.team_id = p.team_id
ORDER BY player_id;";

    await using var command = new SqlCommand(sql, connection);
    await using var reader = await command.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        players.Add(new PlayerDto(
            reader.GetInt32(0),
            reader.GetInt32(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.IsDBNull(4) ? null : reader.GetInt32(4),
            reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
            reader.GetDateTime(6),
            reader.IsDBNull(7) ? string.Empty : reader.GetString(7)));
    }

    return players;
}

static async Task<PlayerDto?> LoadPlayerByIdAsync(string connectionString, int id)
{
    await using var connection = await OpenConnectionAsync(connectionString);

    const string sql = @"
SELECT p.player_id, p.team_id, p.first_name, p.last_name, p.jersey_number, p.position, p.created_at, t.name AS team_name
FROM dbo.Players p
INNER JOIN dbo.Teams t ON t.team_id = p.team_id
WHERE p.player_id = @id;";

    await using var command = new SqlCommand(sql, connection);
    command.Parameters.AddWithValue("@id", id);
    await using var reader = await command.ExecuteReaderAsync();

    if (!await reader.ReadAsync())
    {
        return null;
    }

    return new PlayerDto(
        reader.GetInt32(0),
        reader.GetInt32(1),
        reader.GetString(2),
        reader.GetString(3),
        reader.IsDBNull(4) ? null : reader.GetInt32(4),
        reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
        reader.GetDateTime(6),
        reader.IsDBNull(7) ? string.Empty : reader.GetString(7));
}

static async Task<PlayerDto> InsertPlayerAsync(string connectionString, PlayerWriteDto player)
{
    await using var connection = await OpenConnectionAsync(connectionString);

    const string sql = @"
INSERT INTO dbo.Players (team_id, first_name, last_name, jersey_number, position)
OUTPUT inserted.player_id, inserted.created_at
VALUES (@team_id, @first_name, @last_name, @jersey_number, @position);";

    await using var command = new SqlCommand(sql, connection);
    command.Parameters.AddWithValue("@team_id", player.TeamId);
    command.Parameters.AddWithValue("@first_name", player.FirstName.Trim());
    command.Parameters.AddWithValue("@last_name", player.LastName.Trim());
    command.Parameters.AddWithValue("@jersey_number", player.JerseyNumber.HasValue ? player.JerseyNumber.Value : DBNull.Value);
    command.Parameters.AddWithValue("@position", string.IsNullOrWhiteSpace(player.Position) ? DBNull.Value : player.Position.Trim());

    await using var reader = await command.ExecuteReaderAsync();
    await reader.ReadAsync();

    return new PlayerDto(
        reader.GetInt32(0),
        player.TeamId,
        player.FirstName.Trim(),
        player.LastName.Trim(),
        player.JerseyNumber,
        player.Position.Trim(),
        reader.GetDateTime(1),
        string.Empty);
}

static async Task<bool> UpdatePlayerAsync(string connectionString, int id, PlayerWriteDto player)
{
    await using var connection = await OpenConnectionAsync(connectionString);

    const string sql = @"
UPDATE dbo.Players
SET team_id = @team_id,
    first_name = @first_name,
    last_name = @last_name,
    jersey_number = @jersey_number,
    position = @position
WHERE player_id = @id;";

    await using var command = new SqlCommand(sql, connection);
    command.Parameters.AddWithValue("@id", id);
    command.Parameters.AddWithValue("@team_id", player.TeamId);
    command.Parameters.AddWithValue("@first_name", player.FirstName.Trim());
    command.Parameters.AddWithValue("@last_name", player.LastName.Trim());
    command.Parameters.AddWithValue("@jersey_number", player.JerseyNumber.HasValue ? player.JerseyNumber.Value : DBNull.Value);
    command.Parameters.AddWithValue("@position", string.IsNullOrWhiteSpace(player.Position) ? DBNull.Value : player.Position.Trim());

    return await command.ExecuteNonQueryAsync() > 0;
}

static async Task<bool> DeletePlayerAsync(string connectionString, int id)
{
    await using var connection = await OpenConnectionAsync(connectionString);
    await using var command = new SqlCommand("DELETE FROM dbo.Players WHERE player_id = @id;", connection);
    command.Parameters.AddWithValue("@id", id);
    return await command.ExecuteNonQueryAsync() > 0;
}

static async Task<List<GameDto>> LoadGamesAsync(string connectionString)
{
    var games = new List<GameDto>();
    await using var connection = await OpenConnectionAsync(connectionString);

    const string sql = @"
SELECT game_id, home_team_id, away_team_id, game_date, location, home_score, away_score, status, created_at
FROM dbo.Games
ORDER BY game_id;";

    await using var command = new SqlCommand(sql, connection);
    await using var reader = await command.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        games.Add(new GameDto(
            reader.GetInt32(0),
            reader.GetInt32(1),
            reader.GetInt32(2),
            reader.GetDateTime(3),
            reader.GetString(4),
            reader.IsDBNull(5) ? null : reader.GetInt32(5),
            reader.IsDBNull(6) ? null : reader.GetInt32(6),
            reader.GetString(7),
            reader.GetDateTime(8)));
    }

    return games;
}

static async Task<GameDto?> LoadGameByIdAsync(string connectionString, int id)
{
    await using var connection = await OpenConnectionAsync(connectionString);

    const string sql = @"
SELECT game_id, home_team_id, away_team_id, game_date, location, home_score, away_score, status, created_at
FROM dbo.Games
WHERE game_id = @id;";

    await using var command = new SqlCommand(sql, connection);
    command.Parameters.AddWithValue("@id", id);
    await using var reader = await command.ExecuteReaderAsync();

    if (!await reader.ReadAsync())
    {
        return null;
    }

    return new GameDto(
        reader.GetInt32(0),
        reader.GetInt32(1),
        reader.GetInt32(2),
        reader.GetDateTime(3),
        reader.GetString(4),
        reader.IsDBNull(5) ? null : reader.GetInt32(5),
        reader.IsDBNull(6) ? null : reader.GetInt32(6),
        reader.GetString(7),
        reader.GetDateTime(8));
}

static async Task<GameDto> InsertGameAsync(string connectionString, GameWriteDto game)
{
    await using var connection = await OpenConnectionAsync(connectionString);

    const string sql = @"
INSERT INTO dbo.Games (home_team_id, away_team_id, game_date, location, home_score, away_score, status)
OUTPUT inserted.game_id, inserted.created_at
VALUES (@home_team_id, @away_team_id, @game_date, @location, @home_score, @away_score, @status);";

    await using var command = new SqlCommand(sql, connection);
    command.Parameters.AddWithValue("@home_team_id", game.HomeTeamId);
    command.Parameters.AddWithValue("@away_team_id", game.AwayTeamId);
    command.Parameters.AddWithValue("@game_date", game.GameDate);
    command.Parameters.AddWithValue("@location", game.Location.Trim());
    command.Parameters.AddWithValue("@home_score", game.HomeScore.HasValue ? game.HomeScore.Value : DBNull.Value);
    command.Parameters.AddWithValue("@away_score", game.AwayScore.HasValue ? game.AwayScore.Value : DBNull.Value);
    command.Parameters.AddWithValue("@status", game.Status.Trim());

    await using var reader = await command.ExecuteReaderAsync();
    await reader.ReadAsync();

    return new GameDto(
        reader.GetInt32(0),
        game.HomeTeamId,
        game.AwayTeamId,
        game.GameDate,
        game.Location.Trim(),
        game.HomeScore,
        game.AwayScore,
        game.Status.Trim(),
        reader.GetDateTime(1));
}

static async Task<bool> UpdateGameAsync(string connectionString, int id, GameWriteDto game)
{
    await using var connection = await OpenConnectionAsync(connectionString);

    const string sql = @"
UPDATE dbo.Games
SET home_team_id = @home_team_id,
    away_team_id = @away_team_id,
    game_date = @game_date,
    location = @location,
    home_score = @home_score,
    away_score = @away_score,
    status = @status
WHERE game_id = @id;";

    await using var command = new SqlCommand(sql, connection);
    command.Parameters.AddWithValue("@id", id);
    command.Parameters.AddWithValue("@home_team_id", game.HomeTeamId);
    command.Parameters.AddWithValue("@away_team_id", game.AwayTeamId);
    command.Parameters.AddWithValue("@game_date", game.GameDate);
    command.Parameters.AddWithValue("@location", game.Location.Trim());
    command.Parameters.AddWithValue("@home_score", game.HomeScore.HasValue ? game.HomeScore.Value : DBNull.Value);
    command.Parameters.AddWithValue("@away_score", game.AwayScore.HasValue ? game.AwayScore.Value : DBNull.Value);
    command.Parameters.AddWithValue("@status", game.Status.Trim());

    return await command.ExecuteNonQueryAsync() > 0;
}

static async Task<bool> DeleteGameAsync(string connectionString, int id)
{
    await using var connection = await OpenConnectionAsync(connectionString);
    await using var command = new SqlCommand("DELETE FROM dbo.Games WHERE game_id = @id;", connection);
    command.Parameters.AddWithValue("@id", id);
    return await command.ExecuteNonQueryAsync() > 0;
}

static async Task<List<ScheduleDto>> LoadSchedulesAsync(string connectionString)
{
    var schedules = new List<ScheduleDto>();
    await using var connection = await OpenConnectionAsync(connectionString);

    const string sql = @"
SELECT schedule_id, team_id, game_id, is_home, created_at
FROM dbo.Schedules
ORDER BY schedule_id;";

    await using var command = new SqlCommand(sql, connection);
    await using var reader = await command.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        schedules.Add(new ScheduleDto(
            reader.GetInt32(0),
            reader.GetInt32(1),
            reader.GetInt32(2),
            reader.GetBoolean(3),
            reader.GetDateTime(4)));
    }

    return schedules;
}

static async Task<ScheduleDto?> LoadScheduleByIdAsync(string connectionString, int id)
{
    await using var connection = await OpenConnectionAsync(connectionString);

    const string sql = @"
SELECT schedule_id, team_id, game_id, is_home, created_at
FROM dbo.Schedules
WHERE schedule_id = @id;";

    await using var command = new SqlCommand(sql, connection);
    command.Parameters.AddWithValue("@id", id);
    await using var reader = await command.ExecuteReaderAsync();

    if (!await reader.ReadAsync())
    {
        return null;
    }

    return new ScheduleDto(
        reader.GetInt32(0),
        reader.GetInt32(1),
        reader.GetInt32(2),
        reader.GetBoolean(3),
        reader.GetDateTime(4));
}

static async Task<ScheduleDto> InsertScheduleAsync(string connectionString, ScheduleWriteDto schedule)
{
    await using var connection = await OpenConnectionAsync(connectionString);

    const string sql = @"
INSERT INTO dbo.Schedules (team_id, game_id, is_home)
OUTPUT inserted.schedule_id, inserted.created_at
VALUES (@team_id, @game_id, @is_home);";

    await using var command = new SqlCommand(sql, connection);
    command.Parameters.AddWithValue("@team_id", schedule.TeamId);
    command.Parameters.AddWithValue("@game_id", schedule.GameId);
    command.Parameters.AddWithValue("@is_home", schedule.IsHome);

    await using var reader = await command.ExecuteReaderAsync();
    await reader.ReadAsync();

    return new ScheduleDto(
        reader.GetInt32(0),
        schedule.TeamId,
        schedule.GameId,
        schedule.IsHome,
        reader.GetDateTime(1));
}

static async Task<bool> UpdateScheduleAsync(string connectionString, int id, ScheduleWriteDto schedule)
{
    await using var connection = await OpenConnectionAsync(connectionString);

    const string sql = @"
UPDATE dbo.Schedules
SET team_id = @team_id,
    game_id = @game_id,
    is_home = @is_home
WHERE schedule_id = @id;";

    await using var command = new SqlCommand(sql, connection);
    command.Parameters.AddWithValue("@id", id);
    command.Parameters.AddWithValue("@team_id", schedule.TeamId);
    command.Parameters.AddWithValue("@game_id", schedule.GameId);
    command.Parameters.AddWithValue("@is_home", schedule.IsHome);

    return await command.ExecuteNonQueryAsync() > 0;
}

static async Task<bool> DeleteScheduleAsync(string connectionString, int id)
{
    await using var connection = await OpenConnectionAsync(connectionString);
    await using var command = new SqlCommand("DELETE FROM dbo.Schedules WHERE schedule_id = @id;", connection);
    command.Parameters.AddWithValue("@id", id);
    return await command.ExecuteNonQueryAsync() > 0;
}

static string? ValidateTeam(TeamWriteDto team)
{
    if (string.IsNullOrWhiteSpace(team.Name))
    {
        return "Team name is required.";
    }

    if (string.IsNullOrWhiteSpace(team.City))
    {
        return "City is required.";
    }

    return null;
}

static string? ValidatePlayer(PlayerWriteDto player)
{
    if (player.TeamId <= 0)
    {
        return "Team Id must be greater than zero.";
    }

    if (string.IsNullOrWhiteSpace(player.FirstName))
    {
        return "First name is required.";
    }

    if (string.IsNullOrWhiteSpace(player.LastName))
    {
        return "Last name is required.";
    }

    return null;
}

static string? ValidateGame(GameWriteDto game)
{
    if (game.HomeTeamId <= 0 || game.AwayTeamId <= 0)
    {
        return "Home team and away team Ids must be greater than zero.";
    }

    if (game.HomeTeamId == game.AwayTeamId)
    {
        return "Home and away team cannot be the same.";
    }

    if (string.IsNullOrWhiteSpace(game.Location))
    {
        return "Location is required.";
    }

    if (string.IsNullOrWhiteSpace(game.Status))
    {
        return "Status is required.";
    }

    return null;
}

static string? ValidateSchedule(ScheduleWriteDto schedule)
{
    if (schedule.TeamId <= 0)
    {
        return "Team Id must be greater than zero.";
    }

    if (schedule.GameId <= 0)
    {
        return "Game Id must be greater than zero.";
    }

    return null;
}


static async Task<List<StatDto>> LoadStatsAsync(string connectionString)
{
    var stats = new List<StatDto>();
    await using var connection = await OpenConnectionAsync(connectionString);

    const string sql = @"
SELECT stat_id, game_id, player_id, two_pt_miss, two_pt_made, three_pt_miss, three_pt_made,
       steals, turnovers, assists, blocks, fouls, offensive_rebounds, defensive_rebounds, created_at
FROM dbo.Stats
ORDER BY stat_id;";

    await using var command = new SqlCommand(sql, connection);
    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
        stats.Add(ReadStat(reader));

    return stats;
}

static async Task<StatDto?> LoadStatByIdAsync(string connectionString, int id)
{
    await using var connection = await OpenConnectionAsync(connectionString);

    const string sql = @"
SELECT stat_id, game_id, player_id, two_pt_miss, two_pt_made, three_pt_miss, three_pt_made,
       steals, turnovers, assists, blocks, fouls, offensive_rebounds, defensive_rebounds, created_at
FROM dbo.Stats WHERE stat_id = @id;";

    await using var command = new SqlCommand(sql, connection);
    command.Parameters.AddWithValue("@id", id);
    await using var reader = await command.ExecuteReaderAsync();
    return await reader.ReadAsync() ? ReadStat(reader) : null;
}

static async Task<List<StatDto>> LoadStatsByGameAsync(string connectionString, int gameId)
{
    var stats = new List<StatDto>();
    await using var connection = await OpenConnectionAsync(connectionString);

    const string sql = @"
SELECT stat_id, game_id, player_id, two_pt_miss, two_pt_made, three_pt_miss, three_pt_made,
       steals, turnovers, assists, blocks, fouls, offensive_rebounds, defensive_rebounds, created_at
FROM dbo.Stats WHERE game_id = @gameId ORDER BY player_id;";

    await using var command = new SqlCommand(sql, connection);
    command.Parameters.AddWithValue("@gameId", gameId);
    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
        stats.Add(ReadStat(reader));

    return stats;
}

static async Task<List<StatDto>> LoadStatsByPlayerAsync(string connectionString, int playerId)
{
    var stats = new List<StatDto>();
    await using var connection = await OpenConnectionAsync(connectionString);

    const string sql = @"
SELECT stat_id, game_id, player_id, two_pt_miss, two_pt_made, three_pt_miss, three_pt_made,
       steals, turnovers, assists, blocks, fouls, offensive_rebounds, defensive_rebounds, created_at
FROM dbo.Stats WHERE player_id = @playerId ORDER BY game_id;";

    await using var command = new SqlCommand(sql, connection);
    command.Parameters.AddWithValue("@playerId", playerId);
    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
        stats.Add(ReadStat(reader));

    return stats;
}

static async Task<StatDto> InsertStatAsync(string connectionString, StatWriteDto stat)
{
    await using var connection = await OpenConnectionAsync(connectionString);

    const string sql = @"
INSERT INTO dbo.Stats
    (game_id, player_id, two_pt_miss, two_pt_made, three_pt_miss, three_pt_made,
     steals, turnovers, assists, blocks, fouls, offensive_rebounds, defensive_rebounds)
OUTPUT inserted.stat_id, inserted.created_at
VALUES
    (@game_id, @player_id, @two_pt_miss, @two_pt_made, @three_pt_miss, @three_pt_made,
     @steals, @turnovers, @assists, @blocks, @fouls, @offensive_rebounds, @defensive_rebounds);";

    await using var command = new SqlCommand(sql, connection);
    AddStatParams(command, stat);

    await using var reader = await command.ExecuteReaderAsync();
    await reader.ReadAsync();

    return new StatDto(reader.GetInt32(0), stat.GameId, stat.PlayerId,
        stat.TwoPtMiss, stat.TwoPtMade, stat.ThreePtMiss, stat.ThreePtMade,
        stat.Steals, stat.Turnovers, stat.Assists, stat.Blocks, stat.Fouls,
        stat.OffensiveRebounds, stat.DefensiveRebounds, reader.GetDateTime(1));
}

static async Task<bool> UpdateStatAsync(string connectionString, int id, StatWriteDto stat)
{
    await using var connection = await OpenConnectionAsync(connectionString);

    const string sql = @"
UPDATE dbo.Stats SET
    game_id = @game_id, player_id = @player_id,
    two_pt_miss = @two_pt_miss, two_pt_made = @two_pt_made,
    three_pt_miss = @three_pt_miss, three_pt_made = @three_pt_made,
    steals = @steals, turnovers = @turnovers, assists = @assists,
    blocks = @blocks, fouls = @fouls,
    offensive_rebounds = @offensive_rebounds, defensive_rebounds = @defensive_rebounds
WHERE stat_id = @id;";

    await using var command = new SqlCommand(sql, connection);
    command.Parameters.AddWithValue("@id", id);
    AddStatParams(command, stat);

    return await command.ExecuteNonQueryAsync() > 0;
}

static async Task<bool> DeleteStatAsync(string connectionString, int id)
{
    await using var connection = await OpenConnectionAsync(connectionString);
    await using var command = new SqlCommand("DELETE FROM dbo.Stats WHERE stat_id = @id;", connection);
    command.Parameters.AddWithValue("@id", id);
    return await command.ExecuteNonQueryAsync() > 0;
}

static StatDto ReadStat(SqlDataReader r) => new(
    r.GetInt32(0), r.GetInt32(1), r.GetInt32(2),
    r.GetInt32(3), r.GetInt32(4), r.GetInt32(5), r.GetInt32(6),
    r.GetInt32(7), r.GetInt32(8), r.GetInt32(9), r.GetInt32(10),
    r.GetInt32(11), r.GetInt32(12), r.GetInt32(13), r.GetDateTime(14));

static void AddStatParams(SqlCommand cmd, StatWriteDto s)
{
    cmd.Parameters.AddWithValue("@game_id", s.GameId);
    cmd.Parameters.AddWithValue("@player_id", s.PlayerId);
    cmd.Parameters.AddWithValue("@two_pt_miss", s.TwoPtMiss);
    cmd.Parameters.AddWithValue("@two_pt_made", s.TwoPtMade);
    cmd.Parameters.AddWithValue("@three_pt_miss", s.ThreePtMiss);
    cmd.Parameters.AddWithValue("@three_pt_made", s.ThreePtMade);
    cmd.Parameters.AddWithValue("@steals", s.Steals);
    cmd.Parameters.AddWithValue("@turnovers", s.Turnovers);
    cmd.Parameters.AddWithValue("@assists", s.Assists);
    cmd.Parameters.AddWithValue("@blocks", s.Blocks);
    cmd.Parameters.AddWithValue("@fouls", s.Fouls);
    cmd.Parameters.AddWithValue("@offensive_rebounds", s.OffensiveRebounds);
    cmd.Parameters.AddWithValue("@defensive_rebounds", s.DefensiveRebounds);
}

static string? ValidateStat(StatWriteDto stat)
{
    if (stat.GameId <= 0) return "Game Id must be greater than zero.";
    if (stat.PlayerId <= 0) return "Player Id must be greater than zero.";
    return null;
}

public record StatDto(int StatId, int GameId, int PlayerId,
    int TwoPtMiss, int TwoPtMade, int ThreePtMiss, int ThreePtMade,
    int Steals, int Turnovers, int Assists, int Blocks, int Fouls,
    int OffensiveRebounds, int DefensiveRebounds, DateTime CreatedAt);

public record StatWriteDto(int GameId, int PlayerId,
    int TwoPtMiss, int TwoPtMade, int ThreePtMiss, int ThreePtMade,
    int Steals, int Turnovers, int Assists, int Blocks, int Fouls,
    int OffensiveRebounds, int DefensiveRebounds);


public record ConnectionCandidate(string Name, string ConnectionString);
public record ConnectionAttempt(string Name, bool Success, string? Error);
public record DbOperationResult<T>(T? Data, string? SucceededConnection, List<ConnectionAttempt> Attempts);

public record SchemaCountsDto(int TeamsCount, int PlayersCount, int GamesCount, int SchedulesCount);

public record TeamDto(int TeamId, string Name, string City, string Coach, DateTime CreatedAt);
public record TeamWriteDto(string Name, string City, string Coach);

public record PlayerDto(int PlayerId, int TeamId, string FirstName, string LastName, int? JerseyNumber, string Position, DateTime CreatedAt, string TeamName);
public record PlayerWriteDto(int TeamId, string FirstName, string LastName, int? JerseyNumber, string Position);

public record GameDto(int GameId, int HomeTeamId, int AwayTeamId, DateTime GameDate, string Location, int? HomeScore, int? AwayScore, string Status, DateTime CreatedAt);
public record GameWriteDto(int HomeTeamId, int AwayTeamId, DateTime GameDate, string Location, int? HomeScore, int? AwayScore, string Status);

public record ScheduleDto(int ScheduleId, int TeamId, int GameId, bool IsHome, DateTime CreatedAt);
public record ScheduleWriteDto(int TeamId, int GameId, bool IsHome);
