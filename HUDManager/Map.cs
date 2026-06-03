using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HUDManager;

internal class Map
{
    private static Lumina.Excel.ExcelSheet<Lumina.Excel.Sheets.Map>? _sheet;
    private static Lumina.Excel.ExcelSheet<Lumina.Excel.Sheets.Map> GetSheet(IDataManager data)
    {
        return _sheet ??= data.GetExcelSheet<Lumina.Excel.Sheets.Map>()!;
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
            .Where(IsCandidateMap)
            .DistinctBy(map => map.PlaceName.Value!.Name.ToString())
            .Select(map => new Map(map.PlaceName.Value!.Name.ExtractText(), map.RowId))
            .ToList();

    private static bool IsCandidateMap(Lumina.Excel.Sheets.Map map) {
        if (map.RowId == 0)
            return false;

        var placeName = map.PlaceName.Value.Name.ToString();
        if (string.IsNullOrWhiteSpace(placeName))
            return false;

        var placeNameSub = map.PlaceNameSub.Value.Name.ToString();
        if (string.IsNullOrWhiteSpace(placeNameSub))
            return true;
        if (placeName == placeNameSub)
            return true;

        return false;
    }

    public static uint? GetRootZoneId(IDataManager data, uint territoryType)
    {
        var territorySheet = data.GetExcelSheet<Lumina.Excel.Sheets.TerritoryType>();
        if (territorySheet.FirstOrNull(t => t.RowId == territoryType) is not { } territory) {
            // territoryType can be 0 when Dalamud is loaded mid-game
            return null;
        }
        try {
            return GetSheet(data).First(map => map.PlaceName.RowId == territory.PlaceName.RowId).RowId;
        } catch (InvalidOperationException) {
            return null;
        }
    }
}
