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
	var result = await TryLoadSinglePlayerWithFallbackAsync(config);

	if (result.Attempts.Count == 0)
	{
		return Results.Problem(
			title: "Database configuration missing",
			detail: "Configure at least one connection string: SqlDatabaseSqlAuth or SqlDatabase.",
			statusCode: StatusCodes.Status500InternalServerError);
	}

	if (result.SucceededConnection is not null)
	{
		return Results.Ok(new
		{
			status = "ok",
			connection = result.SucceededConnection,
			hasRecord = result.Player is not null
		});
	}

	return Results.Problem(
		title: "Database connection failed",
		detail: BuildAttemptsDetail(result.Attempts),
		statusCode: StatusCodes.Status503ServiceUnavailable);
});

app.MapGet("/api/milestone-record", async (IConfiguration config) =>
{
	var result = await TryLoadSinglePlayerWithFallbackAsync(config);

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

	if (result.Player is null)
	{
		return Results.NotFound(new
		{
			message = "No records found in dbo.Players.",
			connection = result.SucceededConnection
		});
	}

	return Results.Ok(result.Player);
});

app.Run();

static async Task<DbLoadResult> TryLoadSinglePlayerWithFallbackAsync(IConfiguration config)
{
	var attempts = new List<ConnectionAttempt>();

	foreach (var candidate in GetConnectionCandidates(config))
	{
		try
		{
			var player = await LoadSinglePlayerAsync(candidate.ConnectionString);
			attempts.Add(new ConnectionAttempt(candidate.Name, true, null));
			return new DbLoadResult(player, candidate.Name, attempts);
		}
		catch (Exception ex)
		{
			attempts.Add(new ConnectionAttempt(candidate.Name, false, ex.Message));
		}
	}

	return new DbLoadResult(null, null, attempts);
}

static IEnumerable<ConnectionCandidate> GetConnectionCandidates(IConfiguration config)
{
	var keys = new[]
	{
		"SqlDatabaseSqlAuth",
		"SqlDatabase"
	};

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

static async Task<PlayerDto?> LoadSinglePlayerAsync(string connectionString)
{
	await using var connection = new SqlConnection(connectionString);
	await connection.OpenAsync();

	const string sql = @"
SELECT TOP (1)
	Id,
	PlayerName,
	Team,
	Points
FROM dbo.Players
ORDER BY Id;";

	await using var command = new SqlCommand(sql, connection);
	await using var reader = await command.ExecuteReaderAsync();

	if (await reader.ReadAsync())
	{
		return new PlayerDto(
			reader.GetInt32(0),
			reader.GetString(1),
			reader.GetString(2),
			reader.GetInt32(3));
	}

	return null;
}

public record ConnectionCandidate(string Name, string ConnectionString);
public record ConnectionAttempt(string Name, bool Success, string? Error);
public record DbLoadResult(PlayerDto? Player, string? SucceededConnection, List<ConnectionAttempt> Attempts);
public record PlayerDto(int Id, string PlayerName, string Team, int Points);
