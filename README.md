# Run Instructions

## Prerequisites

- .NET 10 SDK
- .NET MAUI workload

Optional checks:

```bash
dotnet --version
dotnet workload list
```

## Run on macOS

From the repo root:

```bash
./run-dev.sh
```

Default target on macOS is `net10.0-maccatalyst`.

To specify a framework manually:

```bash
./run-dev.sh net10.0-maccatalyst
```

## Run on Windows

From the repo root:

```powershell
.\run-dev.cmd
```

This starts the API and launches MAUI using `net10.0-windows10.0.19041.0`.

## What the run scripts do

1. Start the API on `http://127.0.0.1:5117`
2. Wait for API health at `http://127.0.0.1:5117/api/health`
3. Launch the MAUI app

## Stop the app

- Press `Ctrl+C` in the terminal running the script.

## Quick troubleshooting

- API failed health check: ensure port `5117` is free, then run again.
- MAUI launch failed on macOS: try `./run-dev.sh net10.0-maccatalyst`.
- Missing commands: install/update `.NET SDK` and MAUI workload.

## Running unit tests

- Run this command - dotnet test --verbosity normal