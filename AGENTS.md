# AGENTS.md

This file provides guidance to Codex (Codex.ai/code) when working with code in this repository.

## What this is

EHMR — a single-project .NET MAUI **Windows desktop** app (net9.0-windows10.0.19041.0, unpackaged / `WindowsPackageType=None`) for a Macedonian rheumatology clinic: patients, encounters (прегледи), appointments, therapy protocols/cycles, prescriptions, MKB-10 codes, reports, RBAC user admin, and database backup/restore. Data lives in local SQL Server Express via EF Core 9.

There is no test project and no README. `EHMR.sln` contains one project.

## Commands

```bash
dotnet build EHMR.sln
```

```bash
dotnet run --project EHMR.csproj -f net9.0-windows10.0.19041.0
```

EF Core migrations (design-time uses `DesktopTherapyDbContextFactory`, which reads `appsettings.json` from the *current directory* — run from the repo root):

```bash
dotnet ef migrations add <Name> --context DesktopTherapyDbContext
```

```bash
dotnet ef database update --context DesktopTherapyDbContext
```

Migrations also run automatically at app startup (`DatabaseMigrationService.MigrateAsync`, kicked off from the `App` constructor), which takes a `BACKUP DATABASE` snapshot into `%LOCALAPPDATA%\EHMR_Backups` before applying pending migrations, then runs all `IEntitySeeder`s via `SeederRunner`.

## Language convention

Code comments, UI strings, dialog text, and log messages are written in **Macedonian**. Type/member names are English. Match this when editing — don't translate existing Macedonian strings to English.

## Architecture

### Composition root
`MauiProgram.CreateMauiApp` → `builder.Services.RegisterEHMR()` in [EhmrCollectionResitration.cs](EhmrCollectionResitration.cs). That one file is the DI map for the whole app: `RegisterDb` (EF + seeders + report providers + backup pipeline), `RegisterApiServices` (all `I*Service` singletons), `RegisterViewModels`/`RegisterPages` (reflection-based: **every** type whose name ends in `ViewModel`/`VM`/`Page` is auto-registered as transient), and `RegisterModules` → `AddUiCore` (explicit page/VM registrations, including `View`-suffixed pages the reflection scan misses).

`MauiProgram.ServiceProvider` and `App.ServiceProvider` are static escape hatches used where constructor injection isn't available.

Startup order: `App.CreateWindow` shows `LoadingPage`, then on `Window.Created` resolves `AppShell`, calls `HandleInitialNavigationAsync`, and swaps `window.Page` to the shell. Failures land on `ErrorPage`.

### Navigation + RBAC (they are one system)
- Routes are string constants in [AppRoutes.cs](Domain/Entities/Rbac/AppRoutes.cs) and mapped to page types in `AppShell.RegisterRoutes()`. Adding a screen means: route constant → `Routing.RegisterRoute` → DI registration → an entry in `AppNavigation.AllGroups`.
- [AppNavigation.cs](Domain/Entities/Rbac/AppNavigation.cs) is the single source for the flyout menu **and** for route→module mapping. `AuthorizationService.ResolveModule` looks a route up in `AppNavigation` to find its `Modules.*` name; a route absent from `AppNavigation` is treated as unrestricted.
- `INavigationCoordinator` is the central pipe: `AppShell.SetupCoordinator` registers the single handler that checks `IAuthorizationService.CanAccessRoute` before `GoToAsync`, guards reentrancy with `_isNavigating`, and closes the flyout.
- Permissions resolve as: `Modules` (module names + role defaults) × `RolePermissionMatrix` (module → `ModuleAction` flags per role) × `User.Modules` explicit overrides, with Admin/SuperAdmin bypass. `RoleHierarchy` governs who may manage/assign which roles.
- Row-level scoping: non-admin users only see their own doctor's data. This is applied in `BaseViewModel.ApplyDoctorScope` via each VM's `DoctorOwnerSelector` — a doctor-role user with no linked `Doctor` record sees **nothing**, not everything.
- Cross-page state is passed through `ISelectedItemService<T>` (`SelectedItem` + `OpenInEditMode`), not through route query parameters.

### ViewModels
`BaseViewModel<T>` ([ViewModels/BaseViewModel.cs](ViewModels/BaseViewModel.cs)) is CommunityToolkit.Mvvm-based and carries the list-screen contract most VMs inherit: a fixed pipeline `ApplyDoctorScope → ApplySearch → ApplyFilters → ApplySort` over an in-memory `AllItems`, client-side pagination, `IsBusy`/error state, `ExecuteSafeAsync`, permission flags (`CanCreate/CanUpdate/CanDelete` from `EvaluatePermissions()`), and the Spark UI collections (`Tabs`, `Pickers`, `Buttons`). Derived VMs must supply `ModuleName`, `DetailRoute`, `ApplyFilters`, and `ResetFilters`. Note it filters **in memory** — data is loaded fully, then paged.

