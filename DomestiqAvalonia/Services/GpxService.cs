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

    public void ExportToGpx(string filePath, List<RouteNode> path)
    {
        using var fs = File.Create(filePath);
        using var sw = new StreamWriter(fs);

        sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sw.WriteLine("<gpx version=\"1.1\" creator=\"DomestiqAvalonia\" xmlns=\"http://www.topografix.com/GPX/1/1\">");
        sw.WriteLine("  <trk>");
        sw.WriteLine("    <name>Planned Route</name>");
        sw.WriteLine("    <trkseg>");

        foreach (var node in path)
        {
            sw.WriteLine(System.FormattableString.Invariant($"      <trkpt lat=\"{node.Latitude}\" lon=\"{node.Longitude}\">"));
            sw.WriteLine(System.FormattableString.Invariant($"        <ele>{node.Elevation}</ele>"));
            sw.WriteLine("      </trkpt>");
        }

        sw.WriteLine("    </trkseg>");
        sw.WriteLine("  </trk>"); 
        sw.WriteLine("</gpx>");
    }
}
