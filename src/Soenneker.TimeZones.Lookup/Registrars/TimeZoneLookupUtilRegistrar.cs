using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.TimeZones.Lookup.Abstract;
using Soenneker.Utils.File.Registrars;
using Soenneker.Utils.Paths.Resources.Registrars;

namespace Soenneker.TimeZones.Lookup.Registrars;

/// <summary>
/// Fast Time Zone Resolution for .NET
/// </summary>
public static class TimeZoneLookupUtilRegistrar
{
    /// <summary>
    /// Adds <see cref="ITimeZoneLookupUtil"/> as a singleton service. <para/>
    /// </summary>
    /// <param name="services">Service collection that receives the registration.</param>
    /// <returns>The same service collection, so additional registrations can be chained.</returns>
    public static IServiceCollection AddTimeZoneLookupUtilAsSingleton(this IServiceCollection services)
    {
        services.AddFileUtilAsSingleton()
                .AddResourcesPathUtilAsSingleton()
                .TryAddSingleton<TimeZoneGeoJsonLoader>();

        services
                .TryAddSingleton<ITimeZoneLookupUtil, TimeZoneLookupUtil>();

        return services;
    }

    /// <summary>
    /// Adds <see cref="ITimeZoneLookupUtil"/> as a scoped service. <para/>
    /// </summary>
    /// <param name="services">Service collection that receives the registration.</param>
    /// <returns>The same service collection, so additional registrations can be chained.</returns>
    public static IServiceCollection AddTimeZoneLookupUtilAsScoped(this IServiceCollection services)
    {
        services.AddFileUtilAsScoped()
                .AddResourcesPathUtilAsScoped()
                .TryAddScoped<TimeZoneGeoJsonLoader>();

        services
                .TryAddScoped<ITimeZoneLookupUtil, TimeZoneLookupUtil>();

        return services;
    }
}
