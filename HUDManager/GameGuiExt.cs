using Dalamud.Game.Gui;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;

namespace HUD_Manager
{
    internal static class GameGuiExt
    {
        public class NoAtkUnitFoundException : Exception
        {
            public NoAtkUnitFoundException(string? message) : base(message) { }
        }

        public static AtkUnitBase? GetAtkUnitByName(this GameGui gameGui, string name, int index)
        {
            var addon = gameGui.GetAddonByName(name, index);
            if (addon == IntPtr.Zero) {
                return null;
            }

            unsafe {
                return *(AtkUnitBase*)addon;
            }
        }
    }
}
