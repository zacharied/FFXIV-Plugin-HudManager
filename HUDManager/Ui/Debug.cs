using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HUDManager.Structs;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace HUDManager.Ui;

public class Debug
{
    private Plugin Plugin { get; }

    private Layout? PreviousLayout { get; set; }

    private (bool drawUnknownIds, bool _) _ui = (false, false);

    public Debug(Plugin plugin)
    {
        Plugin = plugin;
    }

    internal void Draw() {
        using var tab = ImRaii.TabItem("Debug");
        if (!tab) return;

        ImGui.TextUnformatted("Print layout pointer address");

        if (ImGui.Button("1##ptr1")) {
            var ptr = Hud.GetLayoutPointer(HudSlot.One);
            Plugin.ChatGui.Print($"{ptr.ToInt64():X}");
        }

        ImGui.SameLine();

        if (ImGui.Button("2##ptr2")) {
            var ptr = Hud.GetLayoutPointer(HudSlot.Two);
            Plugin.ChatGui.Print($"{ptr.ToInt64():X}");
        }

        ImGui.SameLine();

        if (ImGui.Button("3##ptr3")) {
            var ptr = Hud.GetLayoutPointer(HudSlot.Three);
            Plugin.ChatGui.Print($"{ptr.ToInt64():X}");
        }

        ImGui.SameLine();

        if (ImGui.Button("4##ptr4")) {
            var ptr = Hud.GetLayoutPointer(HudSlot.Four);
            Plugin.ChatGui.Print($"{ptr.ToInt64():X}");
        }

        ImGui.TextUnformatted("Log layout to console");
        void LogLayout(HudSlot slot)
        {
            var layout = Hud.ReadLayout(slot);
            Plugin.Log.Information($"===== Layout START (slot={slot}) =====");
            for (var i = 0; i < layout.elements.Length; i++) {
                Plugin.Log.Information($"  i={i:000} {layout.elements[i]}");
            }
            Plugin.Log.Information("===== Layout END =====");
        }

        if (ImGui.Button("1##print1")) {
            LogLayout(HudSlot.One);
        }
        ImGui.SameLine();
        if (ImGui.Button("2##print2")) {
            LogLayout(HudSlot.Two);
        }
        ImGui.SameLine();
        if (ImGui.Button("3##print3")) {
            LogLayout(HudSlot.Three);
        }
        ImGui.SameLine();
        if (ImGui.Button("4##print4")) {
            LogLayout(HudSlot.Four);
        }

        if (ImGui.Button("Data pointer")) {
            var ptr = Hud.GetDataPointer();
            Plugin.ChatGui.Print($"{ptr.ToInt64():X}");
        }

        if (ImGui.Button("CS Addon Config")) {
            unsafe {
                var ptr = AddonConfig.Instance();
                Plugin.ChatGui.Print($"{(nint)ptr:X}");
            }
        }

        if (ImGui.Button("Save layout")) {
            var ptr = Hud.GetLayoutPointer(Hud.GetActiveHudSlot());
            var layout = Marshal.PtrToStructure<Layout>(ptr);
            PreviousLayout = layout;
        }

        if (ImGui.Button("Find unknown IDs")) {
            foreach (var v in GetUnknownElements()) {
                Plugin.Log.Information($"Unknown ID: {v.id}");
            }
        }

        ImGui.SameLine();

        ImGui.Checkbox("Draw unknown ID labels", ref _ui.drawUnknownIds);
        if (_ui.drawUnknownIds) {
            DrawUnknownIdElements();
        }

        if (ImGui.Button("Find difference") && PreviousLayout != null) {
            var ptr = Hud.GetLayoutPointer(Hud.GetActiveHudSlot());
            var layout = Marshal.PtrToStructure<Layout>(ptr);

            foreach (var prevElem in PreviousLayout.Value.elements) {
                var currElem = layout.elements.FirstOrDefault(el => el.id == prevElem.id);
                if (currElem.visibility == prevElem.visibility && !(Math.Abs(currElem.x - prevElem.x) > .01)) {
                    continue;
                }

                Plugin.Log.Information(currElem.id.ToString());
                Plugin.ChatGui.Print(currElem.id.ToString());
            }
        }

        if (ImGui.Button("Print current slot")) {
            var slot = Hud.GetActiveHudSlot();
            Plugin.ChatGui.Print($"{slot} ({(int)slot})");
        }

        if (ImGui.Button("Print player status address")) {
            Plugin.ChatGui.Print($"{Plugin.ObjectTable.LocalPlayer:X}");
        }

        if (ImGui.Button("Print Config")) {
            unsafe {
                Plugin.ChatGui.Print($"{(IntPtr)Framework.Instance()->SystemConfig.SystemConfigBase.ConfigBase.ConfigEntry:X}");
            }
        }

        if (ImGui.Button("FATE Status")) {
            Plugin.Log.Information($"Level: {Plugin.ObjectTable.LocalPlayer?.Level}");
            Plugin.Log.Information($"IsInFate: {Statuses.IsInFate()}");
            Plugin.Log.Information($"IsLevelSynced: {Statuses.IsLevelSynced()}");
        }

        if (ImGui.Button("Print ClassJob dict values")) {
            var s = "";
            foreach (var row in Plugin.DataManager.GetExcelSheet<ClassJob>())
                s += $"[{row.RowId}] = \"{row.Abbreviation}\",\n";
            Plugin.ChatGui.Print(s);
        }

        ImGui.Text($"Active Hud Slot: {Hud.GetActiveHudSlot()}");
        ImGui.Text($"Active Hud Slot (CS): {Hud.GetActiveHudSlotCs()}");
        ImGui.Text($"Active Hud Slot (Manual): {Hud.GetActiveHudSlotManual()}");
    }

    private static List<RawElement> GetUnknownElements()
    {
        var items = new List<RawElement>();

        foreach (var hudSlot in Enum.GetValues<HudSlot>()) {
            var ptr = Hud.GetLayoutPointer(hudSlot);
            for (var i = 0; i < 92; i++) {
                var idPtr = (ptr + i * Marshal.SizeOf<RawElement>()) + 0;
                var id = Marshal.ReadInt32(idPtr);
                if (id == 0 || items.Exists(r => (uint)r.id == (uint)id))
                    continue;
                items.Add(Marshal.PtrToStructure<RawElement>(idPtr));
            }
        }

        return items.Where(e => !Enum.IsDefined(e.id)).ToList();
    }

    private void DrawUnknownIdElements() {
        var drawList = ImGui.GetForegroundDrawList();

        foreach (var raw in GetUnknownElements()) {
            var element = new Element(raw);
            var preview = PreviewUtils.CreatePreviewData(Plugin.DataManager, element);

            preview.DebugDrawAll();
            drawList.AddText(preview.ActiveRect.Position, ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 1f)), element.Id.LocalisedName(Plugin.DataManager) );
        }
    }
}
