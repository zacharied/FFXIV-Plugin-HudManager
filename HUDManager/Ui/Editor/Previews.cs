using HUD_Manager.Structs;
using ImGuiNET;
using System;
using System.Collections.Generic;

namespace HUD_Manager.Ui.Editor
{
    public class Previews
    {
        private Plugin Plugin { get; }
        private Interface Ui { get; }

        internal HashSet<ElementKind> Elements { get; } = new();
        internal HashSet<ElementKind> Update { get; } = new();

        public Previews(Plugin plugin, Interface ui)
        {
            this.Plugin = plugin;
            this.Ui = ui;
        }

        public void Draw(ref bool update)
        {
            const float tolerance = 0.0001f;
            const ImGuiWindowFlags flags = ImGuiWindowFlags.NoTitleBar
                                           | ImGuiWindowFlags.NoResize
                                           | ImGuiWindowFlags.NoFocusOnAppearing
                                           | ImGuiWindowFlags.NoScrollbar;

            if (this.Ui.SelectedLayout == Guid.Empty) {
                return;
            }

            if (!this.Plugin.Config.Layouts.TryGetValue(this.Ui.SelectedLayout, out var layout)) {
                return;
            }

            foreach (var element in layout.Elements.Values) {
                if (!this.Elements.Contains(element.Id)) {
                    continue;
                }

                var (pos, size) = ImGuiExt.ConvertGameToImGui(element);
                if (this.Update.Remove(element.Id)) {
                    ImGui.SetNextWindowPos(pos);
                } else {
                    ImGui.SetNextWindowPos(pos, ImGuiCond.Appearing);
                }

                ImGui.SetNextWindowSize(size);

                if (!ImGui.Begin($"##uimanager-preview-{element.Id}", flags)) {
                    continue;
                }

                ImGui.TextUnformatted(element.Id.LocalisedName(this.Plugin.DataManager));

                // determine if the window has moved and update if it has
                var newPos = ImGuiExt.ConvertImGuiToGame(element, ImGui.GetWindowPos());
                if (Math.Abs(newPos.X - element.X) > tolerance || Math.Abs(newPos.Y - element.Y) > tolerance) {
                    element.X = newPos.X;
                    element.Y = newPos.Y;
                    update = true;
                }

                ImGui.End();
            }
        }

        public void Clear()
        {
            Elements.Clear();
            Update.Clear();
        }
    }
}
