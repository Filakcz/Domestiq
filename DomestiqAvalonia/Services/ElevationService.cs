using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DomestiqAvalonia.Services;

public class ElevationService
{
    private readonly string _hgtFolder;
    private const int Arc1Size = 3601;

    // cache
    private readonly Dictionary<string, byte[]> _tileCache = new();
    private readonly LinkedList<string> _lruList = new();
    private const int MaxCachedTiles = 24;

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

            byte[]? fileData;
            if (_tileCache.TryGetValue(filePath, out fileData))
            {
                _lruList.Remove(filePath);
                _lruList.AddFirst(filePath);
            }
            else
            {
                fileData = File.ReadAllBytes(filePath);
                
                if (_tileCache.Count >= MaxCachedTiles)
                {
                    var last = _lruList.Last;
                    if (last != null)
                    {
                        _tileCache.Remove(last.Value);
                        _lruList.RemoveLast();
                    }
                }

                _tileCache[filePath] = fileData;
                _lruList.AddFirst(filePath);
            }

            int r = (int)Math.Round((latFloor + 1 - lat) * (Arc1Size - 1));
            int c = (int)Math.Round((lon - lonFloor) * (Arc1Size - 1));

            // 1 bod = 2 bajty (256 * 1. bajt) + 2. bajt
            int pos = (r * Arc1Size + c) * 2;
            
            if (pos < 0 || pos > fileData.Length - 2)
            {
                return 0;
            }

            short elevation = (short)((fileData[pos] << 8) | fileData[pos + 1]);

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
