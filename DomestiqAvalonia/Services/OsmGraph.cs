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

        HashSet<long> usedNodeIds = new HashSet<long>(); // pouzite nody 
        List<Way> ways = new List<Way>(); // cesty dle OsmSharp.Way 

        foreach (var item in source)
        {
            if (item is Way way && IsRoad(way))
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
                long id = osmNode.Id ?? -1; // pokud null tak se = -1

                if (usedNodeIds.Contains(id))
                {
                    RouteNode node = new RouteNode(
                        osmNode.Latitude!.Value,
                        osmNode.Longitude!.Value,
                        osmNode.Id ?? 0 // vytvoreni id dle aktualniho ticku
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

            for (int i = 0; i < way.Nodes.Length - 1; i++)
            {
                long id1 = way.Nodes[i];
                long id2 = way.Nodes[i + 1];

                if (Nodes.TryGetValue(id1, out RouteNode? n1) && Nodes.TryGetValue(id2, out RouteNode? n2))
                {
                    double dist = n1.DistanceTo(n2);
                    n1.Edges.Add(new RouteEdge(id2, dist));
                    n2.Edges.Add(new RouteEdge(id1, dist));
                }
            }
        }
    }

    private bool IsRoad(Way way)
    {
        if (way.Tags == null)
        {
            return false;
        }
        return way.Tags.ContainsKey("highway");
        // zatim vsechny cesty (polni, dalnice atd, musi se filtrovat)
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
                node.Edges.Add(new RouteEdge(
                    br.ReadInt64(),
                    br.ReadDouble()
                ));
            }
            Nodes[node.Id] = node;
            AddToGrid(node);
        }
    }
}
