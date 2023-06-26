using Dalamud.Data;
using Dalamud.Logging;
using Lumina.Excel.GeneratedSheets;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

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
            return plugin.GameConfig.UiConfig.TryGet("PadMode", out bool isPadMode) && isPadMode;
        }

        public static bool FullScreen(Plugin plugin) // treats Borderless as Full Screen
        {
            return plugin.GameConfig.System.TryGet("ScreenMode", out uint mode) && mode > 0;
        }

        public static bool IsCharacterConfigOpen()
        {
            unsafe {
                var agent = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUiModule()->GetAgentModule()->
                    GetAgentByInternalId(AgentId.ConfigCharacter);
                return agent->IsAgentActive();
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
