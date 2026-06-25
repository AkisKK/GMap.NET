# GMap.NET Copilot Instructions

## Build & Test Commands

```sh
# Build the full solution
dotnet build GMap.NET.slnx -c Debug --nologo

# Build a specific project
dotnet build GMap.NET/GMap.NET.Core/GMap.NET.Core.csproj -c Debug --nologo
dotnet build GMap.NET/GMap.NET.WindowsForms/GMap.NET.WindowsForms.csproj -c Debug --nologo

# Run all tests
dotnet test GMap.NET.slnx -c Debug --nologo

# Run a single test class
dotnet test UnitTest/UnitTest.GMap.NET.Core/UnitTest.GMap.NET.Core.csproj --filter "ClassName=UnitTest.GMap.NET.Core.UnitTestGoogleMapProvider" --nologo

# Run a single test method
dotnet test UnitTest/UnitTest.GMap.NET.Core/UnitTest.GMap.NET.Core.csproj --filter "Name=TestGetPoint" --nologo
```

Build outputs go to `Build/Debug/` or `Build/Release/` (configured in `Directory.Build.props`).

## Architecture Overview

The solution is organized into these layers:

- **`GMap.NET/GMap.NET.Core`** — Platform-agnostic core library (`GMap.NET` namespace). Contains the tile engine, all map providers, projections, caching, and geocoding/routing abstractions.
- **`GMap.NET/GMap.NET.WindowsForms`** — WinForms UI control (`GMap.NET.WindowsForms` namespace). Implements `GMapControl : UserControl, IInterface`.
- **`UnitTest/UnitTest.GMap.NET.Core`** — MSTest unit tests for the Core library (targeting `net10.0-windows`).
- **`Demo/Demo.WindowsForms`** — Demo application.
- **`Tools/MapCruncher`** — Utility tool.

### Key Abstractions in Core

| Type | Role |
|---|---|
| `GMapProvider` | Abstract base for all map tile providers. Each provider has a unique `Guid Id` and `int DatabaseId`. |
| `GMapProviders` | Static registry; auto-discovers all providers via reflection on static fields at startup. |
| `PureProjection` | Abstract base for coordinate projections (Mercator, PlateCarree, SWEREF99, etc.). |
| `IPureImageCache` | Interface for tile storage backends (SQLite, MSSQL, MySQL, PostgreSQL, in-memory). |
| `IInterface` | Contract that both the WinForms and WPF controls must implement. |
| `GMaps` | Singleton-style maps manager; configures cache, access mode, and HTTP settings. |
| `Core` (internal) | Internal tile-loading engine used by all UI controls. |

### Provider Pattern

Every map provider follows a singleton pattern:
```csharp
public class OpenStreetMapProvider : OpenStreetMapProviderBase
{
    public static readonly OpenStreetMapProvider Instance;
    static OpenStreetMapProvider() { Instance = new OpenStreetMapProvider(); }
    private OpenStreetMapProvider() { }

    public override Guid Id => new Guid("...");
    public override string Name => "OpenStreetMap";
    public override PureProjection Projection => MercatorProjection.Instance;
    public override GMapProvider[] Overlays => [this];
    public override PureImage GetTileImage(GPoint pos, int zoom) { ... }
}
```
- Providers that support routing/geocoding implement `IRoutingProvider`, `IGeocodingProvider`, `IDirectionsProvider`, or `IRoadsProvider`.
- Register new providers by adding a `public static readonly` field on `GMapProviders`; the static constructor auto-discovers them via reflection.

### Tile Caching

`GMaps.PrimaryCache` defaults to `SQLitePureImageCache`. Alternative backends exist in `GMap.NET.Core/CacheProviders/`. Compile-time `#define` constants (`SQLite`, `MySQL_disabled`, `PostgreSQL_disabled`) control which backends are active.

### Coordinate System

The library uses `PointLatLng` (geographic) and `GPoint` (pixel/tile). Conversions go through the active `PureProjection`. All UI overlays (`GMapOverlay`) hold `GMapMarker`, `GMapRoute`, and `GMapPolygon` objects that use `PointLatLng` positions.

## Key Conventions

- **Target framework**: `net10.0-windows` for Core, WinForms, and test projects. `Tools/MapCruncher` targets `net48`. The solution file is `.slnx` format (not `.sln`).
- **Shared version/metadata**: Defined once in `Directory.Build.props` (version `2.1.7`, strong-named with `sn.snk`).
- **Compile-time conditionals**: Database backends are toggled with `#if SQLite` / `#if NETFRAMEWORK` / `#if NETCORE` etc. in `.csproj` `<DefineConstants>`.
- **`var` usage**: Use `var` when the type is apparent (`csharp_style_var_when_type_is_apparent = true:warning`); spell out the type for built-ins.
- **Braces**: Always use braces for control flow (`csharp_prefer_braces = true`).
- **Namespace style**: File-scoped namespaces (`namespace GMap.NET;`).
- **JSON**: Newtonsoft.Json is the JSON library (not `System.Text.Json`).
- **HTTP**: Use `HttpClientFactory` in `GMap.NET.Core/Internals/` rather than creating `HttpClient` instances directly.
- **Tests**: MSTest framework. Tests that call live map APIs (Google, OSM) require valid API keys embedded in the test class.
