using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using DomestiqAvalonia.Models;

namespace DomestiqAvalonia.Services;

public class CsvImportService
{
    public List<ActivityRecord> ImportActivities(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return new List<ActivityRecord>();
        }

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null
        };

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, config);
        
        return csv.GetRecords<ActivityRecord>().ToList();
    }
}
