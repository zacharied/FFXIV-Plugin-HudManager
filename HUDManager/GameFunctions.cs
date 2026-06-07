using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Runtime.InteropServices;

namespace HUDManager;

public class GameFunctions {
    private delegate byte UpdateAddonPositionDelegate(nint raptureAtkUnitManager, nint addon, byte clicked);
    private readonly UpdateAddonPositionDelegate updateAddonPosition;

    private Plugin Plugin { get; }

    public GameFunctions(Plugin plugin) {
        Plugin = plugin;

        var updatePositionPtr = Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 8B 8B ?? ?? ?? ?? 33 D2 48 8B 01 FF 90 ?? ?? ?? ??");
        updateAddonPosition = Marshal.GetDelegateForFunctionPointer<UpdateAddonPositionDelegate>(updatePositionPtr);
    }

    public unsafe void SetAddonPosition(string uiName, short x, short y) {
        var addonPtr = Plugin.GameGui.GetAddonByName(uiName);
        if (addonPtr.IsNull)
            return;

        var addon = (AtkUnitBase*)addonPtr.Address;
        var raptureAtkUnitManager = RaptureAtkUnitManager.Instance();

        updateAddonPosition((nint)raptureAtkUnitManager, addonPtr, 1);
        addon->SetPosition(x, y);
        updateAddonPosition((nint)raptureAtkUnitManager, addonPtr, 0);
    }

    public Vector2<short>? GetAddonPosition(string uiName) {
        var ptr = Plugin.GameGui.GetAddonByName(uiName);
        if (ptr.IsNull)
            return null;

        return new Vector2<short>(ptr.X, ptr.Y);
    }
}
