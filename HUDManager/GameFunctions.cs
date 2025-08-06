using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Runtime.InteropServices;

namespace HUDManager;

public class GameFunctions
{
    private delegate byte UpdateAddonPositionDelegate(IntPtr raptureAtkUnitManager, IntPtr addon, byte clicked);
    private readonly UpdateAddonPositionDelegate _updateAddonPosition;

    private Plugin Plugin { get; }

    public GameFunctions(Plugin plugin)
    {
        Plugin = plugin;

        var updatePositionPtr = Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 8B 8B ?? ?? ?? ?? 33 D2 48 8B 01 FF 90 ?? ?? ?? ??");
        _updateAddonPosition = Marshal.GetDelegateForFunctionPointer<UpdateAddonPositionDelegate>(updatePositionPtr);
    }

    public unsafe void SetAddonPosition(string uiName, short x, short y)
    {
        var addon = Plugin.GameGui.GetAddonByName(uiName);
        if (addon == IntPtr.Zero) {
            return;
        }

        var addonPtr = (AtkUnitBase*)addon.Address;

        var baseUi = Plugin.GameGui.GetUIModule().Address;
        var manager = Marshal.ReadIntPtr(baseUi + 0x20);

        _updateAddonPosition(
            manager,
            addon,
            1
        );
        addonPtr->SetPosition(x, y);
        _updateAddonPosition(
            manager,
            addon,
            0
        );
    }

    public Vector2<short>? GetAddonPosition(string uiName)
    {
        var addon = Plugin.GameGui.GetAtkUnitByName(uiName, 1);
        if (addon == null) {
            return null;
        }

        return new Vector2<short>(addon.Value.X, addon.Value.Y);
    }
}
