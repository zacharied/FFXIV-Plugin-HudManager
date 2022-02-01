using Dalamud.Data;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;

namespace HUD_Manager {
    public static class Util {

        public static bool ContainsIgnoreCase(this string haystack, string needle) {
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
            unsafe
            {
                var player = FFXIVClientStructs.FFXIV.Client.Game.UI.UIState.Instance()->PlayerState;
                return player.ClassJobLevelArray[classJob.ExpArrayIndex] > 0;
            }
        }

        public static bool PetHotbarActive()
        {
            // Updated 6.08
            var offsetDismount = 0x10650;
            var offsetFirstSlot = 0xFCB0;

            IntPtr hotbarsAddress;
            unsafe {
                hotbarsAddress = (IntPtr)Framework.Instance()->GetUiModule()->GetRaptureHotbarModule();
            }
            return (Marshal.ReadByte(hotbarsAddress + offsetDismount) & 1) == 0 
                || (Marshal.ReadByte(hotbarsAddress + offsetFirstSlot) & 1) == 0;
        }
    }
}
