using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HUDManager;

internal class Map
{
    private static Lumina.Excel.ExcelSheet<Lumina.Excel.GeneratedSheets.Map>? _sheet;
    private static Lumina.Excel.ExcelSheet<Lumina.Excel.GeneratedSheets.Map> GetSheet(IDataManager data)
    {
        return _sheet ??= data.GetExcelSheet<Lumina.Excel.GeneratedSheets.Map>()!;
    }

    public string Name { get; private set; }
    public uint RowId { get; private set; }

    private Map(string name, uint rowId)
    {
        Name = name;
        RowId = rowId;
    }

    public static List<Map> GetZoneMaps(IDataManager data)
        => GetSheet(data)
            .Where(map => string.IsNullOrWhiteSpace(map.PlaceNameSub.Value!.Name.ToString()))
            .Where(map => !string.IsNullOrWhiteSpace(map.PlaceName.Value!.ToString()))
            .DistinctBy(map => map.PlaceName.Value!.Name.ToString())
            .Select(map => new Map(map.PlaceName.Value!.Name, map.RowId))
            .Skip(1)
            .ToList();

    public static uint? GetRootZoneId(IDataManager data, uint territoryType)
    {
        var territorySheet = data.GetExcelSheet<Lumina.Excel.GeneratedSheets.TerritoryType>()!;
        var territory = territorySheet.FirstOrDefault(t => t.RowId == territoryType);
        if (territory == null) {
            // territoryType can be 0 when Dalamud is loaded mid-game
            return null;
        }
        try {
            return GetSheet(data).First(map => map.PlaceName.RawRow!.RowId == territory.PlaceName.RawRow!.RowId).RowId;
        } catch (InvalidOperationException) {
            return null;
        }
    }
}
