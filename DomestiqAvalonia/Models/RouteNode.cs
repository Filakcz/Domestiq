using System;

namespace DomestiqAvalonia.Models;

public class RouteNode
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Elevation { get; set; }
    public string? Name { get; set; }

    public RouteNode(double lat, double lon, double elevation = 0, string? name = null)
    {
        Latitude = lat;
        Longitude = lon;
        Elevation = elevation;
        Name = name;
    }
}
