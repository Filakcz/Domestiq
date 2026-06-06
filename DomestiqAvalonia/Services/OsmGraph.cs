using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OsmSharp;
using OsmSharp.Streams;
using DomestiqAvalonia.Models;

namespace DomestiqAvalonia.Services;

public class OsmGraph
{
    public Dictionary<long, RouteNode> Nodes = new();
    
    private Dictionary<(int, int), List<long>> _grid = new();
    private const double GridSize = 0.01; // 1 km

    public void LoadFromPbf(string filePath, string? hgtFolder)
    {
        Nodes.Clear();
        _grid.Clear();

        ElevationService? elevationService = null;
        if (!string.IsNullOrEmpty(hgtFolder))
        {
            elevationService = new ElevationService(hgtFolder);
        }

        using var fileStream = File.OpenRead(filePath);
        var source = new PBFOsmStreamSource(fileStream);

        HashSet<long> usedNodeIds = new HashSet<long>(); 
        List<Way> ways = new List<Way>(); 

        foreach (var item in source)
        {
            if (item is Way way && way.Tags != null && way.Tags.ContainsKey("highway"))
            {
                ways.Add(way);
                if (way.Nodes != null)
                {
                    foreach (long nodeId in way.Nodes)
                    {
                        usedNodeIds.Add(nodeId);
                    }
                }
            }
        }

        fileStream.Position = 0;
        source = new PBFOsmStreamSource(fileStream);

        foreach (var item in source)
        {
            if (item is OsmSharp.Node osmNode)
            {
                long id = osmNode.Id ?? 0; // pokud null tak se = 0

                if (usedNodeIds.Contains(id))
                {
                    double lat = osmNode.Latitude!.Value;
                    double lon = osmNode.Longitude!.Value;
                    short elev;
                    if (elevationService != null)
                    {
                        elev = elevationService.GetElevation(lat, lon);
                    }
                    else
                    {
                        elev = 0;
                    }

                    RouteNode node = new RouteNode(lat, lon, id, elev);

                    Nodes[node.Id] = node;
                    AddToGrid(node);
                }
            }
        }

        foreach (Way way in ways)
        {
            if (way.Nodes == null || way.Nodes.Length < 2)
            {
                continue;
            }

            way.Tags.TryGetValue("highway", out string highway);
            way.Tags.TryGetValue("surface", out string surface);
            way.Tags.TryGetValue("tracktype", out string tracktype);
            way.Tags.TryGetValue("bicycle", out string bike);

            bool isMotorway = (highway == "motorway" || highway == "motorway_link");
            bool isOffroad = false;
            int priority = 4;

            if (isMotorway || highway == "trunk")
            {
                priority = 1;
            }
            else if (highway == "primary" || highway == "secondary")
            {
                priority = 2;
            }
            else if (highway == "tertiary")
            {
                priority = 3;
            }
            else if (highway == "service" || highway == "track" || highway == "path")
            {
                priority = 5;
            }

            string[] badSurfaces = { "gravel", "dirt", "ground", "unpaved", "sand", "grass", "compacted", "fine_gravel", "woodchips", "earth" };
            if (surface != null && badSurfaces.Contains(surface))
            {
                isOffroad = true;
            }

            string[] offroadHighways = { "track", "path", "footway", "steps", "pedestrian" };
            if (offroadHighways.Contains(highway))
            {
                if (surface == null || (surface != "asphalt" && surface != "concrete" && surface != "paved"))
                {
                    isOffroad = true;
                }
            }
            if (tracktype != null && tracktype != "grade1")
            {
                isOffroad = true;
            }

            for (int i = 0; i < way.Nodes.Length - 1; i++)
            {
                if (Nodes.TryGetValue(way.Nodes[i], out RouteNode? n1) && Nodes.TryGetValue(way.Nodes[i + 1], out RouteNode? n2))
                {
                    double dist = n1.DistanceTo(n2);
                    n1.Edges.Add(new RouteEdge(n2.Id, dist, isMotorway, isOffroad, priority));
                    n2.Edges.Add(new RouteEdge(n1.Id, dist, isMotorway, isOffroad, priority));
                }
            }
        }
    }

    private void AddToGrid(RouteNode node)
    {
        int gx = (int)(node.Longitude / GridSize); // orizne desetinnou cast bez zaokrouhleni
        int gy = (int)(node.Latitude / GridSize);
        var key = (gx, gy);
        if (!_grid.TryGetValue(key, out List<long>? list))
        {
            list = new List<long>();
            _grid[key] = list;
        }
        list.Add(node.Id);
    }

    public long FindNearestNode(double lat, double lon)
    {
        int gx = (int)(lon / GridSize);
        int gy = (int)(lat / GridSize);

        long bestId = -1;
        double bestDist = double.MaxValue;

        for (int x = gx - 1; x <= gx + 1; x++)
        {
            for (int y = gy - 1; y <= gy + 1; y++)
            {
                if (_grid.TryGetValue((x, y), out List<long>? nodeIds))
                {
                    foreach (long id in nodeIds)
                    {
                        RouteNode node = Nodes[id];
                        double d = RouteNode.CalculateDistance(lat, lon, node.Latitude, node.Longitude);
                        if (d < bestDist)
                        {
                            bestDist = d;
                            bestId = id;
                        }
                    }
                }
            }
        }

        return bestId;
    }

    public void SaveBinary(string filePath)
    {
        using var fs = File.Create(filePath);
        using var bw = new BinaryWriter(fs);

        bw.Write(Nodes.Count);
        foreach (RouteNode node in Nodes.Values)
        {
            bw.Write(node.Id);
            bw.Write(node.Latitude);
            bw.Write(node.Longitude);
            bw.Write(node.Elevation);
            bw.Write(node.Edges.Count);
            foreach (var edge in node.Edges)
            {
                bw.Write(edge.TargetId);
                bw.Write(edge.Distance);
                bw.Write(edge.IsMotorway);
                bw.Write(edge.IsOffroad);
                bw.Write(edge.Priority);
            }
        }
    }

    public void LoadBinary(string filePath)
    {
        Nodes.Clear();
        _grid.Clear();

        using var fs = File.OpenRead(filePath);
        using var br = new BinaryReader(fs);

        int nodeCount = br.ReadInt32();
        for (int i = 0; i < nodeCount; i++)
        {
            long id = br.ReadInt64();
            double lat = br.ReadDouble();
            double lon = br.ReadDouble();
            short elev = br.ReadInt16();
            
            RouteNode node = new RouteNode(lat, lon, id, elev);
            
            int edgeCount = br.ReadInt32();
            for (int j = 0; j < edgeCount; j++)
            {
                RouteEdge edge = new RouteEdge(br.ReadInt64(), br.ReadDouble(), br.ReadBoolean(), br.ReadBoolean(), br.ReadInt32());
                node.Edges.Add(edge);
            }
            Nodes[node.Id] = node;
            AddToGrid(node);
        }
    }
}
