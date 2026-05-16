namespace Soenneker.TimeZones.Lookup.Models;

internal readonly record struct BoundingBox(double MinLat, double MaxLat, double MinLon, double MaxLon)
{
    public bool Contains(double latitude, double longitude)
    {
        return latitude >= MinLat &&
               latitude <= MaxLat &&
               longitude >= MinLon &&
               longitude <= MaxLon;
    }
}
