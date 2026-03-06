# De-Risking Milestone 1 (Minimal Path)

Goal: prove one end-to-end path works.

`MAUI app` -> `Local API` -> `SQL database` -> `show one record on screen`

## Scope

1. Create DB and share access.
2. API reads one record from DB.
3. MAUI is a simple Hello World page.
4. MAUI calls API.
5. Milestone 1 success: one DB record is displayed in MAUI.

## Prerequisites

- .NET 10 SDK
- MAUI workload
- Azure CLI
- Access to SQL server/database

Verify:

```bash
dotnet --version
dotnet workload list
az --version
```

## Step 1: Create Table and Seed One Record

Run this SQL once in your target database:

```sql
IF OBJECT_ID('dbo.Players', 'U') IS NULL
BEGIN
	CREATE TABLE dbo.Players
	(
		Id INT IDENTITY(1,1) PRIMARY KEY,
		PlayerName NVARCHAR(100) NOT NULL,
		Team NVARCHAR(100) NOT NULL,
		Points INT NOT NULL
	);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Players)
BEGIN
	INSERT INTO dbo.Players (PlayerName, Team, Points)
	VALUES ('Milestone Player', 'DeRisk Team', 42);
END;
```

## Step 2: Share DB Access (No Password in Code)

This project uses Entra auth in the connection string (`Authentication=Active Directory Default`).

1. Ensure your user has access to the SQL DB.
2. Run `az login` locally.
3. Keep connection string in `GobTrackerApi/appsettings.Development.json` under:

```json
"ConnectionStrings": {
  "SqlDatabase": "Server=tcp:<server>.database.windows.net,1433;Initial Catalog=<database>;Encrypt=True;TrustServerCertificate=False;Authentication=Active Directory Default;"
}
```

Optional fallback keys for teammates:

```json
"ConnectionStrings": {
	"SqlDatabase": "...Active Directory Default...",
	"SqlDatabaseInteractive": "...Active Directory Interactive...",
	"SqlDatabaseSqlAuth": "Server=tcp:<server>.database.windows.net,1433;Initial Catalog=<database>;Encrypt=True;TrustServerCertificate=False;User ID=<sql_user>;Password=<sql_password>;"
}
```

The API now tries these keys in order and uses the first one that works.

## Step 3: Run the App

macOS:

```bash
./run-dev.sh
```

Windows:

```powershell
.\run-dev.cmd
```

What the script does:

1. Starts API at `http://127.0.0.1:5117`
2. Waits for `/api/health`
3. Launches MAUI

## Step 4: Validate Milestone 1

In MAUI, click `Load Milestone Record`.

Expected result:

- Status: `Success: MAUI -> API -> SQL is working.`
- Player/Team/Points labels show data from `dbo.Players`

## Quick Troubleshooting

- API returns DB error: verify `az login` and DB permissions.
- Check DB auth details quickly: open `http://127.0.0.1:5117/api/db-health`.
- `No records found`: insert one row into `dbo.Players`.
- API not healthy on port `5117`: ensure port is free.
