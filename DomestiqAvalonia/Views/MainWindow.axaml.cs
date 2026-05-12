using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using DomestiqAvalonia.ViewModels;

namespace DomestiqAvalonia.Views;

public partial class MainWindow : Window
{
    private MemoryLayer _pinLayer = new MemoryLayer { Name = "PinLayer" };

    public MainWindow()
    {
        InitializeComponent();
        InitializeMap();
    }

    private void InitializeMap()
    {
        var map = new Map();
        map.Layers.Add(OpenStreetMap.CreateTileLayer());
        
        _pinLayer.Style = new SymbolStyle { Fill = new Brush(Color.Red), SymbolScale = 0.4 };
        map.Layers.Add(_pinLayer);

        MyMapControl.Map = map;

        var center = SphericalMercator.FromLonLat(15.3, 49.75);
        MyMapControl.Map.Navigator.CenterOnAndZoomTo(new MPoint(center.x, center.y), 1000);
    }

    private void OnMapTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;

        var screenPosition = e.GetPosition(MyMapControl);
        var worldPosition = MyMapControl.Map.Navigator.Viewport.ScreenToWorld(screenPosition.X, screenPosition.Y);

        _pinLayer.Features = new[] { new PointFeature(worldPosition) };
        _pinLayer.DataHasChanged();

        var lonLat = SphericalMercator.ToLonLat(worldPosition.X, worldPosition.Y);
        vm.UpdateSelectedPoint(lonLat.lat, lonLat.lon);
    }

    private void OnZoomInClick(object? sender, RoutedEventArgs e)
    {
        MyMapControl.Map.Navigator.ZoomIn();
    }

    private void OnZoomOutClick(object? sender, RoutedEventArgs e)
    {
        MyMapControl.Map.Navigator.ZoomOut();
    }
}
