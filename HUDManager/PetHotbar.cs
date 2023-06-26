using System;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.Logging;
using ClientFramework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;

namespace HUD_Manager;

/// <summary>
/// Fixes the pet hotbar on occasions when it becomes "stuck" in PvP when hotbar 1 pet overlays are enabled.
///
/// This class actually works around a bug in the base game. When "Automatically replace hotbar 1 with pet hotbar when
/// mounted" is enabled, swapping HUD slots when mounted in PvP using a mount with mount actions causes the overlaid
/// per bar not to be fixed upon dismounting. This can be replicated without addons by mounting, swapping hud slots
/// manually, then dismounting. However, naturally HUD Manager makes this more likely by causing more swaps to happen.
///
/// To fix this, we detect a potential dismount in resetPetHotbarHook (which called when the game tries to restore the
/// hotbar upon dismounting). However, when this bug happens, the bar will not be restored as expected. To fix this, we
/// force-reenable the pet overlay on the following frame by calling setupPetHotbar (clearing the bugged state) then
/// follow up with yet another call to resetPetHotbarHook on the frame after that.
///
/// I don't know if it's possible to break this fix if an automated HUD swap would happen between these frames. If so,
/// we could block swaps while FixingPvpPetBar is set. However, it seems vanishingly unlikely to happen, and could
/// simply be fixed by remounting it if did.
/// </summary>
public class PetHotbar : IDisposable
{
    // Known values at HotbarPetTypeOffset:
    //   0x0E  Quest mount, e.g. Namazu Mikoshi quests
    //   0x12  Regular mount with actions, e.g. Logistics Node
    //   0x22  Unknown (found function but not found in game)
    // (Last updated in 6.4)
    private const int HotbarPetTypeOffset = 0x11970;

    private delegate void ResetPetHotbarDelegate();

    private delegate void SetupPetHotbarDelegate(IntPtr uiModule, uint value);

    private readonly Plugin plugin;

    private readonly IntPtr raptureHotbarModulePtr;
    private readonly IntPtr hotbarPetTypePtr;

    private readonly SetupPetHotbarDelegate setupPetHotbar;
    private readonly Hook<ResetPetHotbarDelegate> resetPetHotbarHook;

    private FixingPvpPetBar fixStage = FixingPvpPetBar.Off;
    private uint fixPetType;

    public PetHotbar(Plugin plugin)
    {
        this.plugin = plugin;

        unsafe {
            var uiModule = ClientFramework.Instance()->GetUiModule();
            raptureHotbarModulePtr = (IntPtr)uiModule->GetRaptureHotbarModule();
            hotbarPetTypePtr = raptureHotbarModulePtr + HotbarPetTypeOffset;
        }

        var resetPetHotbarPtr =
            plugin.SigScanner.ScanText(
                "48 83 EC 28 48 8B 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 85 C0 74 1F 48 8B 10 48 8B C8");
        var setupPetHotbarRealPtr = plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? EB 40 83 FD 01");

        if (setupPetHotbarRealPtr == IntPtr.Zero || resetPetHotbarPtr == IntPtr.Zero) {
            PluginLog.Error(
                "PetHotbar: unable to find one or more signatures. Pet hotbar functionality will be disabled.\n" +
                $"setup: {setupPetHotbarRealPtr}\nreset: 0x{resetPetHotbarPtr:X}");
            return;
        }

        setupPetHotbar = Marshal.GetDelegateForFunctionPointer<SetupPetHotbarDelegate>(setupPetHotbarRealPtr);
        resetPetHotbarHook = Hook<ResetPetHotbarDelegate>.FromAddress(resetPetHotbarPtr, ResetPetHotbarDetour);
        resetPetHotbarHook.Enable();

        this.plugin.Framework.Update += CheckFixLoop;
    }

    private void ResetPetHotbarDetour()
    {
        if (plugin.ClientState.IsPvP && fixStage == FixingPvpPetBar.Off) {
            var hotbarPetType = unchecked((uint)Marshal.ReadInt32(hotbarPetTypePtr));
            if (hotbarPetType > 0) {
                if (plugin.GameConfig.UiConfig.TryGet("ExHotbarChangeHotbar1", out bool isPetOverlayEnabled) && isPetOverlayEnabled) {
                    PluginLog.Debug("PetHotbarFix F0: Detected potentially broken pet hotbar overlay. Fixing...");
                    fixStage = FixingPvpPetBar.Setup;
                    fixPetType = hotbarPetType;
                }
            }
        }

        resetPetHotbarHook.Original();
    }

    private void CheckFixLoop(Framework _)
    {
        if (fixStage > FixingPvpPetBar.Off) {
            if (fixStage == FixingPvpPetBar.Setup) {
                PluginLog.Debug("PetHotbarFix F1: Setting hotbar back to mounted state");
                setupPetHotbar(raptureHotbarModulePtr, fixPetType);
                fixStage = FixingPvpPetBar.Reset;
                return;
            }

            PluginLog.Debug("PetHotbarFix F2: Resetting hotbar");
            resetPetHotbarHook.Original();
            fixStage++;
            fixStage = FixingPvpPetBar.Off;
        }
    }

    private enum FixingPvpPetBar
    {
        Off = 0,
        Setup = 1,
        Reset = 2
    }

    public void Dispose()
    {
        resetPetHotbarHook.Disable();
        resetPetHotbarHook.Dispose();
        plugin.Framework.Update -= CheckFixLoop;
    }
}