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
    private string _statusMessage = "readdy";

    [ObservableProperty]
    private ObservableCollection<RouteNode> _routeNodes = new();

    [ObservableProperty]
    private ObservableCollection<double> _elevationProfile = new();

    public MainWindowViewModel()
    {
        _pathfindingService = new PathfindingService();
        _gpxService = new GpxService();
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
