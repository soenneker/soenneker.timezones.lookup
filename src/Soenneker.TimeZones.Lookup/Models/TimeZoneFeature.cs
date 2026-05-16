using System.Collections.Generic;

namespace Soenneker.TimeZones.Lookup.Models;

internal sealed record TimeZoneFeature(string Tzid, IReadOnlyList<IReadOnlyList<IReadOnlyList<Coordinate>>> MultiPolygon, BoundingBox BoundingBox);
