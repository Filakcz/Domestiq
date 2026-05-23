using System;
using System.Collections.Generic;

namespace DomestiqAvalonia.Models;

public class RouteNode
{
    public long Id { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Elevation { get; set; }
    public string? Name { get; set; }
    public List<RouteEdge> Edges { get; set; } = new();

    public RouteNode(double lat, double lon, long id = 0, double elevation = 0, string? name = null)
    {
        Latitude = lat;
        Longitude = lon;
        Elevation = elevation;
        Name = name;
        if (id == 0)
        {
            Id = DateTime.Now.Ticks;
        }
        else
        {
            Id = id;
        }
    }

    public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        // prej const zrychli kompilator
        const double R = 6371000;
        const double DegToRad = Math.PI / 180.0;

        double dLat = (lat2 - lat1) * DegToRad;
        double dLon = (lon2 - lon1) * DegToRad;

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(lat1 * DegToRad) * Math.Cos(lat2 * DegToRad) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        return 2 * R * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    public double DistanceTo(RouteNode other)
    {
        return CalculateDistance(Latitude, Longitude, other.Latitude, other.Longitude);
    }
}
