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
        try
        {
            if (this[CrossUpComponent.Split]) plugin.Interface.GetIpcSubscriber<(bool, int, int), bool>("CrossUp.SplitBar").InvokeAction(Split);
            if (this[CrossUpComponent.Padlock]) plugin.Interface.GetIpcSubscriber<(int, int, bool), bool>("CrossUp.Padlock").InvokeAction(Padlock);
            if (this[CrossUpComponent.SetNum]) plugin.Interface.GetIpcSubscriber<(int, int, bool), bool>("CrossUp.SetNumText").InvokeAction(SetNum);
            if (this[CrossUpComponent.ChangeSet]) plugin.Interface.GetIpcSubscriber<(int, int), bool>("CrossUp.ChangeSet").InvokeAction(ChangeSet);
            if (this[CrossUpComponent.TriggerText]) plugin.Interface.GetIpcSubscriber<bool, bool>("CrossUp.TriggerText").InvokeAction(!HideTriggerText);
            if (this[CrossUpComponent.Unassigned]) plugin.Interface.GetIpcSubscriber<bool, bool>("CrossUp.EmptySlots").InvokeAction(!HideUnassigned);
            if (this[CrossUpComponent.SelectBG]) plugin.Interface.GetIpcSubscriber<(int, int, Vector3), bool>("CrossUp.SelectBG").InvokeAction(SelectBG);
            if (this[CrossUpComponent.Buttons]) plugin.Interface.GetIpcSubscriber<(Vector3, Vector3), bool>("CrossUp.ButtonGlow").InvokeAction(Buttons);
            if (this[CrossUpComponent.Text]) plugin.Interface.GetIpcSubscriber<(Vector3, Vector3, Vector3), bool>("CrossUp.TextAndBorders").InvokeAction(Text);
            if (this[CrossUpComponent.SepEx]) plugin.Interface.GetIpcSubscriber<(bool, bool), bool>("CrossUp.ExBar").InvokeAction((SepEx, OnlyOneEx));
            if (this[CrossUpComponent.LRpos]) plugin.Interface.GetIpcSubscriber<(int, int), bool>("CrossUp.LRpos").InvokeAction(LRpos);
            if (this[CrossUpComponent.RLpos]) plugin.Interface.GetIpcSubscriber<(int, int), bool>("CrossUp.RLpos").InvokeAction(RLpos);
        }
        catch { PluginLog.LogWarning($"IPC with CrossUp failed. Is CrossUp installed?"); }
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