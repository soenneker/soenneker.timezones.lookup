using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.TimeZones.Lookup.Abstract;

/// <summary>
/// Fast Time Zone Resolution for .NET
/// </summary>
public interface ITimeZoneLookupUtil
{
    /// <summary>
    /// Gets the IANA time zone identifier for the supplied coordinate, or <see langword="null"/> if no polygon contains it.
    /// </summary>
    /// <param name="latitude">Latitude in decimal degrees. Must be between -90 and 90.</param>
    /// <param name="longitude">Longitude in decimal degrees. Must be between -180 and 180.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    ValueTask<string?> GetTimeZoneId(double latitude, double longitude, CancellationToken cancellationToken = default);
}
