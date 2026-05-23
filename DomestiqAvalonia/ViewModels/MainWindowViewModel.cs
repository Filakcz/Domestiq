using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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

        List<RouteNode>? path = _pathfindingService.FindPath(startNode, endNode, _osmGraph.Nodes);

        if (path != null)
        {
            StatusMessage = $"Path {path.Count} nodes";
            PathFound?.Invoke(path);
        }
        else
        {
            StatusMessage = "COuldnt find path";
        }
    }

    [RelayCommand]
    private void LoadGpx()
    {
        StatusMessage = "GPX";
    }

    [RelayCommand]
    private void SaveSettings()
    {
        StatusMessage = "Settings saved";
    }

    public void LoadPbf(string path)
    {
        StatusMessage = "PBF loading";
        _osmGraph.LoadFromPbf(path);
        _osmGraph.SaveBinary("map.bin");
        StatusMessage = "Graph loaded and cached";
    }
}
