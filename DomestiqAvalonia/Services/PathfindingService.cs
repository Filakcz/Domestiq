using System.Collections.Generic;
using DomestiqAvalonia.Models;

namespace DomestiqAvalonia.Services;

public class PathfindingService
{
    public int LastNodesVisited { get; private set; }

    public List<RouteNode>? FindPath(RouteNode start, RouteNode end, Dictionary<long, RouteNode> nodes, bool avoidMotorway, bool avoidOffroad)
    {
        if (start == null || end == null)
        {
            return null;
        }

        LastNodesVisited = 0;
        Dictionary<long, double> distances = new Dictionary<long, double>();
        Dictionary<long, long> previous = new Dictionary<long, long>();
        PriorityQueue<long, double> pq = new PriorityQueue<long, double>();

        distances[start.Id] = 0;
        pq.Enqueue(start.Id, 0 + start.DistanceTo(end));

        while (pq.Count > 0)
        {
            long cur = pq.Dequeue();
            LastNodesVisited++;

            if (cur == end.Id)
            {
                List<RouteNode> path = new List<RouteNode>();
                long currentId = cur;
                while (previous.TryGetValue(currentId, out long prevId))
                {
                    path.Add(nodes[currentId]);
                    currentId = prevId;
                }
                path.Add(start);
                path.Reverse();
                return path;
            }

            if (!nodes.TryGetValue(cur, out RouteNode? node))
            {
                continue;
            }

            foreach (RouteEdge edge in node.Edges)
            {
                if (avoidMotorway && edge.IsMotorway)
                {
                    continue;
                }
                
                double weight = 1.0;
                
                // 1=Motorway, 2=Primary, 3=Tertiary, 4=Residential, 5=Service/Path
                if (edge.Priority == 2)
                {
                    weight = 0.8;
                }
                else if (edge.Priority == 3) 
                {
                    // nejlepsi
                    weight = 0.7; 
                }
                else if (edge.Priority == 5)
                {
                    weight = 3.0; 
                }
                else if (edge.Priority == 1 && !avoidMotorway)
                {
                    weight = 0.5;
                }

                if (avoidOffroad && edge.IsOffroad)
                {
                    weight *= 10.0;
                }
                else if (!avoidOffroad && edge.IsOffroad)
                {
                    weight *= 0.6;
                }

                if (nodes.TryGetValue(edge.TargetId, out RouteNode? targetNode))
                {
                    double eleDiff = targetNode.Elevation - node.Elevation;
                    if (eleDiff > 0)
                    {
                        weight += (eleDiff / edge.Distance) * 4.0;
                    }

                    double newDist = distances[cur] + (edge.Distance * weight);
                    if (!distances.ContainsKey(edge.TargetId) || newDist < distances[edge.TargetId])
                    {
                        distances[edge.TargetId] = newDist;
                        previous[edge.TargetId] = cur;
                        pq.Enqueue(edge.TargetId, newDist + targetNode.DistanceTo(end));
                    }
                }
            }
        }

        return null;
    }
}
