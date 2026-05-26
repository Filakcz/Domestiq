using System;
using System.IO;

namespace DomestiqAvalonia.Services;

public class ElevationService
{
    private readonly string _hgtFolder;
    private const int Arc1Size = 3601;

    private string? _lastFilePath;
    private byte[]? _lastFileData;

    public ElevationService(string hgtFolder)
    {
        _hgtFolder = hgtFolder;
    }

    public short GetElevation(double lat, double lon)
    {
        try
        {
            int latFloor = (int)Math.Floor(lat);
            int lonFloor = (int)Math.Floor(lon);

            // N49E015.hgt
            string filePath = Path.Combine(_hgtFolder, $"N{latFloor:D2}E{lonFloor:D3}.hgt");

            if (!File.Exists(filePath))
            {
                return 0;
            }

            if (_lastFilePath != filePath)
            {
                _lastFileData = File.ReadAllBytes(filePath);
                _lastFilePath = filePath;
            }

            if (_lastFileData == null)
            {
                return 0;
            }

            int r = (int)Math.Round((latFloor + 1 - lat) * (Arc1Size - 1));
            int c = (int)Math.Round((lon - lonFloor) * (Arc1Size - 1));

            // 1 bod = 2 bajty (256 * 1. bajt) + 2. bajt
            int pos = (r * Arc1Size + c) * 2;
            
            if (pos < 0 || pos > _lastFileData.Length - 2)
            {
                return 0;
            }

            short elevation = (short)((_lastFileData[pos] << 8) | _lastFileData[pos + 1]);

            if (elevation == -32768)
            {
                return 0;
            }

            return elevation;
        }
        catch
        {
            return 0;
        }
    }
}
