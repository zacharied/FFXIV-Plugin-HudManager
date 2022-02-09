using Dalamud.Game;
using Dalamud.Logging;
using HUD_Manager;
using System;
using System.Runtime.InteropServices;
using ClientFramework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;

/// <summary>
/// Manages keeping the state of the pet hotbar when HUD changes are made.
/// 
/// It's a little complicated. Basically, in the Character Configuration window, there is a setting to change how the pet
/// hotbar is displayed, either reflecting the actions on hotbar 1 when it appears, or revealing its own hotbar (the "Pet Hotbar"
/// HUD element). Pet hotbar isn't really used for pets anymore, but rather any mount/state change that gives the player new
/// actions. This includes beast tribe mount quests like the Vanu Sanuwa quest, role-playing scenes where you play as an NPC,
/// and vehicles like A4.
/// 
/// The game suffers from a slight bug in which changing the HUD will cause your hotbar 1 to discard the "overlain" pet hotbar
/// and revert to its original set of actions. This is problematic since entering a state in which the pet hotbar is shown
/// can cause a HUD swap depending on the user's configuration, which will lead to the end result of the pet hotbar
/// being thrown out immediately whenever it would appear on screen.
/// 
/// This manager tracks the state of the pet hotbar in memory, and automatically calls the function that populates the pet hotbars
/// whenever a HUD change is made.
/// </summary>
public class PetHotbar : IDisposable
{
    private delegate void PreparePetHotbarResetDelegate();
    private delegate void PerformPetHotbarResetDelegate(IntPtr uiModule, byte playSound);
    private delegate long GetPetHotbarManagerThing(IntPtr thingPointer);

    private readonly IntPtr hotbarModuleAddress;
    private readonly IntPtr uiModuleAddress;

    private readonly Plugin plugin;

    private readonly PreparePetHotbarResetDelegate prepareReset;
    private readonly PerformPetHotbarResetDelegate performReset;
    private readonly GetPetHotbarManagerThing getManagerThing;

    private bool resetInProgress = false;
    private bool prepared = false;

    public PetHotbar(Plugin plugin)
    {
        this.plugin = plugin;

        unsafe {
            var uiModule = ClientFramework.Instance()->GetUiModule();
            this.uiModuleAddress = (IntPtr)uiModule;
            this.hotbarModuleAddress = (IntPtr)uiModule->GetRaptureHotbarModule();
        }

        var preparePetHotbarChangePtr = plugin.SigScanner.ScanText("48 83 EC 28 48 8B 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 85 C0 74 1F 48 8B 10 48 8B C8");
        var performPetHotbarChangePtr = plugin.SigScanner.ScanText("40 53 48 83 EC 20 83 B9 70 19 01 00 00 48 8B D9 75 14");
        if (preparePetHotbarChangePtr == IntPtr.Zero || performPetHotbarChangePtr == IntPtr.Zero) {
            PluginLog.Error("PetHotbar: unable to find one or more signatures. Pet hotbar functionality will be disabled.\n" +
                           $"prepare: {preparePetHotbarChangePtr}\nperform: {performPetHotbarChangePtr}");
            return;
        }

        this.prepareReset = Marshal.GetDelegateForFunctionPointer<PreparePetHotbarResetDelegate>(preparePetHotbarChangePtr);
        this.performReset = Marshal.GetDelegateForFunctionPointer<PerformPetHotbarResetDelegate>(performPetHotbarChangePtr);

        unsafe {
            // 0x14079c16b: CALL qword ptr [RDX + <offset>]
            // Updated 6.08
            const int vtblOffset = 0x68;

            // We want to run the "perform change" function from the game, but the function we hooked here is normally wrapped
            //  by an outer function to perform setup first. We want to disable sounds so we still must run this internal function,
            //  so this code here is just replicating what the outer function would do.

            // Double dereference to acquire the function address from the table.
            var fnPointer = *(long*)uiModuleAddress + vtblOffset;
            this.getManagerThing = Marshal.GetDelegateForFunctionPointer<GetPetHotbarManagerThing>((IntPtr)(*(long*)fnPointer));
        }

        this.plugin.Framework.Update += CheckResetLoop;
    }

    public void ResetPetHotbar()
    {
        if (resetInProgress)
            return;

        resetInProgress = true;
        prepared = false;
    }

    private void CheckResetLoop(Framework _)
    {
        if (resetInProgress) {
            if (!prepared && PetHotbarActive()) {
                this.prepareReset.Invoke();
                prepared = true;
                return;
            }

            if (prepared) {
                this.PerformReset(false);
                resetInProgress = false;
                prepared = false;
            } else {
                resetInProgress = false;
            }
        }
    }

    private void PerformReset(bool playSound)
    {
        var managerThing = this.getManagerThing.Invoke(uiModuleAddress);
        this.performReset.Invoke((IntPtr)managerThing, (byte)(playSound ? 1 : 0));
    }

    public bool PetHotbarActive()
    {
        // 0x140633216: CMP dword ptr [RCX + <offset>],0x0
        // Updated 6.08
        const int offset = 0x11970;

        return Marshal.ReadByte(hotbarModuleAddress + offset) == 0xE;
    }

    public void Dispose()
    {
        this.plugin.Framework.Update -= CheckResetLoop;
    }
}