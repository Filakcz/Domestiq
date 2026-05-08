using CsvHelper.Configuration.Attributes;
using System;

namespace DomestiqAvalonia.Models;

public class ActivityRecord
{
    [Name("id")]
    public string? Id { get; set; }

    [Name("name")]
    public string? Name { get; set; }

    [Name("start_date_local")]
    public DateTime? Date { get; set; }

    [Name("type")]
    public string? Type { get; set; }

    [Name("distance")]
    public double? Distance { get; set;}

    [Name("moving_time")]
    public double? MovingTime { get; set; }

    [Name("icu_training_load")]
    public double? Tss { get; set;}

    [Name("icu_intensity")]
    public double? Intensity { get; set; }

    [Name("icu_average_watts")]
    public double? AverageWatts { get; set; }

    [Name("icu_normalized_watts")]
    public double? NormalizedWatts { get; set; }

    [Name("average_heartrate")]
    public double? AverageHeartRate { get; set; }

    [Name("icu_fitness")]
    public double? Fitness { get; set; }

    [Name("icu_fatigue")]
    public double? Fatigue { get; set; }
    
    [Name("total_elevation_gain")]
    public double? TotalElevationGain { get; set; }
}
