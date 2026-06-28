# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

Always build the full solution (not individual projects) to get proper platform resolution — the WinUI 3 app cannot build as AnyCPU:

```powershell
dotnet build "Loot V2.sln"
```

Run tests (must specify a platform):

```powershell
dotnet test "Loot V2.sln" /p:Platform=x64
```

Run a single test class:

```powershell
dotnet test "Loot V2.Tests.MSTest/Loot V2.Tests.MSTest.csproj" /p:Platform=x64 --filter "ClassName=Loot_V2.Tests.MSTest.TestClass"
```

## Runtime Identifiers

The project uses `win-x86`, `win-x64`, `win-arm64` (not the old `win10-*` prefix). Publish profiles live in `Loot V2/Properties/PublishProfiles/win-{arch}.pubxml`.

## Project Structure

Three-project solution:

- **`Loot V2.Core`** — Platform-agnostic class library. Contains domain models (`PlaidTransaction`, `SampleOrder`) and service interfaces/implementations that don't touch WinUI (`IFileService`, `ISampleDataService`).
- **`Loot V2`** — WinUI 3 app (net8.0-windows). Contains all UI, ViewModels, and services that depend on WinUI or external APIs.
- **`Loot V2.Tests.MSTest`** — MSTest project. UI-touching tests must use `[UITestMethod]` instead of `[TestMethod]`.

## Architecture

**DI/Hosting:** The app uses `Microsoft.Extensions.Hosting` with a full `IHost`. All registrations are in `App.xaml.cs`. `App.GetService<T>()` is the static accessor used in page code-behinds.

**MVVM:** CommunityToolkit.Mvvm. ViewModels extend `ObservableRecipient`. `[ObservableProperty]` and `[RelayCommand]` source generators are used throughout. Pages get their ViewModel via `App.GetService<TViewModel>()` in the constructor.

**Navigation:** Frame-based via `INavigationService`. Pages register against ViewModels in `PageService`. Shell uses a `NavigationView`. ViewModels that need navigation lifecycle implement `INavigationAware` (`OnNavigatedTo` / `OnNavigatedFrom`).

**Settings persistence:** `ILocalSettingsService` abstracts over `ApplicationData.Current.LocalSettings` (when MSIX-packaged) or a JSON file at `%LocalAppData%\Loot_V2\ApplicationData\LocalSettings.json` (unpackaged). Use `ReadSettingAsync<T>` / `SaveSettingAsync<T>`.

## Plaid Integration

`IPlaidService` / `PlaidService` wrap the `Going.Plaid` SDK (v6.61.1).

- **Credentials** are in `appsettings.json` under the `"Plaid"` key and bound via `services.AddPlaid(context.Configuration.GetSection("Plaid"))`.
- **`access_token`** is persisted to `LocalSettings` under key `"PlaidAccessToken"`. Call `InitializeAsync()` before checking `IsConnected`.
- **Link flow** runs in `PlaidLinkWindow` — a `WindowEx` with a `WebView2`. The page is served via `SetVirtualHostNameToFolderMapping` (hostname `plaid-link.local`) so it has a valid HTTPS origin (required by Plaid Link's iframe). The public token is returned via `window.chrome.webview.postMessage` and read with `args.TryGetWebMessageAsString()` (not `WebMessageAsJson`).
- Transaction data is **not persisted** — it is fetched from Plaid on each navigation to `ImportsPage` and held in `ObservableCollection<PlaidTransaction>` on `ImportsViewModel`.

## Key Patterns

- `PlaidLinkWindow` exposes `Task<string?> PublicTokenTask` backed by a `TaskCompletionSource`. The caller `await`s it after `Activate()`.
- `Closed` on `PlaidLinkWindow` always calls `_tcs.TrySetResult(null)` as a safety net so the caller never hangs.
- The `Going.Plaid` `Transaction` type: `Amount` is positive for debits (money out), negative for credits (money in). `Name` and `Category` are obsolete — use `MerchantName` and `PersonalFinanceCategory?.Primary` instead.
