# HomeschoolManager

A Blazor Server application built with Clean Architecture principles for managing homeschool activities and curriculum.

## Project Structure

```
HomeschoolManager/
├── src/
│   ├── HomeschoolManager.Web/          # Blazor Server App (Presentation Layer)
│   ├── HomeschoolManager.Core/         # Domain Models & Interfaces (Domain Layer)
│   ├── HomeschoolManager.Infrastructure/ # Data Access & External Services (Infrastructure Layer)
│   ├── HomeschoolManager.Application/  # Business Logic & Services (Application Layer)
│   └── HomeschoolManager.Shared/      # Shared DTOs & Utilities
└── tests/
    ├── HomeschoolManager.Tests.Unit/   # Unit Tests
    └── HomeschoolManager.Tests.Integration/ # Integration Tests
```

## Architecture Overview

This solution follows Clean Architecture principles:

- **Core**: Contains domain entities, enums, and interfaces
- **Application**: Contains business logic, services, and command/query handlers
- **Infrastructure**: Contains data access, repositories, and external service implementations
- **Web**: Blazor Server application with UI components
- **Shared**: Contains DTOs, view models, and shared utilities

## Getting Started

### Prerequisites

- .NET 10.0 SDK
- Visual Studio 2022 or VS Code

### Running the Application

1. Open the solution in Visual Studio or VS Code
2. Set `HomeschoolManager.Web` as the startup project
3. Build the solution
4. Run the application
5. Open `http://localhost:5129` if the browser does not open automatically

### Building from Command Line

```bash
cd HomeschoolManager
dotnet build
dotnet run --project src/HomeschoolManager.Web
```

## Development Guidelines

### Adding New Features

1. **Domain Models**: Add entities to `HomeschoolManager.Core`
2. **Business Logic**: Add services to `HomeschoolManager.Application`
3. **Data Access**: Add repositories to `HomeschoolManager.Infrastructure`
4. **UI Components**: Add pages and components to `HomeschoolManager.Web`

### Testing

- Unit tests go in `HomeschoolManager.Tests.Unit`
- Integration tests go in `HomeschoolManager.Tests.Integration`
- Run tests with: `dotnet test`

## Dependencies

- .NET 10.0
- Blazor Server
- xUnit (for testing)

## Local Data

The application uses a JSON-backed repository by default. Downloaded or published builds store records on the user's device at:

```text
%LOCALAPPDATA%\HomeschoolManager\homeschool-data.json
```

Runs with `ASPNETCORE_ENVIRONMENT=Development` use `App_Data/homeschool-data.json` because `appsettings.Development.json` sets `DataFilePath`. The included launch profile sets this environment for local development. The `App_Data/` folder is ignored by git so local homeschool records are not committed.

To store the data file somewhere else, set `DataStorage:FilePath` in `appsettings.json`, an environment variable, or a command-line argument:

```bash
dotnet run --project src/HomeschoolManager.Web --DataStorage:FilePath="D:\Homeschool\homeschool-data.json"
```

The Storage page in the app shows the active data file path and supports exporting or importing JSON backups.

## Publishing a Local App

Create a local Windows build with:

```bash
dotnet publish src/HomeschoolManager.Web/HomeschoolManager.Web.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish/homeschool-manager
```

Run `publish/homeschool-manager/HomeschoolManager.Web.exe`, then open `http://localhost:5129`. Each user gets their own local data file by default.

## Contributing

1. Follow Clean Architecture principles
2. Write unit tests for new features
3. Use meaningful commit messages
4. Update documentation as needed
