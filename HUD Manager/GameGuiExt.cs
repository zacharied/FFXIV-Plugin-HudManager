using Dalamud.Game.Gui;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HUD_Manager
{
    internal static class GameGuiExt
    {
        public static AtkUnitBase GetAtkUnitByName(this GameGui gameGui, string name, int index)
        {
            var addon = gameGui.GetAddonByName(name, index);
            if (addon == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Invalid addon name '{name}'");
            }

            unsafe
            {
                return *(AtkUnitBase*)addon;
            }
        }
    }
}
