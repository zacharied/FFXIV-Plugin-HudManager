using Dalamud.Data;
using Lumina.Excel.GeneratedSheets;
using System.Globalization;
using System.Linq;

namespace HUD_Manager
{
    public static class Util
    {

        public static bool ContainsIgnoreCase(this string haystack, string needle)
        {
            return CultureInfo.InvariantCulture.CompareInfo.IndexOf(haystack, needle, CompareOptions.IgnoreCase) >= 0;
        }

        public static ClassJob? FindClassJobByAbbreviation(string abbr, DataManager data)
        {
            // TODO Cache these requests or something...
            var sheet = data.GetExcelSheet<ClassJob>()!;
            return sheet.Where(job => job.Abbreviation == abbr).FirstOrDefault();
        }

        public static bool HasUnlockedClass(Plugin plugin, ClassJob classJob)
        {
            var classJobCount = plugin.DataManager.GetExcelSheet<ClassJob>()!.RowCount;
            unsafe {
                var player = FFXIVClientStructs.FFXIV.Client.Game.UI.UIState.Instance()->PlayerState;
                return player.ClassJobLevelArray[classJob.ExpArrayIndex] > 0;
            }
        }
    }
}
