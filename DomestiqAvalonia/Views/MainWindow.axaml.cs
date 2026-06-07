using System.Collections.Generic;
using System.Linq;
using System.IO;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
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
    private MemoryLayer _highlightLayer = new MemoryLayer { Name = "HighlightLayer" };

    private static readonly SymbolStyle StartStyle = new()
    {
        Fill = new Brush(Color.FromArgb(255, 46, 204, 113)),
        Outline = new Pen(Color.White, 2),
        SymbolScale = 0.5
    };

    private static readonly SymbolStyle EndStyle = new()
    {
        Fill = new Brush(Color.FromArgb(255, 231, 76, 60)), 
        Outline = new Pen(Color.White, 2),
        SymbolScale = 0.5
    };

    private static readonly SymbolStyle HomeStyle = new()
    {
        Fill = new Brush(Color.Yellow), 
        Outline = new Pen(Color.Black, 2),
        SymbolScale = 0.5
    };

    private static readonly SymbolStyle HoverStyle = new()
    {
        Fill = new Brush(Color.FromArgb(220, 0, 255, 255)),
        Outline = new Pen(Color.Black, 3),
        SymbolScale = 0.55
    };


    public MainWindow()
    {
        InitializeComponent();
        InitializeMap();
        this.DataContextChanged += (s, e) => 
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.PathFound += path => UpdatePathLayer(path);
                vm.PropertyChanged += (sender, args) => 
                {
                    if (args.PropertyName == nameof(MainWindowViewModel.StartPoint) || 
                        args.PropertyName == nameof(MainWindowViewModel.EndPoint))
                    {
                        UpdatePinLayer(vm);
                    }
                    if (args.PropertyName == nameof(MainWindowViewModel.HighlightPoint))
                    {
                        UpdateHighlightLayer(vm);
                    }
                };
            }
        };
    }

    private void InitializeMap()
    {
        var map = new Map();
        map.Layers.Add(OpenStreetMap.CreateTileLayer());
        
        _pathLayer.Style = new VectorStyle { Line = new Pen(Color.Blue, 4) };
        _pinLayer.Style = null;
        _highlightLayer.Style = null;

        map.Layers.Add(_pathLayer);
        map.Layers.Add(_pinLayer);
        map.Layers.Add(_highlightLayer);

        MyMapControl.Map = map;

        var center = SphericalMercator.FromLonLat(15.3, 49.75);
        MyMapControl.Map.Navigator.CenterOnAndZoomTo(new MPoint(center.x, center.y), 1000);
    }

    private void UpdateHighlightLayer(MainWindowViewModel vm)
    {
        var features = new List<IFeature>();
        if (vm.HighlightPoint != null)
        {
            var p = SphericalMercator.FromLonLat(vm.HighlightPoint.Longitude, vm.HighlightPoint.Latitude);
            var feature = new PointFeature(new MPoint(p.x, p.y));
            feature.Styles.Add(HoverStyle);
            features.Add(feature);
        }
        _highlightLayer.Features = features;
        _highlightLayer.DataHasChanged();
        MyMapControl.RefreshGraphics();
    }

    private void OnElevationPointerMoved(object? sender, PointerEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm || sender is not Grid grid)
        {
            return;
        }
        
        var pos = e.GetPosition(grid);
        double xPercent = pos.X / grid.Bounds.Width;
        vm.UpdateHover(xPercent);
    }

    private void OnElevationPointerExited(object? sender, PointerEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }
        vm.ClearHover();
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
        var features = new List<IFeature>();
        
        if (vm.IsNavMode)
        {
            if (vm.StartPoint != null)
            {
                var p = SphericalMercator.FromLonLat(vm.StartPoint.Longitude, vm.StartPoint.Latitude);
                var f = new PointFeature(new MPoint(p.x, p.y));
                f.Styles.Add(StartStyle);
                features.Add(f);
            }
            if (vm.EndPoint != null)
            {
                var p = SphericalMercator.FromLonLat(vm.EndPoint.Longitude, vm.EndPoint.Latitude);
                var f = new PointFeature(new MPoint(p.x, p.y));
                f.Styles.Add(EndStyle);
                features.Add(f);
            }
        }
        else if (vm.IsLoopMode)
        {
            for (int i = 0; i < vm.Waypoints.Count; i++)
            {
                var wp = vm.Waypoints[i];
                var p = SphericalMercator.FromLonLat(wp.Longitude, wp.Latitude);
                var f = new PointFeature(new MPoint(p.x, p.y));
                
                if (i == 0)
                {
                    f.Styles.Add(HomeStyle);
                }
                else
                {
                    f.Styles.Add(EndStyle);
                }
                features.Add(f);
            }
        }

        _pinLayer.Features = features;
        _pinLayer.DataHasChanged();
        MyMapControl.RefreshGraphics();
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

    private async void OnLoadPbfClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
        {
            return;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Pick pbf file",
            AllowMultiple = false,
            FileTypeFilter = new[] 
            { 
                new FilePickerFileType("OSM PBF") { Patterns = new[] { "*.pbf" } } 
            }
        });

        if (files.Count > 0)
        {
            vm.LoadPbf(files[0].Path.LocalPath);
        }
    }

    private async void OnLoadBinClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
        {
            return;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Pick binary map file",
            AllowMultiple = false,
            FileTypeFilter = new[] 
            { 
                new FilePickerFileType("Binary map") { Patterns = new[] { "*.bin" } } 
            }
        });

        if (files.Count > 0)
        {
            vm.LoadBinary(files[0].Path.LocalPath);
        }
    }

    private async void OnLoadElevationFolderClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
        {
            return;
        }

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select elevation data folder (.hgt)",
            AllowMultiple = false
        });

        if (folders.Count > 0)
        {
            vm.LoadElevationFolder(folders[0].Path.LocalPath);
        }
    }

    private async void OnExportGpxClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
        {
            return;
        }

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Route to GPX",
            DefaultExtension = ".gpx",
            FileTypeChoices = new[] 
            { 
                new FilePickerFileType("GPX Files") { Patterns = new[] { "*.gpx" } } 
            }
        });

        if (file != null)
        {
            vm.SaveGpx(file.Path.LocalPath);
        }
    }
}
