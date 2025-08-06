using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;

namespace HUDManager;

internal static class GameGuiExt
{
    public static AtkUnitBase? GetAtkUnitByName(this IGameGui gameGui, string name, int index)
    {
        var addon = gameGui.GetAddonByName(name, index);
        if (addon == IntPtr.Zero) {
            return null;
        }

        unsafe {
            return *(AtkUnitBase*)addon.Address;
        }
    }
}
