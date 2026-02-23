# ApplyMate

ApplyMate is a packaged WinUI 3 desktop app for tracking job applications, interview schedules, follow-ups, and CV attachments.

## Features

- Dashboard with status counters, upcoming interviews, and recent applications.
- Add, browse, search, filter, and update applications with status history tracking.
- Details editing with interview date/time and CV attach/open/remove workflows.
- Local SQLite persistence through EF Core.
- Automatic no-response status updates based on configurable thresholds.
- JSON-backed settings persistence in app local storage.
- Windows app notifications with activation routing into app screens.
- Startup task toggle support for launching on sign-in.

## Solution Layout

- `src/ApplyMate.sln`
- `src/ApplyMate.App` (WinUI 3 packaged app)
- `src/ApplyMate.Core` (domain model and business rules)
- `src/ApplyMate.Infrastructure` (EF Core, SQLite, storage services)
- `tests/ApplyMate.Tests` (xUnit tests)

## Requirements

- Windows 11
- .NET SDK 8.x
- Windows App SDK tooling (restored via NuGet)

## Build

```powershell
dotnet build src/ApplyMate.sln -c Debug
```

## Test

```powershell
dotnet test tests/ApplyMate.Tests/ApplyMate.Tests.csproj -c Debug
```

## Run

Use Visual Studio (or another MSIX-capable workflow) to run the packaged `src/ApplyMate.App` project so local app storage, notifications, and startup task integration are available.
