using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Soenneker.TimeZones.Lookup.Abstract;
using Soenneker.Tests.HostedUnit;

namespace Soenneker.TimeZones.Lookup.Tests;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public sealed class TimeZoneLookupUtilTests : HostedUnitTest
{
    private readonly ITimeZoneLookupUtil _util;
    private readonly TimeZoneGeoJsonLoader _geoJsonLoader;

    public TimeZoneLookupUtilTests(Host host) : base(host)
    {
        _util = Resolve<ITimeZoneLookupUtil>(true);
        _geoJsonLoader = Resolve<TimeZoneGeoJsonLoader>(true);
    }

    [Test]
    public void Default()
    {
        _util.Should().NotBeNull();
    }

    [Test]
    public async Task GetTimeZoneId_should_resolve_real_locations_from_packaged_data()
    {
        string? chicago = await _util.GetTimeZoneId(41.8781, -87.6298);
        string? newYork = await _util.GetTimeZoneId(40.7128, -74.0060);
        string? losAngeles = await _util.GetTimeZoneId(34.0522, -118.2437);
        string? denver = await _util.GetTimeZoneId(39.7392, -104.9903);
        string? honolulu = await _util.GetTimeZoneId(21.3069, -157.8583);
        string? tokyo = await _util.GetTimeZoneId(35.6762, 139.6503);
        string? sydney = await _util.GetTimeZoneId(-33.8688, 151.2093);
        string? abidjan = await _util.GetTimeZoneId(5.36, -4.0083);

        chicago.Should().Be("America/Chicago");
        newYork.Should().Be("America/New_York");
        losAngeles.Should().Be("America/Los_Angeles");
        denver.Should().Be("America/Denver");
        honolulu.Should().Be("Pacific/Honolulu");
        tokyo.Should().Be("Asia/Tokyo");
        sydney.Should().Be("Australia/Sydney");
        abidjan.Should().Be("Africa/Abidjan");
    }

    [Test]
    public async Task GetTimeZoneId_should_return_matching_tzid()
    {
        var util = new TimeZoneLookupUtil(_geoJsonLoader, CreateGeoJsonStream);

        string? result = await util.GetTimeZoneId(5, 5);

        result.Should().Be("Etc/Test");
    }

    [Test]
    public async Task GetTimeZoneId_should_return_null_when_no_polygon_contains_coordinate()
    {
        var util = new TimeZoneLookupUtil(_geoJsonLoader, CreateGeoJsonStream);

        string? result = await util.GetTimeZoneId(50, 50);

        result.Should().BeNull();
    }

    [Test]
    public async Task GetTimeZoneId_should_exclude_polygon_holes()
    {
        var util = new TimeZoneLookupUtil(_geoJsonLoader, CreateGeoJsonStream);

        string? result = await util.GetTimeZoneId(14, 14);

        result.Should().BeNull();
    }

    [Test]
    public async Task GetTimeZoneId_should_throw_for_invalid_latitude()
    {
        var util = new TimeZoneLookupUtil(_geoJsonLoader, CreateGeoJsonStream);

        Func<Task> action = async () => await util.GetTimeZoneId(91, 0);

        await action.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    private static ValueTask<Stream> CreateGeoJsonStream(CancellationToken cancellationToken)
    {
        const string geoJson = """
                               {
                                 "type": "FeatureCollection",
                                 "features": [
                                   {
                                     "type": "Feature",
                                     "properties": {
                                       "tzid": "Etc/Test",
                                       "minLat": 0,
                                       "maxLat": 10,
                                       "minLon": 0,
                                       "maxLon": 10
                                     },
                                     "geometry": {
                                       "type": "MultiPolygon",
                                       "coordinates": [[[
                                         [0,0],
                                         [10,0],
                                         [10,10],
                                         [0,10],
                                         [0,0]
                                       ]]]
                                     }
                                   },
                                   {
                                     "type": "Feature",
                                     "properties": {
                                       "tzid": "Etc/Hole",
                                       "minLat": 10,
                                       "maxLat": 20,
                                       "minLon": 10,
                                       "maxLon": 20
                                     },
                                     "geometry": {
                                       "type": "Polygon",
                                       "coordinates": [
                                         [
                                           [10,10],
                                           [20,10],
                                           [20,20],
                                           [10,20],
                                           [10,10]
                                         ],
                                         [
                                           [12,12],
                                           [16,12],
                                           [16,16],
                                           [12,16],
                                           [12,12]
                                         ]
                                       ]
                                     }
                                   }
                                 ]
                               }
                               """;

        Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(geoJson));
        return ValueTask.FromResult(stream);
    }
}
