using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DomestiqAvalonia.Models;
using DomestiqAvalonia.Services;

namespace DomestiqAvalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly PathfindingService _pathfindingService;
    private readonly GpxService _gpxService;

    [ObservableProperty]
    private double? _selectedLatitude;

    [ObservableProperty]
    private double? _selectedLongitude;

    [ObservableProperty]
    private string _statusMessage = "ready";

    [ObservableProperty]
    private ObservableCollection<RouteNode> _routeNodes = new();

    [ObservableProperty]
    private ObservableCollection<double> _elevationProfile = new();

    public MainWindowViewModel()
    {
        _pathfindingService = new PathfindingService();
        _gpxService = new GpxService();
    }

    public void UpdateSelectedPoint(double lat, double lon)
    {
        SelectedLatitude = lat;
        SelectedLongitude = lon;
        StatusMessage = $"Selected: {lat} {lon}";
    }

    [RelayCommand]
    private void PlanRoute()
    {
        StatusMessage = "Planning";
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
}
