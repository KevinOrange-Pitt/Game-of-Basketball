# Game-of-Basketball
macOS: ./run-dev.sh
- Starts local API, waits for health, then launches MAUI app.

Windows: .\run-dev.cmd
- Launches the PowerShell runner that starts API + MAUI together.

Windows helper: run-dev.ps1
- Script used by .\run-dev.cmd; handles API startup/health check and cleanup on exit.

Checklist (before running)
- .NET 10 SDK installed.
- MAUI workload installed (`dotnet workload list` should include maui).
- Platform tooling installed (Windows SDK on Windows, Xcode tooling on macOS).
- Entra account has access to `softengineering.database.windows.net` / `SoftwareEngSpring`.
- Port `5117` is free on your machine.


-----------Run this for dependencies--------
Windows setup (run in PowerShell as Admin)
```powershell
winget install --id Microsoft.DotNet.SDK.10 --exact --accept-source-agreements --accept-package-agreements
winget install --id Microsoft.AzureCLI --exact --accept-source-agreements --accept-package-agreements
winget install --id Microsoft.VisualStudio.2022.Community --exact --override "--add Microsoft.VisualStudio.Workload.NetCrossPlat --includeRecommended --passive --norestart" --accept-source-agreements --accept-package-agreements
dotnet workload install maui
az login

dotnet --version
dotnet workload list
```
