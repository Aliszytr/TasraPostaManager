---
description: Safely start and stop the dotnet development server
---

# Dotnet Development Server Workflow

## Starting the Server

// turbo-all

1. Kill any existing dotnet processes to free up ports:
```powershell
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue; Start-Sleep -Seconds 1
```

2. Start the development server:
```powershell
dotnet run --no-build
```
Note: Use `--no-build` if you've already built. Otherwise use `dotnet run`.
This is a background command — use WaitMsBeforeAsync of 5000.

## Stopping the Server

1. Force-kill all dotnet processes (most reliable method):
```powershell
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
```

## Building and Running (Combined)

1. Kill existing processes and build:
```powershell
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue; Start-Sleep -Seconds 1; dotnet build --nologo --verbosity quiet
```

2. If build succeeds, start the server:
```powershell
dotnet run --no-build
```

## Important Notes

- **Always kill before starting** — prevents port conflicts (5000/5001)
- **Don't use `send_command_input` with Terminate** — it's unreliable for dotnet processes
- **Use `Get-Process | Stop-Process -Force`** — this is the reliable way to stop
- The app listens on `http://localhost:5000` and `https://localhost:5001`
- Health check: `http://localhost:5000/health`
