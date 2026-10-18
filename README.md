# Employee Management

Employee management with a Windows Forms desktop application and a Blazor Interactive Server web application. Both use the same SQL Server LocalDB database and data service.

## Requirements

- Windows, .NET 10 SDK, and SQL Server Express LocalDB (MSSQLLocalDB).
- SQL Server Management Studio or the sqlcmd command-line tool for database setup.

Run the following commands from the solution folder.

## Database setup

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -I -b -i sql/CreateDatabase.sql
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -I -b -i sql/SeedData.sql
```

The scripts create the EmployeeManagement database and sample data. The -I option enables QUOTED_IDENTIFIER for the filtered email index. Alternatively, execute both scripts in SQL Server Management Studio with QUOTED_IDENTIFIER enabled.

Both applications configure their database connection in their own appsettings.json. Keep the connection strings pointed at the same database. The web application also supports the ConnectionStrings__EmployeeManagement environment variable.

## Run

```powershell
dotnet run --project src/EmployeeManagement.Desktop
```

```powershell
dotnet run --project src/EmployeeManagement.Web
```

Open http://localhost:5180 for the web application. The included launch profile uses the Development environment. The web app is a local demonstration without authentication.

## Editing employees

Edit fields directly in the grid or table. Select a department and use the date picker for the hire date. New, edited, and deleted employees remain pending until Save All. Undo cancels a pending deletion; Discard restores the last loaded or saved values. Refresh loads the current database values and asks before discarding pending changes.

Each application has its own editing session. After saving in one application, refresh the other to see the changes. Web users receive separate editors and database contexts for each employee page.

## Projects

- EmployeeManagement.Data: entities, database access, validation, and batch saving.
- EmployeeManagement.Shared: EmployeeEditor and EmployeeRow, reused by both interfaces.
- EmployeeManagement.Desktop: Windows Forms interface.
- EmployeeManagement.Web: Blazor interface using server-side interactivity.

NLog writes to the logs folder under each application's output directory. Desktop and Web use separate log files.

## Build and test

```powershell
dotnet build EmployeeManagement.slnx
dotnet test EmployeeManagement.slnx
```

Database tests create and remove their own LocalDB databases. LocalDB must be available to run them.
