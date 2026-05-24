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

    public void LoadFromPbf(string filePath)
    {
        Nodes.Clear();
        _grid.Clear();

        using var fileStream = File.OpenRead(filePath);
        var source = new PBFOsmStreamSource(fileStream);

        HashSet<long> usedNodeIds = new HashSet<long>(); 
        List<Way> ways = new List<Way>(); 

        foreach (var item in source)
        {
            if (item is Way way && way.Tags != null && way.Tags.ContainsKey("highway"))
            {
                if (way.Tags.TryGetValue("bicycle", out string b) && b == "no")
                {
                    continue;
                }

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
                    RouteNode node = new RouteNode(
                        osmNode.Latitude!.Value,
                        osmNode.Longitude!.Value,
                        id // pokud 0 tak dle aktualniho ticku
                    );

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

            bool isMotorway = (highway == "motorway" || highway == "motorway_link") && bike != "yes";
            
            bool isOffroad = false;
            
            // https://wiki.openstreetmap.org/wiki/Key:surface
            string[] badSurfaces = { "gravel", "dirt", "ground", "unpaved", "sand", "grass", "compacted", "fine_gravel", "woodchips", "earth" };
            if (surface != null && badSurfaces.Contains(surface))
            {
                isOffroad = true;
            }

            // https://wiki.openstreetmap.org/wiki/Key:highway
            string[] offroadHighways = { "track", "path", "footway", "steps", "pedestrian" };
            if (offroadHighways.Contains(highway))
            {
                if (surface == null || (surface != "asphalt" && surface != "concrete" && surface != "paved"))
                {
                    isOffroad = true;
                }
            }

            // https://wiki.openstreetmap.org/wiki/Key:tracktype
            if (tracktype != null && tracktype != "grade1")
            {
                isOffroad = true;
            }

            for (int i = 0; i < way.Nodes.Length - 1; i++)
            {
                long id1 = way.Nodes[i];
                long id2 = way.Nodes[i + 1];

                if (Nodes.TryGetValue(id1, out RouteNode? n1) && Nodes.TryGetValue(id2, out RouteNode? n2))
                {
                    double dist = n1.DistanceTo(n2);
                    
                    var edge1 = new RouteEdge(id2, dist, isMotorway, isOffroad);
                    var edge2 = new RouteEdge(id1, dist, isMotorway, isOffroad);

                    n1.Edges.Add(edge1);
                    n2.Edges.Add(edge2);
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
            bw.Write(node.Edges.Count);
            foreach (RouteEdge? edge in node.Edges)
            {
                bw.Write(edge.TargetId);
                bw.Write(edge.Distance);
                bw.Write(edge.IsMotorway);
                bw.Write(edge.IsOffroad);
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
            
            RouteNode node = new RouteNode(lat, lon, id: id);
            
            int edgeCount = br.ReadInt32();
            for (int j = 0; j < edgeCount; j++)
            {
                RouteEdge edge = new RouteEdge(br.ReadInt64(), br.ReadDouble(), br.ReadBoolean(), br.ReadBoolean());
                node.Edges.Add(edge);
            }
            Nodes[node.Id] = node;
            AddToGrid(node);
        }
    }
}
