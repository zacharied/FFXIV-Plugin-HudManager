using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface;
using HUD_Manager.Configuration;
using HUD_Manager.Structs;
using ImGuiNET;

namespace HUD_Manager.Ui.Editor.Tabs {
    public class Windows {
        private Plugin Plugin { get; }

        public Windows(Plugin plugin) {
            this.Plugin = plugin;
        }

        internal void Draw(SavedLayout layout) {
            if (ImGuiExt.IconButton(FontAwesomeIcon.Plus, "uimanager-add-window")) {
                ImGui.OpenPopup(Popups.AddWindow);
            }

            if (ImGui.BeginPopup(Popups.AddWindow)) {
                ImGui.TextUnformatted("Windows must be open to add them");
                ImGui.Separator();

                foreach (var window in WindowKindExt.All) {
                    var addon = this.Plugin.Interface.Framework.Gui.GetAddonByName(window, 1);
                    var flags = addon?.Visible == true && !layout.Windows.ContainsKey(window)
                        ? ImGuiSelectableFlags.None
                        : ImGuiSelectableFlags.Disabled;

                    if (!ImGui.Selectable(window, false, flags)) {
                        continue;
                    }

                    var pos = this.Plugin.GameFunctions.GetAddonPosition(window);
                    if (pos != null) {
                        layout.Windows.Add(window, new Window(pos));
                    }
                }

                ImGui.EndPopup();
            }

            if (!ImGui.BeginChild("uimanager-layout-editor-windows", new Vector2(0, 0))) {
                return;
            }

            var toRemove = new HashSet<string>();

            foreach (var entry in layout.Windows) {
                if (!ImGui.CollapsingHeader($"{entry.Key}##uimanager-window-{entry.Key}")) {
                    continue;
                }

                var pos = entry.Value.Position;

                var x = (int) pos.X;
                if (ImGui.InputInt($"X##uimanager-window-{entry.Key}", ref x)) {
                    pos.X = (short) x;
                    this.Plugin.GameFunctions.SetAddonPosition(entry.Key, pos.X, pos.Y);
                }

                ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X * 3);
                if (ImGuiExt.IconButton(FontAwesomeIcon.Trash, $"uimanager-remove-window-{entry.Key}")) {
                    toRemove.Add(entry.Key);
                }

                var y = (int) pos.Y;
                if (ImGui.InputInt($"Y##uimanager-window-{entry.Key}", ref y)) {
                    pos.Y = (short) y;
                    this.Plugin.GameFunctions.SetAddonPosition(entry.Key, pos.X, pos.Y);
                }
            }

            foreach (var remove in toRemove) {
                layout.Windows.Remove(remove);
            }

            ImGui.EndChild();
        }
    }
}
