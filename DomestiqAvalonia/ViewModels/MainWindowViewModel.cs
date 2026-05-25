using System;
using System.Collections.Generic;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DomestiqAvalonia.Models;
using DomestiqAvalonia.Services;

namespace DomestiqAvalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly PathfindingService _pathfindingService;
    private readonly OsmGraph _osmGraph;

    [ObservableProperty]
    private RouteNode? _startPoint;

    [ObservableProperty]
    private RouteNode? _endPoint;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _avoidMotorways = true;

    [ObservableProperty]
    private bool _avoidOffroad = false;

    public event Action<List<RouteNode>>? PathFound;

    public MainWindowViewModel()
    {
        _pathfindingService = new PathfindingService();
        _osmGraph = new OsmGraph();
        
        if (File.Exists("map.bin"))
        {
            _osmGraph.LoadBinary("map.bin");
            StatusMessage = "Graph loaded";
        }
    }

    public void UpdateSelectedPoint(double lat, double lon)
    {
        if (StartPoint == null || (StartPoint != null && EndPoint != null))
        {
            StartPoint = new RouteNode(lat, lon);
            EndPoint = null;
            StatusMessage = $"Start: {lat} {lon}";
        }
        else
        {
            EndPoint = new RouteNode(lat, lon);
            StatusMessage = $"End: {lat} {lon}";
            PlanRoute();
        }
    }

    [RelayCommand]
    private void PlanRoute()
    {
        if (StartPoint == null || EndPoint == null)
        {
            StatusMessage = "Select start a end";
            return;
        }

        if (_osmGraph.Nodes.Count == 0)
        {
            StatusMessage = "No graph";
            return;
        }

        long startNodeId = _osmGraph.FindNearestNode(StartPoint.Latitude, StartPoint.Longitude);
        long endNodeId = _osmGraph.FindNearestNode(EndPoint.Latitude, EndPoint.Longitude);

        if (startNodeId == -1 || endNodeId == -1)
        {
            StatusMessage = "Error: nearest node";
            return;
        }

        RouteNode startNode = _osmGraph.Nodes[startNodeId];
        RouteNode endNode = _osmGraph.Nodes[endNodeId];

        List<RouteNode>? path = _pathfindingService.FindPath(startNode, endNode, _osmGraph.Nodes, AvoidMotorways, AvoidOffroad);

        if (path != null)
        {
            double totalDist = 0;
            for (int i = 0; i < path.Count - 1; i++)
            {
                totalDist += path[i].DistanceTo(path[i + 1]);
            }
            
            StatusMessage = $"Path {path.Count} nodes, distance: {totalDist / 1000} km, elevation: idk m";
            PathFound?.Invoke(path);
        }
        else
        {
            StatusMessage = "Couldnt find path";
        }
    }

    [RelayCommand]
    private void LoadGpx()
    {
        StatusMessage = "GPX";
    }

    public void LoadPbf(string path)
    {
        StatusMessage = "PBF loading";
        _osmGraph.LoadFromPbf(path);
        _osmGraph.SaveBinary("map.bin");
        StatusMessage = "Graph loaded and cached";
    }
}
