using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DomestiqAvalonia.Models;
using DomestiqAvalonia.Services;
using System.Linq;
using System.Collections.Generic;

namespace DomestiqAvalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly CsvImportService _csvService;

    public ObservableCollection<ActivityRecord> Activities { get; } = new();

    [ObservableProperty]
    private double? _currentFitness;

    [ObservableProperty]
    private double? _currentFatigue;

    [ObservableProperty]
    private double? _currentForm;

    [ObservableProperty]
    private string _recommendation = "no data";

    public MainWindowViewModel()
    {
        _csvService = new CsvImportService();
    }

    [RelayCommand]
    private void LoadCsv()
    {
        // TODO: Use a file picker in a real application
        string myCsvPath = "/home/filak/Downloads/intervals_activities.csv"; 

        var loadedActivities = _csvService.ImportActivities(myCsvPath)
            .OrderByDescending(a => a.Date)
            .ToList();

        Activities.Clear();

        foreach (var activity in loadedActivities)
        {
            Activities.Add(activity);
        }

        UpdateTrainingMetrics(loadedActivities);
    }

    private void UpdateTrainingMetrics(List<ActivityRecord> loadedActivities)
    {
        if (loadedActivities.Count == 0)
        {
            return;
        }

        var latest = loadedActivities.FirstOrDefault();
        if (latest != null)
        {
            CurrentFitness = latest.Fitness;
            CurrentFatigue = latest.Fatigue;
            if (CurrentFitness.HasValue && CurrentFatigue.HasValue)
            {
                CurrentForm = CurrentFitness - CurrentFatigue;
                GenerateRecommendation();
            }
        }
    }

    private void GenerateRecommendation()
    {
        if (!CurrentForm.HasValue)
        {
            return;
        }

        if (CurrentForm > 5)
        {
            Recommendation = "Fresh";
        }
        else if (CurrentForm > -10)
        {
            Recommendation = "optimal train";
        }
        else if (CurrentForm > -30)
        {
            Recommendation = "easy";
        }
        else
        {
            Recommendation = "Rest";
        }
    }
}
