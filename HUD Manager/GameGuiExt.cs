using Dalamud.Game.Gui;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

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
            if (addon == IntPtr.Zero)
            {
                return null;
            }

            unsafe
            {
                return *(AtkUnitBase*)addon;
            }
        }
    }
}
