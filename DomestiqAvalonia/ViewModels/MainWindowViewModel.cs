using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DomestiqAvalonia.Models;
using DomestiqAvalonia.Services;

namespace DomestiqAvalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly PathfindingService _pathfindingService = new();
    private readonly OsmGraph _osmGraph = new();
    private List<RouteNode> _currentPath = new();

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

    [ObservableProperty]
    private int _plannedDistance = 30;

    [ObservableProperty]
    private bool _isNavMode = true;

    [ObservableProperty]
    private bool _isLoopMode = false;

    [ObservableProperty]
    private bool _isMetric = true;

    [ObservableProperty]
    private string? _elevationGeometry;

    [ObservableProperty]
    private string _elevationMinDisplay = "";

    [ObservableProperty]
    private string _elevationMaxDisplay = "";

    [ObservableProperty]
    private string _hoverInfo = "";

    [ObservableProperty]
    private double _hoverX = -10;

    [ObservableProperty]
    private RouteNode? _highlightPoint;
    private const double KilometersToMiles = 0.621371;


    partial void OnAvoidMotorwaysChanged(bool value)
    {
        AutoRecalculate();
    }

    partial void OnAvoidOffroadChanged(bool value)
    {
        AutoRecalculate();
    }

    private void AutoRecalculate()
    {
        if (IsNavMode && StartPoint != null && EndPoint != null)
        {
            PlanRouteInternal(StartPoint, EndPoint);
        }
        else if (IsLoopMode && Waypoints.Count > 0)
        {
            GenerateLoop();
        }
    }

    public string PlannedDistanceDisplay
    {
        get
        {
            if (IsMetric)
            {
                return $"Loop distance: {PlannedDistance} km";
            }

            double distanceInMiles = PlannedDistance * KilometersToMiles;
            return $"Loop distance: {distanceInMiles:F1} mi";
        }
    }

    public List<RouteNode> Waypoints { get; } = new();
    public event Action<List<RouteNode>>? PathFound;

    public void UpdateHover(double xPercent)
    {
        if (_currentPath.Count < 2)
        {
            return;
        }

        int index = (int)Math.Round(xPercent * (_currentPath.Count - 1));
        index = Math.Clamp(index, 0, _currentPath.Count - 1);

        var node = _currentPath[index];
        HighlightPoint = node;

        double dist = 0;
        for (int i = 0; i < index; i++)
        {
            dist += _currentPath[i].DistanceTo(_currentPath[i + 1]);
        }

        string eUnit;
        string dUnit;
        double ele;
        double dVal;

        if (IsMetric)
        {
            eUnit = "m";
            dUnit = "km";
            ele = node.Elevation;
            dVal = dist / 1000.0;
        }
        else
        {
            eUnit = "ft";
            dUnit = "mi";
            ele = node.Elevation * 3.28084;
            dVal = dist * 0.000621371;
        }

        HoverInfo = $"{ele:F0}{eUnit} | {dVal:F1}{dUnit}";
        HoverX = xPercent * 300;
    }

    public void ClearHover()
    {
        HighlightPoint = null;
        HoverX = -10;
        HoverInfo = "";
    }

    partial void OnIsMetricChanged(bool value)
    {
        OnPropertyChanged(nameof(PlannedDistanceDisplay));
        if (_currentPath.Count > 0)
        {
            UpdatePathInfo(_currentPath);
        }
    }

    partial void OnPlannedDistanceChanged(int value)
    {
        OnPropertyChanged(nameof(PlannedDistanceDisplay));
    }

    public MainWindowViewModel()
    {
        if (File.Exists("map.bin"))
        {
            _osmGraph.LoadBinary("map.bin");
            StatusMessage = "Graph loaded";
        }
    }

    partial void OnIsNavModeChanged(bool value)
    {
        if (value)
        {
            IsLoopMode = false;
            ClearRoute();
        }
    }

    partial void OnIsLoopModeChanged(bool value)
    {
        if (value)
        {
            IsNavMode = false;
            ClearRoute();
        }
    }

    public void UpdateSelectedPoint(double lat, double lon)
    {
        if (IsNavMode)
        {
            if (StartPoint == null || (StartPoint != null && EndPoint != null))
            {
                ClearRoute();
                StartPoint = new RouteNode(lat, lon);
                StatusMessage = "Set destination point";
            }
            else
            {
                EndPoint = new RouteNode(lat, lon);
                if (StartPoint != null)
                {
                    PlanRouteInternal(StartPoint, EndPoint);
                }
            }
        }
        else if (IsLoopMode)
        {
            ClearRoute();
            StartPoint = new RouteNode(lat, lon);
            Waypoints.Add(StartPoint);
            StatusMessage = "Start point set";
        }
    }

    [RelayCommand]
    public void ClearRoute()
    {
        StartPoint = null;
        EndPoint = null;
        Waypoints.Clear();
        _currentPath.Clear();
        ElevationGeometry = null;
        ElevationMinDisplay = "";
        ElevationMaxDisplay = "";
        PathFound?.Invoke(new List<RouteNode>());
        StatusMessage = "Ready";
    }

    public void SaveGpx(string filePath)
    {
        if (_currentPath.Count == 0)
        {
            return;
        }

        try
        {
            new GpxService().ExportToGpx(filePath, _currentPath);
            StatusMessage = "GPX Exported";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export error: {ex.Message}";
        }
    }

    private void PlanRouteInternal(RouteNode start, RouteNode end)
    {
        if (_osmGraph.Nodes.Count == 0)
        {
            StatusMessage = "No map loaded";
            return;
        }

        long sId = _osmGraph.FindNearestNode(start.Latitude, start.Longitude);
        long eId = _osmGraph.FindNearestNode(end.Latitude, end.Longitude);

        if (sId == -1 || eId == -1)
        {
            StatusMessage = "Nearest node error";
            return;
        }

        var path = _pathfindingService.FindPath(_osmGraph.Nodes[sId], _osmGraph.Nodes[eId], _osmGraph.Nodes, AvoidMotorways, AvoidOffroad);
        
        if (path != null)
        {
            UpdatePathInfo(path);
        }
        else
        {
            StatusMessage = "Path not found";
        }
    }

    [RelayCommand]
    private void GenerateLoop()
    {
        var basePoint = StartPoint;

        if (IsLoopMode)
        {
            basePoint = Waypoints.FirstOrDefault();
        }

        if (basePoint == null)
        {
            StatusMessage = "Select point first";
            return;
        }

        if (_osmGraph.Nodes.Count == 0)
        {
            StatusMessage = "No map loaded";
            return;
        }

        long sId = _osmGraph.FindNearestNode(basePoint.Latitude, basePoint.Longitude);
        if (sId == -1)
        {
            StatusMessage = "Nearest node error";
            return;
        }

        var startNode = _osmGraph.Nodes[sId];
        double radius = (PlannedDistance * 1000) / 4.0; // silnice nevedou primo po obvodu
        var rnd = new Random();
        double a1 = rnd.NextDouble() * 2 * Math.PI;
        double a2 = a1 + (Math.PI * 2 / 3);

        RouteNode? wp1 = GetOffsetNode(startNode, radius, a1);
        RouteNode? wp2 = GetOffsetNode(startNode, radius, a2);

        if (wp1 == null || wp2 == null)
        {
            StatusMessage = "Loop WP error";
            return;
        }

        var p1 = _pathfindingService.FindPath(startNode, wp1, _osmGraph.Nodes, AvoidMotorways, AvoidOffroad);
        var p2 = _pathfindingService.FindPath(wp1, wp2, _osmGraph.Nodes, AvoidMotorways, AvoidOffroad);
        var p3 = _pathfindingService.FindPath(wp2, startNode, _osmGraph.Nodes, AvoidMotorways, AvoidOffroad);

        if (p1 != null && p2 != null && p3 != null)
        {
            Waypoints.Clear();
            Waypoints.Add(startNode);
            Waypoints.Add(wp1);
            Waypoints.Add(wp2);

            List<RouteNode> fullPath = new List<RouteNode>();

            fullPath.AddRange(p1);
            fullPath.AddRange(p2.Skip(1)); 
            fullPath.AddRange(p3.Skip(1));
            UpdatePathInfo(fullPath);
        }
        else
        {
            StatusMessage = "Loop path error";
        }
    }

    private RouteNode? GetOffsetNode(RouteNode start, double r, double angle)
    {
        double latOff = (r / 111000.0) * Math.Cos(angle);
        double lonOff = (r / (111000.0 * Math.Cos(start.Latitude * Math.PI / 180.0))) * Math.Sin(angle);
        long id = _osmGraph.FindNearestNode(start.Latitude + latOff, start.Longitude + lonOff);
        
        if (id != -1)
        {
            return _osmGraph.Nodes[id];
        }
        return null;
    }

    private void UpdatePathInfo(List<RouteNode> path)
    {
        _currentPath = path;
        double dist = 0;
        double gain = 0;
        double loss = 0;

        for (int i = 0; i < path.Count - 1; i++)
        {
            dist += path[i].DistanceTo(path[i + 1]);
            double d = path[i + 1].Elevation - path[i].Elevation;
            if (d > 0)
            {
                gain += d;
            }
            else
            {
                loss += Math.Abs(d);
            }
        }

        if (IsMetric)
        {
            StatusMessage = $"Dist: {dist / 1000:F1}km | +{gain:F0}m / -{loss:F0}m";
            NormalizeElevation(path, 1.0);
        }
        else
        {
            double miles = dist * 0.000621371;
            double feetGain = gain * 3.28084;
            double feetLoss = loss * 3.28084;
            StatusMessage = $"Dist: {miles:F1}mi | +{feetGain:F0}ft / -{feetLoss:F0}ft";
            NormalizeElevation(path, 3.28084);
        }

        PathFound?.Invoke(path);
    }

    private void NormalizeElevation(List<RouteNode> path, double factor)
    {
        if (path.Count < 2)
        {
            ElevationGeometry = null;
            return;
        }

        // vic smooth max points
        int maxPoints = 300;
        int step = Math.Max(1, path.Count / maxPoints);
        var sampled = new List<double>();
        for (int i = 0; i < path.Count; i += step)
        {
            sampled.Add(path[i].Elevation * factor);
        }
        if ((path.Count - 1) % step != 0)
        {
            sampled.Add(path.Last().Elevation * factor);
        }

        double min = sampled.Min();
        double max = sampled.Max();
        double range = max - min;
        
        string unit;
        if (IsMetric)
        {
            unit = "m";
        }
        else
        {
            unit = "ft";
        }
        ElevationMinDisplay = $"{min:F0} {unit}";
        ElevationMaxDisplay = $"{max:F0} {unit}";

        if (range < 1)
        {
            range = 100;
        }

        int width = 300;
        int height = 100;

        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < sampled.Count; i++)
        {
            double x = (double)i / (sampled.Count - 1) * width;
            double y = height - (((sampled[i] - min) / range) * height * 0.8 + 10); // 80% vyska + 10px margin
            
            if (i == 0)
            {
                sb.Append($"M {x:F1},{y:F1} ");
            }
            else
            {
                sb.Append($"L {x:F1},{y:F1} ");
            }
        }

        sb.Append($"L {width}, {height} L 0, {height} Z");
        ElevationGeometry = sb.ToString();
    }

    public void LoadBinary(string path)
    {
        try
        {
            _osmGraph.LoadBinary(path);
            StatusMessage = "Loaded";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    public void LoadPbf(string path)
    {
        string? hgtFolder = null;
        if (Directory.Exists("elevation"))
        {
            hgtFolder = "elevation";
        }
        
        if (hgtFolder == null)
        {
            StatusMessage = "No elevation folder";
        }

        _osmGraph.LoadFromPbf(path, hgtFolder);
        _osmGraph.SaveBinary("map.bin");
        
        StatusMessage = "Graph loaded and cached";
    }
}
