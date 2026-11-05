# AGENTS.md — AI Agent and Developer Technical Reference for SDAnext

This document is the authoritative technical reference, architectural guide, and rule set for AI agents and human developers working on the **SDAnext** codebase.

---

## 1. Project Overview & Tech Stack

**SDAnext** is a modern, high-performance, cross-platform Steam Desktop Authenticator (based on and inspired by [NebulaAuth](https://github.com/achiez/NebulaAuth)) built on **.NET 8** and **Avalonia UI 12.1**.

### Core Capabilities
- **Steam Guard TOTP Generation:** Real-time generation of 5-character Steam Guard codes (RFC 6238 with Steam's custom Base32 alphabet) with time alignment against Valve servers and an interactive hardware-accelerated progress bar.
- **Trade & Market Confirmations:** Live viewing, inspection, and one-click or bulk acceptance/rejection of trade offers and Community Market listings. Includes multi-language item/price scraping and separated or grouped market listing views.
- **7-Step Account Linker Wizard (`Linker`):** End-to-end onboarding workflow to bind Steam Guard to a new Steam account directly from the application (handling phone attachment, SMS validation, and recovery code issuance).
- **4-Step maFile Mover Wizard (`MafileMover`):** Safe migration workflow to transfer authenticator secrets from mobile devices or other SDA installations without trade cooldowns.
- **maFiles Management & Backup:** Flexible backup, export templates, bulk renaming, import, and archiving (`maFiles_backup/`, `maFiles_removed/`).
- **Cryptographic Protection:** Strong local encryption of `maFiles` using AES-256-CBC with user master password and secure key derivation.
- **Dynamic Internationalization:** Fully reactive runtime language switching (English and Russian) without restarting the application.
- **Memory Safety & Performance:** Zero-allocation Protobuf parsing via `ArrayPool<byte>.Shared`, Span-based cryptographic hashing with `stackalloc`, in-memory secret wiping via `CryptographicOperations.ZeroMemory`, Source-Generated Regex and JSON, and resilient HTTP client pipelines.

### Technology Stack
- **Platform:** .NET 8 (C# 12).
- **GUI Framework:** Avalonia UI 12.1.4 (XAML/AXAML, CompiledBindings).
- **MVVM & UI Components:** `CommunityToolkit.Mvvm` 8.4.2, `Material.Icons.Avalonia` 3.0.2, `DialogHost.Avalonia` 0.12.3.
- **Protobuf Stack:** `protobuf-net` 3.2.52 with custom high-performance pooled streaming.
- **HTML/JSON Scraping:** `HtmlAgilityPack` 1.12.1 and `System.Text.Json` Source Generators.
- **Target Operating Systems:** 
  - Linux (x64, glibc >= 2.16, native Wayland / X11).
  - Windows (x64).
  - macOS (x64 / arm64).
- **Publish Profile:** Single-file self-contained deployment with trimmer descriptors and optional Native AOT support.

---

## 2. Repository Structure

```text
.
├── src/
│   ├── SteamLibForked/          # [STEAM PROTOCOL CORE] Pure net8.0 library: network logic, crypto, Protobuf, Login V2
│   ├── NextAuth.Core/           # [BUSINESS LOGIC] Pure net8.0 library: models, storage, ViewModels, services, localization
│   └── NextAuth.Desktop/        # [AVALONIA GUI] net8.0 desktop app: Avalonia 12.1, AXAML views, custom controls
├── tests/
│   └── SteamLib.Tests/          # [TEST SUITES] xUnit unit and regression tests for SteamLibForked
├── NextAuth.slnx                # Modern solution definition (.slnx)
├── Directory.Build.props        # Common compiler configurations and warning suppressions
├── LICENSE                      # GNU Affero General Public License v3.0 (AGPL-3.0)
├── LICENSES.md                  # Comprehensive third-party license notices and attributions
├── README.md                    # User-facing GitHub documentation
└── AGENTS.md                    # Technical reference and AI agent invariants (this file)
```

---

## 3. Mandatory Rules & Invariants for AI Agents

All AI agents and contributors working in this repository must strictly observe the following rules:

### 1. `SteamLibForked` Purity & Isolation
- `SteamLibForked` is an independent, self-contained, rigorously tested implementation of the Steam protocol (Login V2, WebAPI, TOTP, Trade/Market Confirmations, Authenticator Linker).
- **Never introduce UI dependencies** (such as Avalonia, WinForms, or WPF) or heavy incompatible third-party libraries into this project.
- Keep serialization centered on `System.Text.Json` source generation and protobuf on `protobuf-net`.

### 2. `maFiles` Security and 100% Backward Compatibility
- The `maFiles/` directory contains critical private authentication keys (`shared_secret`, `identity_secret`, `revocation_code`).
- Any changes to `Storage.cs`, `PHandler.cs`, or `MafileSerializer.cs` **must maintain 100% backward and forward compatibility** with legacy SDA `.mafile` files (v1 format), v2, v3, and minified formats.
- Unknown properties must be preserved during deserialization and re-serialization via `UnusedProperties` so that user metadata or future Steam additions are not lost.

### 3. Cross-Platform Paths and Environment
- **Never hardcode Windows path separators (`\`)**. Always use `Path.Combine(...)` or `Path.DirectorySeparatorChar`.
- For resolving the application's working/base directory, always use `AppContext.BaseDirectory`. In single-file self-contained Linux binaries, `Assembly.Location` returns an empty string (`""`).
- All configuration files (`settings.json`, export templates) and localization resources must be read and written using `UTF-8 without BOM`.

### 4. Prohibition of Windows-Only APIs
- **Strictly forbidden:** `System.Windows.Forms`, `System.Windows.*`, `System.Drawing.Common`, and P/Invoke calls to `user32.dll` or `dwmapi.dll`.
- Operating system interactions (clipboard access, file picking, directory selection, dialogs, application lifecycle) must be performed exclusively through core service abstractions:
  - `IClipboardService`
  - `IFileDialogService`
  - `IDialogService`
  - `INotificationService`
  - `IAppService`

### 5. Documentation Synchronization
- Whenever code changes modify architecture, APIs, features, or project dependencies, immediately update the corresponding markdown documentation files (`AGENTS.md`, `README.md`, `LICENSES.md`).

---

## 4. Comprehensive Subsystem Reference

### 4.1. `SteamLibForked` (`src/SteamLibForked/`)
- **Target Framework:** `net8.0` (pure cross-platform C# 12).
- **Root Namespace:** `SteamLib` (all subcomponents reside under `SteamLib.*`).
- **Dependencies:** `protobuf-net`, `protobuf-net.Core`, `HtmlAgilityPack`, `System.IdentityModel.Tokens.Jwt`, `Microsoft.Extensions.Options`, `Microsoft.Extensions.DependencyInjection.Abstractions`.

#### Architecture & Submodules
- **`Abstractions/`**:
  - `ISteamAuthenticationService.cs`: Service contract for Steam Login V2, session polling, token caching, and JWT refresh operations.
  - `ISteamConfirmationsService.cs`: Service contract for fetching, parsing, and processing single or batch trade/market confirmations.
  - `ISteamLinkerService.cs`: Service contract for end-to-end 2FA binding, SMS validation, and authenticator removal.
  - `ISteamTimeAligner.cs`: Service contract for synchronizing local system clocks with Valve's time servers (`ITwoFactorService/QueryTime`).
  - `ISessionData.cs`: Base contract for an authenticated Steam session (`SessionId`, `SteamId`, `AccessToken`, `RefreshToken`).
  - `Auth/`: Authorization contracts (`IAuthProvider`, `IEmailAuthProvider`, `ILoginConsumer`, `ISteamGuardProvider`).
  - `Linker/`: Interaction contracts for phone and SMS code inputs during 2FA onboarding (`IPhoneNumberProvider`, `ISmsCodeProvider`).
- **`Services/`**:
  - `SteamAuthenticationService.cs`: Default implementation of Login V2 workflow with status polling and token refreshes.
  - `SteamConfirmationsService.cs`: Default implementation for retrieving and responding to mobile confirmations.
  - `SteamLinkerService.cs`: High-level wrapper for 2FA account binding.
  - `SteamLibServiceCollectionExtensions.cs`: Dependency injection extension method `services.AddSteamLib()` to register all SteamLib services in an `IServiceCollection`.
- **`Api/`**:
  - `Mobile/`: `SteamAuthenticatorLinkerApi` (low-level 2FA API calls), `SteamMobileConfirmationsApi` (confirmation fetch and submission), `SteamMobileApi` (OAuth/JWT token renewals).
  - `Services/`: `AuthenticationServiceApi` — Protobuf endpoints for Steam's `IAuthenticationService`.
  - `Trade/`: `SteamTradeApi` — HTTP endpoints for web trade confirmation.
  - `SteamGlobalApi.cs`: Scrapes session cookies and `g_sessionID` from Steam Community pages.
- **`Authentication/`**:
  - `AuthResult.cs`: Discriminated union for Login V2 outcomes (`Success`, `NeedDeviceCode`, `NeedEmailCode`, `InvalidCredentials`, `RateLimited`, `Failed`), enabling clean pattern-matching without throwing exceptions for normal login branches.
  - `LoginV2/`: Steam Login V2 engine (`LoginV2Executor.cs`, `FinalizeLoginJson.cs`, `TransferResult.cs`), supporting both typed asynchronous execution and the legacy `DoLogin` facade.
  - `AdmissionHelper.cs`, `AuthHelper.cs`, `SessionDataValidator.cs` (session structure validation), `StaticLoginConsumer.cs`, `SteamTokenHelper.cs` (JWT parsing).
- **`Core/`**:
  - `StatusCodes/`: Typed representations and handling of Steam error codes (`SteamStatusCode.cs`, `SteamStatusCode_Codes.cs`, `StatusCodeContext.cs`).
  - `SteamConstants.cs`: Base URLs for Valve APIs, Steam Community, and Steam Store.
  - `SteamDomains.cs`: Enumeration and mapping of Steam domains.
  - `SteamLibErrorMonitor.cs`: Interceptor and analyzer for recurring Steam HTTP and protocol errors.
- **`Exceptions/`**:
  - Typed exception hierarchy: `LoginException`, `SessionInvalidException`, `SessionPermanentlyExpiredException`, `UnsupportedAuthTypeException`, `AuthenticatorLinkerException`, `BadMobileCookiesException`, `CantLoadConfirmationsException`, `SteamStatusCodeException`.
- **`Factory/`**:
  - `DeviceDetailsDefaultBuilder.cs`: Constructs compliant device metadata headers for Steam Login V2.
- **`Models/`**:
  - `Session/`: Models for authenticated sessions (`SessionData.cs`, `MobileSessionData.cs`, `SteamAuthToken.cs`, `SteamAccessTokenType.cs`).
  - `SteamIds/`: Strongly typed, allocation-free Steam ID structs (`SteamId64.cs`, `SteamId2.cs`, `SteamId3.cs`, `SteamId.cs`) with conversion between all representations.
  - `Account/`: `WebTradeEligibility.cs` (trading restriction details).
  - `ScriptHeaderModel.cs`: Models session metadata embedded in Steam HTML headers.
- **`MobileData.cs`**:
  - Base and extended authenticator data structures (`MobileData`, `MobileDataExtended`) that map to the `.mafile` format.
- **`ProtoCore/`**:
  - Lightweight, embedded Protobuf communication stack without heavy runtime dependencies:
    - `PooledBufferStream.cs`: High-performance stream backed by `ArrayPool<byte>.Shared`, eliminating temporary buffer allocations during network serialization.
    - `ProtoHelpers.cs` & `ProtoHttpClientExtension.cs`: Zero-allocation protobuf payload serialization directly over `HttpClient`.
    - `Services/`: Protobuf message definitions for `AuthenticationService.cs` (`BeginAuthSession`, `PollAuthSessionStatus`, `UpdateAuthSessionWithMobileConfirmation`) using stack-allocated HMAC-SHA256 calculations.
    - `Enums/`: `EAuthSessionGuardType`, `EAuthTokenPlatformType`, `ETokenRenewalType`, `EResult`.
- **`Results/`**:
  - High-performance, allocation-free result types: `readonly struct SteamResult<T>` and `SteamResult` (`IsSuccess`, `Value`, `Error`, `StatusCode`, `Exception`, `Match`, `ValueOr`).
- **`SteamMobile/`**:
  - `SteamGuardCodeGenerator.cs`: 5-character TOTP generator implementing RFC 6238 with Steam's custom alphabet. Uses `HMACSHA1.HashData` over stack-allocated spans (`stackalloc byte[8]`), big-endian integer encoding via `BinaryPrimitives`, and cryptographic zeroing of key buffers.
  - `TimeAligner.cs` & `SteamTimeAligner.cs`: Synchronizes system clock with Steam servers via `ITwoFactorService/QueryTime`.
  - `AuthenticatorLinker/`: Step-by-step account binding coordinator (`SteamAuthenticatorLinker.cs`, `LinkOptions.cs`, `LinkResult.cs`, `CheckPhoneResult.cs`).
  - `Confirmations/`: Typed confirmation models:
    - Concrete classes: `Confirmation.cs`, `TradeConfirmation.cs`, `MarketConfirmation.cs`, `PurchaseConfirmation.cs`, `RegisterApiKeyConfirmation.cs`, `AccountRecoveryConfirmation.cs`.
    - Details classes: `ConfirmationDetails.cs`, `MarketConfirmationDetails.cs`, `TradeDetailsConfirmation.cs`.
- **`Utility/`**:
  - `MafileSerialization/`: Robust `.mafile` engine (`MafileSerializer.cs`, `DeserializedMafileData.cs`, `LegacyMafile.cs`, `MafileSerializerSettings.cs`). Handles schema variations, camelCase/PascalCase normalization, session token migration, and preserves unknown fields.
  - `EncryptionHelper.cs`: Cross-platform cryptographic operations (RSA password encryption via `RSA.Create()`, HMAC-SHA1 confirmations via `HMACSHA1.HashData`, in-memory secret clearing via `ZeroMemory`).
  - `EnvironmentUtility.cs`: OS detection for Steam mobile client emulation (`EOSType`).
  - `SteamIdParser.cs`: High-performance Steam ID regex parser using `[GeneratedRegex]`.
- **`Web/`**:
  - `ISteamHttpClientProvider.cs` & `SteamHttpClientFactory.cs`: Singleton provider of long-lived `HttpClient` instances backed by `SocketsHttpHandler` (15-minute pooled connection lifetime, automatic DNS rotation, compression, HTTP/2).
  - `SteamRetryHandler.cs` & `SteamRetryOptions.cs`: Delegating HTTP resilience handler featuring exponential backoff with full jitter, honoring `Retry-After` on HTTP 429, handling transient 502–504 gateway failures and Cloudflare 520–524 errors.
  - `SteamSessionContext.cs`: Encapsulates an account's cookie jar, tokens, and active session client with lazy instantiation and automatic secret masking in `ToString()`.
  - `SteamJsonSerializerContext.cs`: Source-generated `System.Text.Json` context for compile-time reflection-free JSON operations.
  - `Scrappers/`: `ScriptHeaderScrapper.cs` (extracts sessionID/steamID from HTML via generated regex) and `MobileConfirmationScrapper.cs` (parses mobile confirmation JSON and HTML payloads with multi-language item and price extraction).

---

### 4.2. `NextAuth.Core` (`src/NextAuth.Core/`)
- **Target Framework:** `net8.0` (pure cross-platform business logic, zero UI references).
- **Root Namespace:** `NextAuth.Core` (subnamespaces: `NextAuth.Core.Model.*`, `NextAuth.Core.ViewModel.*`, `NextAuth.Core.Services`, `NextAuth.Core.Utility`).
- **Dependencies:** `SteamLibForked`, `CommunityToolkit.Mvvm`.

#### Architecture & Submodules
- **`Abstractions/`**:
  - `IClipboardService`: Asynchronous clipboard operations (`GetTextAsync`, `SetTextAsync`, `GetFilesAsync`).
  - `IFileDialogService`: File picking, directory browsing, and file saving dialog abstractions (`PickMaFilesAsync`, `PickFolderAsync`, `SaveFileAsync`).
  - `INotificationService`: User-facing notification dispatch (`ShowSuccess`, `ShowError`, `ShowWarning`, `ShowInfo`).
  - `IDialogService`: Modal dialog orchestration driven by ViewModels (`ShowDialogAsync<TViewModel>`).
  - `IAppService`: Window and lifecycle management (`Minimize`, `Close`, `IsMainWindowMinimized`).
- **`Model/`**:
  - `Storage.cs`: Central filesystem manager for `maFiles/`, `maFiles_removed/`, and `maFiles_backup/`. Handles loading, saving, encryption checks, and bulk operations.
  - `PHandler.cs`: Master password management, PBKDF2/AES key derivation, file encryption/decryption, and security state tracking.
  - `Settings.cs`: Reactive application settings persisted to `settings.json` (includes confirmation grouping options, language preferences, auto-refresh intervals).
  - `MafileExport/`: Configurable export templates and token replacement engine.
  - `Shell.cs`: Core startup coordinator and shared timer services.
- **`ViewModel/`**:
  - `MainVM.cs` (partitioned across partial classes `_Code.cs`, `_Confirmations.cs`, `_File.cs`, `_Groups.cs`, `_Menu.cs`): Primary application ViewModel managing account selection, Steam Guard TOTP countdowns, confirmation fetching, race condition prevention via `CancellationTokenSource` and incrementing request IDs, and `IDisposable` cleanup.
  - `Linker/LinkAccountVM.cs`: 7-step wizard ViewModel for account binding using non-throwing pattern-matched `AuthResult`.
  - `MafileMover/MafileMoverVM.cs`: 4-step wizard ViewModel for migrating authenticators between devices.
  - `Other/`: `AddAccountVM`, `MafileExporterVM`, `SettingsVM`, `SetCryptPasswordVM`, `ConfirmCancelDialogVM`.
- **`Resources/` & `Localization/`**:
  - Standard resource files: `Strings.resx` (default English) and `Strings.ru.resx` (Russian).
  - `Strings.Designer.cs`: Strongly typed, compile-time checked string accessor.
  - `LocManager.cs`: Central culture and language coordinator (`CurrentCulture`, `CurrentUICulture`, events).
  - `LocalizationSource.cs`: Reactive `INotifyPropertyChanged` provider exposing `this[string key]`, enabling instant on-the-fly UI language switching across all Avalonia bindings without application restarts.

---

### 4.3. `NextAuth.Desktop` (`src/NextAuth.Desktop/`)
- **Target Framework:** `net8.0` (executables for `linux-x64`, `win-x64`, `osx-x64`).
- **Root Namespace:** `NextAuth.Desktop`.
- **Assembly & Product Name:** `SDAnext`.
- **Dependencies:** `NextAuth.Core`, `SteamLibForked`, `Avalonia` 12.1.4, `Material.Icons.Avalonia` 3.0.2, `DialogHost.Avalonia` 0.12.3.

#### Architecture & Submodules
- **`Program.cs` & `App.axaml.cs`**:
  - Avalonia application initialization using `ClassicDesktopStyleApplicationLifetime`.
  - Service container wiring: maps Avalonia implementations (`AvaloniaClipboardService`, `AvaloniaFileDialogService`, `AvaloniaNotificationService`, `AvaloniaDialogService`, `AvaloniaAppService`) to `NextAuth.Core` abstractions.
- **`Views/`**:
  - `MainWindow.axaml`: Main application window featuring custom borderless chrome, account list, Steam Guard code display, `CodeProgressBar`, and trade confirmation cards with dynamic localization.
  - `Dialogs/`: AXAML modal views (`AddAccountDialog`, `ConfirmCancelDialog`, `LoginAgainDialog`, `MafileImportDialog`, `SetCryptPasswordDialog`, `MafileExporterView`, `LinkerView`, `MafileMoverView`).
- **`Controls/`**:
  - `CodeProgressBar.cs`: Custom Avalonia control with direct geometry rendering via `DrawingContext.DrawGeometry` for silky smooth TOTP expiration visualization.
  - `DialogHeader.axaml`: Reusable dialog header featuring drag support and close buttons.
  - `HintBox.axaml`: Contextual notification banner with fade animations.
- **`Converters/`**:
  - `ConfirmationTitleConverter.cs`: Dual `IMultiValueConverter` and `IValueConverter` providing localized confirmation titles and item descriptions that respond instantly to language changes.
- **`Localization/`**:
  - `LocExtension.cs`: XAML markup extension `{loc:Loc KeyName}` binding directly to `LocalizationSource.Instance`.

---

### 4.4. `tests/SteamLib.Tests` (`tests/SteamLib.Tests/`)
- **Target Framework:** `net8.0` (`xUnit`, `Microsoft.NET.Test.Sdk`).
- **Purpose:** Comprehensive, offline unit and regression test suite verifying protocol, cryptography, serialization, and memory invariants without making live calls to Valve servers.

#### Test Suites
1. **`SteamGuardCodeGeneratorTests.cs`**: Tests TOTP code generation against deterministic test vectors, verifies RFC 6238 compliance, Steam custom Base32 alphabet mapping, and stability across 30-second time intervals.
2. **`SteamIdConversionTests.cs`**: Tests bidirectional conversions between `SteamId64`, `SteamId2`, `SteamId3`, and string representations, validating universe, account type, and edge-case handling in `Parse` and `TryParse`.
3. **`MafileSerializationTests.cs`**: Regression tests for `.mafile` v1 (legacy SDA), v2, v3, minified formats, camelCase property mappings, and round-trip preservation of unknown fields in `UnusedProperties`.
4. **`ScrappersTests.cs`**: Tests HTML and JSON response scrapers (`ScriptHeaderScrapper` extracting `sessionID` and `steamID`, `MobileConfirmationScrapper` parsing trade details, market listings, and session expiration states like `needauth`).
5. **`SteamServicesTests.cs`**: Tests `SteamTimeAligner`, the `TimeAligner` facade, `SteamSessionContext`, session validation, and dependency injection registration via `AddSteamLib()`.
6. **`ProtobufOptimizationTests.cs`**: Tests `PooledBufferStream`, `ProtoHelpers` zero-allocation byte conversion, `stackalloc`/`HMACSHA256.HashData` signature generation, and `ETokenRenewalType` binary wire compatibility.
7. **`JsonOptimizationTests.cs`**: Tests the source-generated `SteamJsonSerializerContext`, custom converters (`UnixTimeStampJsonConverter`, `SteamIdJsonConverter`), and models (`TimeQueryResponse`, `FinalizeLoginJson`, `WalletInfoSchema`, `WebTradeEligibility`).
8. **`MemorySafetyAndMaskingTests.cs`**: Verifies that sensitive credentials (passwords, JWT tokens, `shared_secret`, `identity_secret`) are masked in `ToString()` implementations across all session and data models, and that span-based cryptographic operations safely clear buffers via `ZeroMemory`.
9. **`TypedResultsTests.cs`**: Tests `SteamResult`, `SteamResult<T>`, and all branches of the discriminated union `AuthResult`.
10. **`UtilitiesReplacementTests.cs`**: Tests legacy utility migration and helper methods.
