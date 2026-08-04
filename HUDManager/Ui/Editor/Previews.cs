using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using HUDManager.Structs;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace HUDManager.Ui.Editor;

public class Previews {
    private Plugin Plugin { get; }
    private Interface Ui { get; }

    internal HashSet<ElementKind> Elements { get; } = [];
    internal HashSet<ElementKind> Update { get; } = [];

    public Previews(Plugin plugin, Interface ui) {
        Plugin = plugin;
        Ui = ui;
    }

    public void Draw(ref bool update) {
        const float tolerance = 0.0001f;

        if (Ui.SelectedLayout == Guid.Empty)
            return;

        if (!Plugin.Config.Layouts.TryGetValue(Ui.SelectedLayout, out var layout))
            return;

        foreach (var element in layout.Elements.Values) {
            if (!Elements.Contains(element.Id))
                continue;

            var preview = PreviewUtils.CreatePreviewData(Plugin.DataManager, element);

            var (pos, size) = preview.ActiveRect;
            var pixelPos = new Vector2(float.Truncate(pos.X), float.Truncate(pos.Y));

            if (Update.Remove(element.Id)) {
                ImGui.SetNextWindowPos(pos);
            } else {
                ImGui.SetNextWindowPos(pos, ImGuiCond.Appearing);
            }
            ImGui.SetNextWindowSize(size);

            using (ImRaii.PushColor(ImGuiCol.WindowBg, new Vector4(0f, 0f, 0f, .5f)))
            using (ImRaii.PushStyle(ImGuiStyleVar.WindowRounding, 0))
            using (ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.Zero))
            using (ImRaii.PushStyle(ImGuiStyleVar.WindowMinSize, Vector2.Zero)) {
                if (!ImGui.Begin($"##uimanager-preview-{element.Id}", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoScrollbar)) {
                    continue;
                }
            }

            ImGui.Text(element.Id.LocalisedName(Plugin.DataManager));

            var pixelPosAfter = ImGui.GetWindowPos();
            if (pixelPosAfter != pixelPos) {
                var gamePos = PreviewUtils.ConvertImGuiToGame(element, preview, ImGui.GetWindowPos());
                if (Math.Abs(gamePos.X - element.X) > tolerance || Math.Abs(gamePos.Y - element.Y) > tolerance) {
                    element.X = gamePos.X;
                    element.Y = gamePos.Y;
                    update = true;
                }
            }

            ImGui.End();
        }
    }

    public void Clear() {
        Elements.Clear();
        Update.Clear();
    }
}
