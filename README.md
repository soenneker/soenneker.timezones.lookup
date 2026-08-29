[![](https://img.shields.io/nuget/v/soenneker.timezones.lookup.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.timezones.lookup/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.timezones.lookup/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.timezones.lookup/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.timezones.lookup.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.timezones.lookup/)

# Soenneker.TimeZones.Lookup

Fast Time Zone Resolution for .NET.

## Install

```bash
dotnet add package Soenneker.TimeZones.Lookup
```

## Quick start

```csharp
using Soenneker.TimeZones.Lookup.Registrars;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
var result = services.AddTimeZoneLookupUtilAsSingleton();
```

Adds `ITimeZoneLookupUtil` as a singleton service.

## What you get

- `ITimeZoneLookupUtil` — Fast Time Zone Resolution for .NET.
- `TimeZoneLookupUtilRegistrar` — Fast Time Zone Resolution for .NET.
- `TimeZoneGeoJsonLoader` — Represents the time zone geo json loader.

## API at a glance

| API | What it does | Result / important behavior |
| --- | --- | --- |
| `TimeZoneLookupUtilRegistrar.AddTimeZoneLookupUtilAsSingleton(services)` | Adds `ITimeZoneLookupUtil` as a singleton service. | The same service collection, so additional registrations can be chained. |
| `TimeZoneLookupUtilRegistrar.AddTimeZoneLookupUtilAsScoped(services)` | Adds `ITimeZoneLookupUtil` as a scoped service. | The same service collection, so additional registrations can be chained. |
