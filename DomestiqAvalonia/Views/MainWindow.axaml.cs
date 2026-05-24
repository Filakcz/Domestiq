using System.Collections.Generic;
using System.Linq;
using System.IO;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Nts;
using Mapsui.UI.Avalonia;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using DomestiqAvalonia.ViewModels;
using DomestiqAvalonia.Models;

namespace DomestiqAvalonia.Views;

public partial class MainWindow : Window
{
    private MemoryLayer _pinLayer = new MemoryLayer { Name = "PinLayer" };
    private MemoryLayer _pathLayer = new MemoryLayer { Name = "PathLayer" };

    public MainWindow()
    {
        InitializeComponent();
        InitializeMap();
        this.DataContextChanged += (s, e) => 
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.PathFound += path => UpdatePathLayer(path);
            }
        };
    }

    private void InitializeMap()
    {
        var map = new Map();
        map.Layers.Add(OpenStreetMap.CreateTileLayer());
        
        _pinLayer.Style = new SymbolStyle { Fill = new Brush(Color.Red), SymbolScale = 0.4 };
        _pathLayer.Style = new VectorStyle { Line = new Pen(Color.Blue, 4) };

        map.Layers.Add(_pathLayer);
        map.Layers.Add(_pinLayer);

        MyMapControl.Map = map;

        var center = SphericalMercator.FromLonLat(15.3, 49.75);
        MyMapControl.Map.Navigator.CenterOnAndZoomTo(new MPoint(center.x, center.y), 1000);
    }

    private void OnMapTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        var screenPosition = e.GetPosition(MyMapControl);
        var worldPosition = MyMapControl.Map.Navigator.Viewport.ScreenToWorld(screenPosition.X, screenPosition.Y);
        var lonLat = SphericalMercator.ToLonLat(worldPosition.X, worldPosition.Y);

        vm.UpdateSelectedPoint(lonLat.lat, lonLat.lon);
        UpdatePinLayer(vm);
    }

    private void UpdatePinLayer(MainWindowViewModel vm)
    {
        List<PointFeature> features = new List<PointFeature>();
        if (vm.StartPoint != null)
        {
            var p = SphericalMercator.FromLonLat(vm.StartPoint.Longitude, vm.StartPoint.Latitude);
            features.Add(new PointFeature(new MPoint(p.x, p.y)));
        }
        if (vm.EndPoint != null)
        {
            var p = SphericalMercator.FromLonLat(vm.EndPoint.Longitude, vm.EndPoint.Latitude);
            features.Add(new PointFeature(new MPoint(p.x, p.y)));
        }
        _pinLayer.Features = features;
        _pinLayer.DataHasChanged();
    }

    private void UpdatePathLayer(List<RouteNode> path)
    {
        var points = path.Select(n => SphericalMercator.FromLonLat(n.Longitude, n.Latitude))
                         .Select(p => new MPoint(p.x, p.y)).ToList();
        
        var lineString = new NetTopologySuite.Geometries.LineString(points.Select(p => new NetTopologySuite.Geometries.Coordinate(p.X, p.Y)).ToArray());
        _pathLayer.Features = new[] { new GeometryFeature(lineString) };
        _pathLayer.DataHasChanged();
        MyMapControl.RefreshGraphics();
    }

    private void OnZoomInClick(object? sender, RoutedEventArgs e)
    {
        MyMapControl.Map.Navigator.ZoomIn();
    }

    private void OnZoomOutClick(object? sender, RoutedEventArgs e)
    {
        MyMapControl.Map.Navigator.ZoomOut();
    }

    private void OnLoadPbfClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            if (File.Exists("/home/filak/Downloads/praha-260507.osm.pbf"))
            {
                vm.LoadPbf("/home/filak/Downloads/praha-260507.osm.pbf");
            }
            else
            {
                vm.StatusMessage = "Pbf not found";
            }
        }
    }
}
