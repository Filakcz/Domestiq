using Avalonia.Controls;
using Avalonia.Input;
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
        
        _pinLayer.Style = new SymbolStyle { Fill = new Brush(Color.Red), SymbolScale = 0.8 };
        map.Layers.Add(_pinLayer);

        MyMapControl.Map = map;

        var center = SphericalMercator.FromLonLat(14.4179, 50.1265);
        MyMapControl.Map.Navigator.CenterOnAndZoomTo(new MPoint(center.x, center.y), 1000);
    }

    private void OnMapClicked(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;

        var screenPosition = e.GetPosition(MyMapControl);
        var worldPosition = MyMapControl.Map.Navigator.Viewport.ScreenToWorld(screenPosition.X, screenPosition.Y);

        _pinLayer.Features = new[] { new PointFeature(worldPosition) };
        _pinLayer.DataHasChanged();

        var lonLat = SphericalMercator.ToLonLat(worldPosition.X, worldPosition.Y);
        vm.UpdateSelectedPoint(lonLat.lat, lonLat.lon);
    }
}
