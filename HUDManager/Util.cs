using Dalamud.Data;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel.GeneratedSheets;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace HUD_Manager
{
    public static class Util
    {
        static Util()
        {
            EnglishAbbreviationToJobId = new();
            foreach (var (k, v) in JobIdToEnglishAbbreviation) {
                EnglishAbbreviationToJobId[v] = k;
            }
        }

        public static bool ContainsIgnoreCase(this string haystack, string needle)
        {
            return CultureInfo.InvariantCulture.CompareInfo.IndexOf(haystack, needle, CompareOptions.IgnoreCase) >= 0;
        }

        public static bool HasUnlockedClass(Plugin plugin, ClassJob classJob)
        {
            var classJobCount = plugin.DataManager.GetExcelSheet<ClassJob>()!.RowCount;
            unsafe {
                var player = FFXIVClientStructs.FFXIV.Client.Game.UI.UIState.Instance()->PlayerState;
                return player.ClassJobLevelArray[classJob.ExpArrayIndex] > 0;
            }
        }

        public static bool GamepadModeActive(Plugin plugin)
        {
            unsafe {
                var configModule = ConfigModule.Instance();
                var option = configModule->GetValueById((short)ConfigOption.PadMode);
                return (option->Byte & 1) > 0;
            }
        }

        public readonly static Dictionary<uint, string> JobIdToEnglishAbbreviation = new Dictionary<uint, string>()
        {
            [0] = "ADV",
            [1] = "GLA",
            [2] = "PGL",
            [3] = "MRD",
            [4] = "LNC",
            [5] = "ARC",
            [6] = "CNJ",
            [7] = "THM",
            [8] = "CRP",
            [9] = "BSM",
            [10] = "ARM",
            [11] = "GSM",
            [12] = "LTW",
            [13] = "WVR",
            [14] = "ALC",
            [15] = "CUL",
            [16] = "MIN",
            [17] = "BTN",
            [18] = "FSH",
            [19] = "PLD",
            [20] = "MNK",
            [21] = "WAR",
            [22] = "DRG",
            [23] = "BRD",
            [24] = "WHM",
            [25] = "BLM",
            [26] = "ACN",
            [27] = "SMN",
            [28] = "SCH",
            [29] = "ROG",
            [30] = "NIN",
            [31] = "MCH",
            [32] = "DRK",
            [33] = "AST",
            [34] = "SAM",
            [35] = "RDM",
            [36] = "BLU",
            [37] = "GNB",
            [38] = "DNC",
            [39] = "RPR",
            [40] = "SGE",
        };
        public readonly static Dictionary<string, uint> EnglishAbbreviationToJobId;
    }
}
