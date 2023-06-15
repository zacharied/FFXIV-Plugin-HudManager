using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using HUD_Manager.Structs;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace HUD_Manager.Ui
{
#if DEBUG
    public class Debug
    {
        private Plugin Plugin { get; }

        private Layout? PreviousLayout { get; set; }

        private (bool drawUnknownIds, bool _) Ui = (false, false);

        public Debug(Plugin plugin)
        {
            this.Plugin = plugin;
        }

        internal void Draw()
        {
            if (!ImGui.BeginTabItem("Debug")) {
                return;
            }

            ImGui.TextUnformatted("Print layout pointer address");

            if (ImGui.Button("1")) {
                var ptr = this.Plugin.Hud.GetLayoutPointer(HudSlot.One);
                this.Plugin.ChatGui.Print($"{ptr.ToInt64():x}");
            }

            ImGui.SameLine();

            if (ImGui.Button("2")) {
                var ptr = this.Plugin.Hud.GetLayoutPointer(HudSlot.Two);
                this.Plugin.ChatGui.Print($"{ptr.ToInt64():x}");
            }

            ImGui.SameLine();

            if (ImGui.Button("3")) {
                var ptr = this.Plugin.Hud.GetLayoutPointer(HudSlot.Three);
                this.Plugin.ChatGui.Print($"{ptr.ToInt64():x}");
            }

            ImGui.SameLine();

            if (ImGui.Button("4")) {
                var ptr = this.Plugin.Hud.GetLayoutPointer(HudSlot.Four);
                this.Plugin.ChatGui.Print($"{ptr.ToInt64():x}");
            }

            ImGui.SameLine();

            if (ImGui.Button("Default")) {
                var ptr = this.Plugin.Hud.GetDefaultLayoutPointer();
                this.Plugin.ChatGui.Print($"{ptr.ToInt64():x}");
            }

            if (ImGui.Button("File pointer 0")) {
                var ptr = this.Plugin.Hud.GetFilePointer(0);
                this.Plugin.ChatGui.Print($"{ptr.ToInt64():x}");
            }

            if (ImGui.Button("Data pointer")) {
                var ptr = this.Plugin.Hud.GetDataPointer();
                this.Plugin.ChatGui.Print($"{ptr.ToInt64():x}");
            }

            if (ImGui.Button("Save layout")) {
                var ptr = this.Plugin.Hud.GetLayoutPointer(HudSlot.One);
                var layout = Marshal.PtrToStructure<Layout>(ptr);
                this.PreviousLayout = layout;
            }

            var unknowns = GetUnknownElements();
            if (ImGui.Button("Find unknown IDs")) {
                foreach (var v in unknowns) {
                    PluginLog.Log($"Unknown ID: {v.id}");
                }
            }

            ImGui.SameLine();

            ImGui.Checkbox("Draw unknown ID labels", ref Ui.drawUnknownIds);
            if (Ui.drawUnknownIds) {
                DrawUnknownIdElements(unknowns);
            }

            if (ImGui.Button("Print layout")) {
                var layout = this.Plugin.Hud.ReadLayout(Plugin.Hud.GetActiveHudSlot());
                foreach (var e in layout.elements) {
                    PluginLog.Log($"{e.id}, {e.x}");
                }
            }

            if (ImGui.Button("Find difference") && this.PreviousLayout != null) {
                var ptr = this.Plugin.Hud.GetLayoutPointer(HudSlot.One);
                var layout = Marshal.PtrToStructure<Layout>(ptr);

                foreach (var prevElem in this.PreviousLayout.Value.elements) {
                    var currElem = layout.elements.FirstOrDefault(el => el.id == prevElem.id);
                    if (currElem.visibility == prevElem.visibility && !(Math.Abs(currElem.x - prevElem.x) > .01)) {
                        continue;
                    }

                    PluginLog.Log(currElem.id.ToString());
                    this.Plugin.ChatGui.Print(currElem.id.ToString());
                }
            }

            if (ImGui.Button("Print current slot")) {
                var slot = this.Plugin.Hud.GetActiveHudSlot();
                this.Plugin.ChatGui.Print($"{slot}");
            }

            if (ImGui.Button("Print player status address")) {
                this.Plugin.ChatGui.Print($"{this.Plugin.ClientState.LocalPlayer:x}");
            }

            if (ImGui.Button("Print Config")) {
                unsafe {
                    this.Plugin.ChatGui.Print($"{(IntPtr)Framework.Instance()->SystemConfig.CommonSystemConfig.ConfigBase.ConfigEntry:x}");
                }
            }

            if (ImGui.Button("FATE Status")) {
                ////PluginLog.Log($"{this.Plugin.Statuses.IsInFate(this.Plugin.ClientState.LocalPlayer)}");
                PluginLog.Log($"{this.Plugin.Statuses.IsLevelSynced()}");

            }

            if (ImGui.Button("Print ClassJob dict values")) {
                string s = "";
                foreach (var row in Plugin.DataManager.GetExcelSheet<ClassJob>()!)
                    s += $"[{row.RowId}] = \"{row.Abbreviation}\",\n";
                Plugin.ChatGui.Print(s);
            }            

            ImGui.EndTabItem();
        }

        private List<RawElement> GetUnknownElements()
        {
            var items = new List<RawElement>();

            foreach (var hudSlot in Enum.GetValues<HudSlot>()) {
                var ptr = Plugin.Hud.GetLayoutPointer(hudSlot);
                for (int i = 0; i < 92; i++) {
                    var idPtr = (ptr + i * Marshal.SizeOf<RawElement>()) + 0;
                    var id = Marshal.ReadInt32(idPtr);
                    if (id == 0 || items.Exists(r => (uint)r.id == (uint)id))
                        continue;
                    items.Add(Marshal.PtrToStructure<RawElement>(idPtr));
                }
            }

            return items.Where(e => !Enum.IsDefined(e.id)).ToList();
        }

        private void DrawUnknownIdElements(IEnumerable<RawElement> list)
        {
            const ImGuiWindowFlags flags = ImGuiWindowFlags.NoTitleBar
                               | ImGuiWindowFlags.NoResize
                               | ImGuiWindowFlags.NoFocusOnAppearing
                               | ImGuiWindowFlags.NoScrollbar
                               | ImGuiWindowFlags.NoMove;

            foreach (var raw in list) {
                var element = new Element(raw);
                var (pos, size) = ImGuiExt.ConvertGameToImGui(element);
                ImGui.SetNextWindowPos(pos, ImGuiCond.Appearing);

                ImGui.SetNextWindowSize(size);

                if (!ImGui.Begin($"##uimanager-preview-{element.Id}", flags)) {
                    continue;
                }

                ImGui.TextUnformatted(element.Id.LocalisedName(this.Plugin.DataManager));

                ImGui.End();
            }
        }
    }
#endif
}
