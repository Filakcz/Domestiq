using System;
using System.Collections.Generic;
using DomestiqAvalonia.Models;

namespace DomestiqAvalonia.Services;

public class PathfindingService
{
    // dijsktra, A*
    public List<RouteNode> FindPath(RouteNode start, RouteNode end, List<RouteEdge> edges)
    {
        return new List<RouteNode> { start, end };
    }

    private double CalculateDistance(RouteNode a, RouteNode b)
    {
        // https://en.wikipedia.org/wiki/Haversine_formula
        return Math.Sqrt(Math.Pow(a.Latitude - b.Latitude, 2) + Math.Pow(a.Longitude - b.Longitude, 2));
    }
}
