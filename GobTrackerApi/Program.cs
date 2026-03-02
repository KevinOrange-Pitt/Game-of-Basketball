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

app.MapGet("/api/players", async (IConfiguration config) =>
{
	var defaultConnectionString = config.GetConnectionString("SqlDatabaseDefault");
	var interactiveConnectionString = config.GetConnectionString("SqlDatabaseInteractive");

	if (string.IsNullOrWhiteSpace(defaultConnectionString) || string.IsNullOrWhiteSpace(interactiveConnectionString))
	{
		return Results.Problem("Connection strings 'SqlDatabaseDefault' and 'SqlDatabaseInteractive' must be configured.");
	}

	try
	{
		var players = await LoadPlayersAsync(defaultConnectionString);
		return Results.Ok(players);
	}
	catch (Exception ex) when (ex.Message.Contains("DefaultAzureCredential failed", StringComparison.OrdinalIgnoreCase))
	{
		var players = await LoadPlayersAsync(interactiveConnectionString);
		return Results.Ok(players);
	}
	catch (Exception ex)
	{
		return Results.Problem($"Database query failed: {ex.Message}");
	}
});

app.Run();

static async Task<List<PlayerDto>> LoadPlayersAsync(string connectionString)
{
	var players = new List<PlayerDto>();

	await using var connection = new SqlConnection(connectionString);
	await connection.OpenAsync();

	const string sql = @"
SELECT TOP (100)
	Id,
	PlayerName,
	Team,
	Points
FROM dbo.Players
ORDER BY Id;";

	await using var command = new SqlCommand(sql, connection);
	await using var reader = await command.ExecuteReaderAsync();

	while (await reader.ReadAsync())
	{
		players.Add(new PlayerDto(
			reader.GetInt32(0),
			reader.GetString(1),
			reader.GetString(2),
			reader.GetInt32(3)));
	}

	return players;
}

public record PlayerDto(int Id, string PlayerName, string Team, int Points);
