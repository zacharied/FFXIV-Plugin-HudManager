using System;
using System.Numerics;
using Dalamud.Logging;
using HUD_Manager;

namespace HUDManager.Structs.External;

[Serializable]
public class CrossUpConfig
{
    public CrossUpComponent Enabled { get; set; }

    public (bool on, int distance, int center) Split = (false, 100, 0);
    public (int x, int y, bool hide) Padlock = (0, 0, false);
    public (int x, int y, bool hide) SetNum = (0, 0, false);
    public (int x, int y) ChangeSet = (0, 0);
    public bool HideTriggerText;
    public bool HideUnassigned;
    public (int style, int blend, Vector3 color) SelectBG = (0, 0, new(1f / 2.55f));
    public (Vector3 glow, Vector3 pulse) Buttons = (new(1f), new(1f));
    public (Vector3 color, Vector3 glow, Vector3 border) Text = (new(1f), new(0.616f, 0.514f, 0.357f), new(1f));
    public bool SepEx;
    public bool OnlyOneEx;
    public (int x, int y) LRpos = (-214, -88);
    public (int x, int y) RLpos = (214, -88);

    public CrossUpConfig()
    {
    }

    public CrossUpConfig(
        CrossUpComponent enabled,
        (bool, int, int) split,
        (int, int, bool) padlock,
        (int, int, bool) setNum,
        (int, int) changeSet,
        bool hideTriggerText,
        bool hideUnassigned,
        (int, int, Vector3) selectBG,
        (Vector3, Vector3) buttons,
        (Vector3, Vector3, Vector3) text,
        bool sepEx,
        bool onlyOneEx,
        (int, int) lr,
        (int, int) rl)
    {
        Enabled = enabled;
        Split = split;
        Padlock = padlock;
        SetNum = setNum;
        ChangeSet = changeSet;
        HideTriggerText = hideTriggerText;
        HideUnassigned = hideUnassigned;
        SelectBG = selectBG;
        Buttons = buttons;
        Text = text;
        SepEx = sepEx;
        OnlyOneEx = onlyOneEx;
        LRpos = lr;
        RLpos = rl;
    }

    public void ApplyConfig(Plugin plugin)
    {
        static void Exec(string cmd, Plugin plugin)
        {
            PluginLog.Log($"CrossUp command: {cmd}");
            plugin.CommandManager.ProcessCommand(cmd);
        }

        static string FloatToHex(float f) => Convert.ToHexString(BitConverter.GetBytes((uint)(f * 255)))[..2];
        static string VecToHex(Vector3 color) => FloatToHex(color.X) + FloatToHex(color.Y) + FloatToHex(color.Z);

        try
        {
            if (this[CrossUpComponent.Split]) Exec($"/xup split {(Split.on ? "on" : "off")} {Split.distance} {Split.center}", plugin);
            if (this[CrossUpComponent.Padlock]) Exec($"/xup padlock {(Padlock.hide ? "off" : "on")} {Padlock.x} {Padlock.y}", plugin);
            if (this[CrossUpComponent.SetNum]) Exec($"/xup setnum {(SetNum.hide ? "off" : "on")} {SetNum.x} {SetNum.y}", plugin);
            if (this[CrossUpComponent.ChangeSet]) Exec($"/xup changeset {ChangeSet.x} {ChangeSet.y}", plugin);
            if (this[CrossUpComponent.TriggerText]) Exec($"/xup triggertext {(HideTriggerText ? "off" : "on")}", plugin);
            if (this[CrossUpComponent.Unassigned]) Exec($"/xup emptyslots {(HideUnassigned ? "off" : "on")}", plugin);
            if (this[CrossUpComponent.SelectBG]) Exec($"/xup selectbg {SelectBG.style} {SelectBG.blend} {VecToHex(SelectBG.color)}", plugin);
            if (this[CrossUpComponent.Buttons]) Exec($"/xup buttonglow {VecToHex(Buttons.glow)} {VecToHex(Buttons.pulse)}", plugin);
            if (this[CrossUpComponent.Text]) Exec($"/xup text {VecToHex(Text.color)} {VecToHex(Text.glow)} {VecToHex(Text.border)}", plugin);
            if (this[CrossUpComponent.SepEx])
            {
                Exec($"/xup exbar {(SepEx ? "on" : "off")}", plugin);
                Exec($"/xup onlyone {(OnlyOneEx ? "true" : "false")}", plugin);
            }
            if (this[CrossUpComponent.LRpos]) Exec($"/xup lrpos {LRpos.x} {LRpos.y}", plugin);
            if (this[CrossUpComponent.RLpos]) Exec($"/xup rlpos {RLpos.x} {RLpos.y}", plugin);
        }
        catch (Exception ex)
        {
            PluginLog.LogWarning($"Error applying CrossUp settings: {ex}");
        }
    }

    public bool this[CrossUpComponent component]
    {
        get => (Enabled & component) > 0;
        set
        {
            if (value) Enabled |= component;
            else Enabled &= ~component;
        }
    }

    public CrossUpConfig Clone() => new(Enabled, Split, Padlock, SetNum, ChangeSet, HideTriggerText, HideUnassigned,
        SelectBG, Buttons, Text, SepEx, OnlyOneEx, LRpos, RLpos);

    [Flags]
    public enum CrossUpComponent
    {
        Split = 1 << 0,
        Padlock = 1 << 1,
        SetNum = 1 << 2,
        ChangeSet = 1 << 3,
        TriggerText = 1 << 4,
        Unassigned = 1 << 5,
        SelectBG = 1 << 6,
        Buttons = 1 << 7,
        Text = 1 << 8,
        SepEx = 1 << 9,
        LRpos = 1 << 10,
        RLpos = 1 << 11,
    }
}