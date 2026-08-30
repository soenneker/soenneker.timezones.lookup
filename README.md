[![](https://img.shields.io/nuget/v/soenneker.timezones.lookup.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.timezones.lookup/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.timezones.lookup/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.timezones.lookup/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.timezones.lookup.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.timezones.lookup/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.timezones.lookup/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.timezones.lookup/actions/workflows/codeql.yml)

# Soenneker.TimeZones.Lookup

Resolves latitude and longitude coordinates to IANA time-zone identifiers using a packaged GeoJSON boundary dataset.

## Installation

```bash
dotnet add package Soenneker.TimeZones.Lookup
```

The lookup package references `Soenneker.TimeZones.Data`, so the default boundary resource is included automatically.

## Registration

```csharp
using Soenneker.TimeZones.Lookup.Registrars;

builder.Services.AddTimeZoneLookupUtilAsSingleton();
```

The singleton registration is recommended for most applications because the parsed geographic features are loaded once and retained for later lookups. A scoped registrar is available when isolation is more important than repeating that memory and parse cost per scope.

## Usage

```csharp
using Soenneker.TimeZones.Lookup.Abstract;

public sealed class LocationService(ITimeZoneLookupUtil timeZones)
{
    public ValueTask<string?> GetZone(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        return timeZones.GetTimeZoneId(latitude, longitude, cancellationToken);
    }
}

string? zone = await locationService.GetZone(41.8781, -87.6298);
// America/Chicago
```

Pass latitude first and longitude second. Latitude must be from -90 through 90 and longitude from -180 through 180; invalid values throw `ArgumentOutOfRangeException`. A valid coordinate returns `null` when no dataset polygon contains it, such as an uncovered ocean coordinate.

## Loading behavior

The first lookup parses the GeoJSON and caches the resulting features for that `TimeZoneLookupUtil` instance. Concurrent callers share that initialization. Cancellation can stop the initial load; once loaded, lookups operate on the in-memory geometry.

The public constructor overload accepting a stream factory supports a custom GeoJSON dataset. The factory must return a readable stream containing a `FeatureCollection`; the loader owns and disposes the returned stream after parsing. Features use `properties.tzid` and `Polygon` or `MultiPolygon` geometry.

Coordinates on a polygon edge count as contained. If the dataset contains overlapping zones, the first matching feature determines the result.
