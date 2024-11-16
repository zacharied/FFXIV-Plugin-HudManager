using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace HUDManager;

public static class Util
{
    public static readonly Dictionary<string, uint> EnglishAbbreviationToJobId = new();

    static Util()
    {
        foreach (var (k, v) in JobIdToEnglishAbbreviation) {
            EnglishAbbreviationToJobId[v] = k;
        }
    }

    public static bool ContainsIgnoreCase(this string haystack, string needle)
    {
        return CultureInfo.InvariantCulture.CompareInfo.IndexOf(haystack, needle, CompareOptions.IgnoreCase) >= 0;
    }

    public static bool HasUnlockedClass(ClassJob classJob)
    {
        unsafe {
            var player = FFXIVClientStructs.FFXIV.Client.Game.UI.UIState.Instance()->PlayerState;
            return player.ClassJobLevels[classJob.ExpArrayIndex] > 0;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GamepadModeActive(Plugin plugin)
    {
        return plugin.GameConfig.UiConfig.TryGet("PadMode", out bool isPadMode) && isPadMode;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool FullScreen(Plugin plugin) // treats Borderless as Full Screen
    {
        return plugin.GameConfig.System.TryGet("ScreenMode", out uint mode) && mode > 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsCharacterConfigOpen()
    {
        unsafe {
            var agent = AgentModule.Instance()-> GetAgentByInternalId(AgentId.ConfigCharacter);
            return agent->IsAgentActive();
        }
    }

    public static uint GetPlayerJobId(Plugin plugin)
    {
        return plugin.ClientState.LocalPlayer?.ClassJob.RowId ?? uint.MaxValue;
    }

    private static readonly Dictionary<uint, string> JobIdToEnglishAbbreviation = new()
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
        [41] = "VIP",
        [42] = "PCT",
    };
}
