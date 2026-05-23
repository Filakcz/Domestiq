using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DomestiqAvalonia.Models;

namespace DomestiqAvalonia.Services;

public class GpxService
{
    public List<RouteNode> ParseGpx(string filePath)
    {
        return new List<RouteNode>();
    }

    public List<double> GetElevationProfile(List<RouteNode> nodes)
    {
        var profile = new List<double>();
        foreach (var node in nodes)
        {
            profile.Add(node.Elevation);
        }
        return profile;
    }
}
