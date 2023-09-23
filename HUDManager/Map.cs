using Dalamud.Data;
using Dalamud.Logging;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HUDManager
{
    internal class Map
    {
        private static Lumina.Excel.ExcelSheet<Lumina.Excel.GeneratedSheets.Map> _sheet = null!;
        private static Lumina.Excel.ExcelSheet<Lumina.Excel.GeneratedSheets.Map> GetSheet(IDataManager data)
        {
            if (_sheet is null)
                _sheet = data.GetExcelSheet<Lumina.Excel.GeneratedSheets.Map>()!;
            return _sheet;
        }

        public string Name { get; private set; }
        public uint RowId { get; private set; }

        public Map(string name, uint rowId)
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
            var territory = territorySheet.First(t => t.RowId == territoryType);
            try {
                return GetSheet(data).Where(map => map.PlaceName.RawRow!.RowId == territory.PlaceName.RawRow!.RowId).First().RowId;
            } catch (InvalidOperationException) {
                return null;
            }
        }
    }
}
