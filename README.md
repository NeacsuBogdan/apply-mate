# ApplyMate

ApplyMate is a WinUI 3 desktop app for tracking job applications, interviews, and follow-ups.

## Solution Layout

- `src/ApplyMate.sln`
- `src/ApplyMate.App` (WinUI 3 packaged app)
- `src/ApplyMate.Core` (domain and business logic)
- `src/ApplyMate.Infrastructure` (persistence and integrations)
- `tests/ApplyMate.Tests` (xUnit tests)

## Build

```powershell
dotnet build src/ApplyMate.sln -c Debug
```

## Test

```powershell
dotnet test tests/ApplyMate.Tests/ApplyMate.Tests.csproj -c Debug
```
