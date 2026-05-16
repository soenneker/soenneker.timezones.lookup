using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.TimeZones.Lookup.Abstract;
using Soenneker.TimeZones.Lookup.Models;
using Soenneker.Utils.AsyncSingleton;

namespace Soenneker.TimeZones.Lookup;

/// <inheritdoc cref="ITimeZoneLookupUtil"/>
public sealed class TimeZoneLookupUtil: ITimeZoneLookupUtil
{
    private readonly AsyncSingleton<IReadOnlyList<TimeZoneFeature>> _features;

    public TimeZoneLookupUtil(TimeZoneGeoJsonLoader geoJsonLoader)
    {
        _features = new AsyncSingleton<IReadOnlyList<TimeZoneFeature>>(async cancellationToken => await geoJsonLoader.Load(null, cancellationToken));
    }

    public TimeZoneLookupUtil(TimeZoneGeoJsonLoader geoJsonLoader, Func<CancellationToken, ValueTask<Stream>> streamFactory)
    {
        _features = new AsyncSingleton<IReadOnlyList<TimeZoneFeature>>(async cancellationToken => await geoJsonLoader.Load(streamFactory, cancellationToken));
    }

    public async ValueTask<string?> GetTimeZoneId(double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        ValidateCoordinate(latitude, longitude);

        IReadOnlyList<TimeZoneFeature> features = await _features.Get(cancellationToken);

        foreach (TimeZoneFeature feature in features)
        {
            if (!feature.BoundingBox.Contains(latitude, longitude))
                continue;

            if (Contains(feature.MultiPolygon, latitude, longitude))
                return feature.Tzid;
        }

        return null;
    }

    private static void ValidateCoordinate(double latitude, double longitude)
    {
        if (double.IsNaN(latitude) || latitude is < -90 or > 90)
            throw new ArgumentOutOfRangeException(nameof(latitude), latitude, "Latitude must be between -90 and 90.");

        if (double.IsNaN(longitude) || longitude is < -180 or > 180)
            throw new ArgumentOutOfRangeException(nameof(longitude), longitude, "Longitude must be between -180 and 180.");
    }

    private static bool Contains(IReadOnlyList<IReadOnlyList<IReadOnlyList<Coordinate>>> multiPolygon, double latitude, double longitude)
    {
        foreach (IReadOnlyList<IReadOnlyList<Coordinate>> polygon in multiPolygon)
        {
            if (polygon.Count == 0)
                continue;

            if (!ContainsRing(polygon[0], latitude, longitude))
                continue;

            var insideHole = false;

            for (var i = 1; i < polygon.Count; i++)
            {
                if (ContainsRing(polygon[i], latitude, longitude))
                {
                    insideHole = true;
                    break;
                }
            }

            if (!insideHole)
                return true;
        }

        return false;
    }

    private static bool ContainsRing(IReadOnlyList<Coordinate> ring, double latitude, double longitude)
    {
        var inside = false;

        for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
        {
            Coordinate current = ring[i];
            Coordinate previous = ring[j];

            if (IsPointOnSegment(previous, current, latitude, longitude))
                return true;

            bool intersects = current.Latitude > latitude != previous.Latitude > latitude &&
                              longitude < (previous.Longitude - current.Longitude) * (latitude - current.Latitude) /
                              (previous.Latitude - current.Latitude) + current.Longitude;

            if (intersects)
                inside = !inside;
        }

        return inside;
    }

    private static bool IsPointOnSegment(Coordinate a, Coordinate b, double latitude, double longitude)
    {
        const double tolerance = 1e-12;

        double cross = (longitude - a.Longitude) * (b.Latitude - a.Latitude) - (latitude - a.Latitude) * (b.Longitude - a.Longitude);

        if (Math.Abs(cross) > tolerance)
            return false;

        return longitude >= Math.Min(a.Longitude, b.Longitude) - tolerance &&
               longitude <= Math.Max(a.Longitude, b.Longitude) + tolerance &&
               latitude >= Math.Min(a.Latitude, b.Latitude) - tolerance &&
               latitude <= Math.Max(a.Latitude, b.Latitude) + tolerance;
    }
}