### Persistence
- `TherapyTrackerDbContext` (base) → `DesktopTherapyDbContext` (concrete). `OnModelCreating` runs conventions over the whole model: decimal precision (18,4), automatic global query filters for every `ISoftDelete` entity, and reflection-loaded `IMappingConfiguration` classes from `Infrastructure/Persistence/Configs`.
- Registered as **`AddDbContextFactory`**, not `AddDbContext` — resolve `IDbContextFactory<DesktopTherapyDbContext>` and create short-lived contexts. `DesktopTherapyDbContext.CreateManual(...)` exists for non-DI paths.
- `Patient.NationalId` is stored encrypted through an EF `ValueConverter` backed by `IEncryptionService`. Consequence: **it cannot be queried, filtered, or sorted in SQL** — anything touching it must materialize first. Changing `Encryption:Key` in `appsettings.json` makes existing rows undecryptable; startup hard-fails if the key is missing.
- `DesktopTherapyDbContext.SeedIds` holds fixed GUIDs shared by all seeders — reuse them rather than inventing new ones.
- `IDbExceptionParserProvider` converts `DbUpdateException` into user-facing messages inside `SaveChangesAsync`.

**Connection string gotcha:** the runtime connection string is *hardcoded* in `RegisterDb()` in [EhmrCollectionResitration.cs](EhmrCollectionResitration.cs) and again as `DesktopTherapyDbContext.DefaultConnection`; `appsettings.json`'s `ConnectionStrings:Default` is only read by the design-time factory. Changing the database means editing all three.

### Spark: reflection-driven forms and grids
`Resources/Controls/SparkForm/` builds entire forms from a POCO at runtime. `SparkReflectionBuilder` reads `Spark*Attribute`s off properties (`SparkField`, `SparkDisplay`, `SparkSection`, `SparkTab`, `SparkLookup`, `SparkRequired`, `SparkRange`, `SparkRegex`, `SparkOrder`, `SparkReadOnly`, `SparkVisible`, `SparkIgnore`) to produce a `SparkFormDefinition`; `SparkTemplateRegistry` maps each `SparkFieldType` to an `ISparkFieldTemplate` that emits the actual `View`. Templates are registered once at startup by `SparkTemplateInitializer.Register()` (called from `MauiProgram`) — a new field type needs a template registered there or the form silently degrades. `SparkGridRow`/`SparkGridColumn`/`SparkButtonItem`/`SparkTabItem`/`SparkPickerItem` (in `Resources/Controls/SparkModels.cs`) are the dynamic-grid counterparts that `BaseViewModel` populates.

### Reports
Each report is an `IReportProvider` (key, columns, tabs/pickers/buttons, `GenerateAsync(from, to)`, `CalculateMetrics`) registered as a singleton in `RegisterDb`. `ReportRegistry` collects them all and throws on duplicate keys. Adding a report = new provider class + one `AddSingleton<IReportProvider, …>` line; no UI changes needed. Export goes through `IReportExportService` (QuestPDF / ClosedXML).

### Backups
`Backups/` is a self-contained subsystem (its own `Views`, `ViewModels`, `Services`, `Providers`, `Interfaces`). `BackupEngine.ExecuteAsync` runs: `IDatabaseBackupProvider` (SQL Server `BACKUP DATABASE` into `Backup:StagingDirectory`) → optional compress → optional encrypt (`IBackupSecurityProvider`) → `IBackupStorageProvider` (local / Azure Blob / network share, chosen via `IStorageProviderResolver` against `Backup:Destinations` keys `local`/`cloud`/`network`) → optional `IBackupVerifier` → history row via `IBackupHistoryRepository`. `RestoreEngine` is the inverse. Note the folder `EHMR.Backups\**` is excluded from compilation in the csproj — the live code is in `Backups/`.

### UI conventions
Custom controls are the `FF*` family in `Resources/Controls/` (`FFDataGrid`, `FFInput`, `FFPicker`, `FFCard`, `FFMetricTile`, `FFStatusChip`, `FFPagination`, `FFSearchBox`, …), each with a paired `FF*Styles.xaml` merged into `App.xaml`. Prefer composing these over raw MAUI controls. `GlobalXmlns.cs` maps the `EHMR` namespace into the default MAUI XAML namespace, so custom controls need no `xmlns:` prefix in XAML.

Windows-specific handler tweaks (e.g. forcing `Picker`/`ComboBox` colors) live behind `#if WINDOWS` in `MauiProgram`.

### Code style you'll see
Existing code omits spaces around `=` and `&&`/`||` (`_auth=auth;`, `IsAuthenticated&&!HasRole(...)`). Large commented-out registration blocks in `EhmrCollectionResitration.cs` mark planned-but-unwired modules — leave them unless asked.
