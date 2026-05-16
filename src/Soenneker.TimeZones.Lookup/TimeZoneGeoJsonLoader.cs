using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.TimeZones.Lookup.Models;
using Soenneker.Utils.File.Abstract;
using Soenneker.Utils.Paths.Resources.Abstract;

namespace Soenneker.TimeZones.Lookup;

public sealed class TimeZoneGeoJsonLoader
{
    private static readonly string[] ResourceFileNames = ["timezones.geojson", Path.Combine("Data", "timezones.geojson")];

    private readonly IFileUtil _fileUtil;
    private readonly IResourcesPathUtil _resourcesPathUtil;

    public TimeZoneGeoJsonLoader(IFileUtil fileUtil, IResourcesPathUtil resourcesPathUtil)
    {
        _fileUtil = fileUtil;
        _resourcesPathUtil = resourcesPathUtil;
    }

    internal async Task<IReadOnlyList<TimeZoneFeature>> Load(Func<CancellationToken, ValueTask<Stream>>? streamFactory, CancellationToken cancellationToken)
    {
        await using Stream stream = streamFactory is not null
            ? await streamFactory(cancellationToken)
            : await OpenDefaultStream(cancellationToken);

        using JsonDocument document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        JsonElement root = document.RootElement;

        if (!root.TryGetProperty("type", out JsonElement typeElement) ||
            !string.Equals(typeElement.GetString(), "FeatureCollection", StringComparison.Ordinal))
        {
            throw new InvalidDataException("The timezone GeoJSON must be a FeatureCollection.");
        }

        if (!root.TryGetProperty("features", out JsonElement featuresElement) || featuresElement.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("The timezone GeoJSON is missing a features array.");

        var features = new List<TimeZoneFeature>();

        foreach (JsonElement featureElement in featuresElement.EnumerateArray())
        {
            TimeZoneFeature? feature = ReadFeature(featureElement);

            if (feature is not null)
                features.Add(feature);
        }

        return features;
    }

    private async ValueTask<Stream> OpenDefaultStream(CancellationToken cancellationToken)
    {
        foreach (string fileName in ResourceFileNames)
        {
            string filePath = await _resourcesPathUtil.GetResourceFilePath(fileName, cancellationToken);

            if (await _fileUtil.Exists(filePath, cancellationToken))
                return _fileUtil.OpenRead(filePath, log: false);
        }

        string expectedPath = await _resourcesPathUtil.GetResourceFilePath(ResourceFileNames[0], cancellationToken);
        throw new FileNotFoundException("Could not find timezone GeoJSON. Expected Soenneker.TimeZones.Data to provide timezones.geojson in Resources.",
            expectedPath);
    }

    private static TimeZoneFeature? ReadFeature(JsonElement featureElement)
    {
        if (!featureElement.TryGetProperty("properties", out JsonElement propertiesElement) ||
            !propertiesElement.TryGetProperty("tzid", out JsonElement tzidElement))
        {
            return null;
        }

        string? tzid = tzidElement.GetString();

        if (string.IsNullOrWhiteSpace(tzid))
            return null;

        if (!featureElement.TryGetProperty("geometry", out JsonElement geometryElement) ||
            !geometryElement.TryGetProperty("type", out JsonElement geometryTypeElement) ||
            !geometryElement.TryGetProperty("coordinates", out JsonElement coordinatesElement))
        {
            return null;
        }

        string? geometryType = geometryTypeElement.GetString();

        IReadOnlyList<IReadOnlyList<IReadOnlyList<Coordinate>>> multiPolygon = geometryType switch
        {
            "Polygon" => [ReadPolygon(coordinatesElement)],
            "MultiPolygon" => ReadMultiPolygon(coordinatesElement),
            _ => []
        };

        if (multiPolygon.Count == 0)
            return null;

        BoundingBox boundingBox = TryReadBoundingBox(propertiesElement, out BoundingBox configuredBoundingBox)
            ? configuredBoundingBox
            : CalculateBoundingBox(multiPolygon);

        return new TimeZoneFeature(tzid, multiPolygon, boundingBox);
    }

    private static bool TryReadBoundingBox(JsonElement propertiesElement, out BoundingBox boundingBox)
    {
        boundingBox = default;

        if (!propertiesElement.TryGetProperty("minLat", out JsonElement minLatElement) ||
            !propertiesElement.TryGetProperty("maxLat", out JsonElement maxLatElement) ||
            !propertiesElement.TryGetProperty("minLon", out JsonElement minLonElement) ||
            !propertiesElement.TryGetProperty("maxLon", out JsonElement maxLonElement))
        {
            return false;
        }

        boundingBox = new BoundingBox(minLatElement.GetDouble(), maxLatElement.GetDouble(), minLonElement.GetDouble(), maxLonElement.GetDouble());
        return true;
    }

    private static IReadOnlyList<IReadOnlyList<IReadOnlyList<Coordinate>>> ReadMultiPolygon(JsonElement coordinatesElement)
    {
        var multiPolygon = new List<IReadOnlyList<IReadOnlyList<Coordinate>>>();

        foreach (JsonElement polygonElement in coordinatesElement.EnumerateArray())
            multiPolygon.Add(ReadPolygon(polygonElement));

        return multiPolygon;
    }

    private static IReadOnlyList<IReadOnlyList<Coordinate>> ReadPolygon(JsonElement polygonElement)
    {
        var polygon = new List<IReadOnlyList<Coordinate>>();

        foreach (JsonElement ringElement in polygonElement.EnumerateArray())
        {
            var ring = new List<Coordinate>();

            foreach (JsonElement coordinateElement in ringElement.EnumerateArray())
            {
                if (coordinateElement.GetArrayLength() < 2)
                    continue;

                ring.Add(new Coordinate(coordinateElement[0].GetDouble(), coordinateElement[1].GetDouble()));
            }

            if (ring.Count > 0)
                polygon.Add(ring);
        }

        return polygon;
    }

    private static BoundingBox CalculateBoundingBox(IReadOnlyList<IReadOnlyList<IReadOnlyList<Coordinate>>> multiPolygon)
    {
        var minLat = double.MaxValue;
        var maxLat = double.MinValue;
        var minLon = double.MaxValue;
        var maxLon = double.MinValue;

        foreach (IReadOnlyList<IReadOnlyList<Coordinate>> polygon in multiPolygon)
        {
            foreach (IReadOnlyList<Coordinate> ring in polygon)
            {
                foreach (Coordinate coordinate in ring)
                {
                    minLat = Math.Min(minLat, coordinate.Latitude);
                    maxLat = Math.Max(maxLat, coordinate.Latitude);
                    minLon = Math.Min(minLon, coordinate.Longitude);
                    maxLon = Math.Max(maxLon, coordinate.Longitude);
                }
            }
        }

        return new BoundingBox(minLat, maxLat, minLon, maxLon);
    }
}
