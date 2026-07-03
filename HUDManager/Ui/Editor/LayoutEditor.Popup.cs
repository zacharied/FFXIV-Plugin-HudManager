using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using HUDManager.Configuration;
using HUDManager.Structs;
using HUDManager.Tree;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace HUDManager.Ui.Editor;

public partial class LayoutEditor {
    private void SetUpAddLayoutPopup(ref bool update, ref bool layoutChanged) {
        using var popup = ImRaii.Popup(Popups.AddLayout);
        if (!popup) return;

        var name = NewLayoutName ?? string.Empty;
        if (ImGui.InputText("Name", ref name, 100)) {
            NewLayoutName = string.IsNullOrWhiteSpace(name) ? null : name;
        }

        var exists = Plugin.Config.Layouts.Values.Any(layout => layout.Name == NewLayoutName);
        if (exists) {
            using (ImRaii.PushColor(ImGuiCol.Text, new Vector4(1f, 0f, 0f, 1f))) {
                ImGui.Text("A layout with that name already exists.");
            }
        } else if (ImGui.Button("Create") && NewLayoutName != null) {
            // create the layout
            var saved = new SavedLayout(NewLayoutName, new Dictionary<ElementKind, Element>(), new Dictionary<string, Window>(), Guid.Empty);
            // reset the new layout name
            NewLayoutName = null;

            // generate a new id
            var id = Guid.NewGuid();

            // add the layout
            Plugin.Config.Layouts[id] = saved;
            // switch the editor to the new layout
            Ui.SelectedLayout = id;

            update = true;
            layoutChanged = true;

            ImGui.CloseCurrentPopup();
        }
    }

    private void SetUpDeleteVerifyPopup(IEnumerable<Node<SavedLayout>> nodes, ref bool update, ref bool layoutChanged) {
        using var popup = ImRaii.PopupModal(Popups.DeleteVerify);
        if (!popup) return;

        if (Plugin.Config.Layouts.TryGetValue(Ui.SelectedLayout, out var deleting)) {
            ImGui.Text($"Are you sure you want to delete the layout \"{deleting.Name}\"?");

            if (ImGui.Button("Yes")) {
                // unset the parent of any child layouts
                var node = nodes.Find(Ui.SelectedLayout);
                if (node != null) {
                    foreach (var child in node.Children) {
                        child.Parent = null;
                        child.Value.Parent = Guid.Empty;
                    }
                }

                Plugin.Config.HudConditionMatches.RemoveAll(match => match.LayoutId == Ui.SelectedLayout);

                Plugin.Config.Layouts.Remove(Ui.SelectedLayout);
                Ui.SelectedLayout = Guid.Empty;
                update = true;
                layoutChanged = true;

                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();
            if (ImGui.Button("No")) {
                ImGui.CloseCurrentPopup();
            }
        }
    }

    private void SetUpRenameLayoutPopup(ref bool update) {
        using var popup = ImRaii.Popup(Popups.RenameLayout);
        if (!popup) return;

        var name = RenameLayoutName ?? "<none>";
        if (ImGui.InputText("Name", ref name, 100)) {
            RenameLayoutName = string.IsNullOrWhiteSpace(name) ? null : name;
        }

        if (ImGui.Button("Rename") && RenameLayoutName != null) {
            Plugin.Config.Layouts[Ui.SelectedLayout].Name = RenameLayoutName;
            update = true;

            ImGui.CloseCurrentPopup();
        }
    }

    private void SetUpImportLayoutPopup(ref bool update, ref bool layoutChanged) {
        using var popup = ImRaii.Popup(Popups.ImportLayout);
        if (!popup) return;

        ImGui.Text("Imported layout name:");

        var importName = ImportLayoutName ?? "";
        ImGui.SetNextItemWidth(160 * ImGuiHelpers.GlobalScale);
        if (ImGui.InputTextWithHint("###importName", "Enter a valid name", ref importName, 100)) {
            ImportLayoutName = string.IsNullOrWhiteSpace(importName) ? null : importName;
        }

        var exists = Plugin.Config.Layouts.Values.Any(layout => layout.Name == ImportLayoutName);
        if (exists) {
            using (ImRaii.PushColor(ImGuiCol.Text, new Vector4(1f, .8f, .2f, 1f))) {
                ImGui.TextColored(ImGuiColors.WarningForeground, "This will overwrite an existing layout.");
            }
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.Text("Import   ");
        ImGui.SameLine(0, 0);
        var color = ImportLayoutName == null ? ImGuiColors.ErrorForeground : ImGuiColors.ParsedGold;
        using (ImRaii.PushColor(ImGuiCol.Text, color)) {
            ImGuiExt.DrawLayoutTextPlain(ImportLayoutName ?? "<unnamed>");
        }
        ImGui.SameLine(0, 0);
        ImGui.Text("  from:");

        using var _ = ImRaii.Disabled(ImportLayoutName == null);

        var current = Hud.GetActiveHudSlot();
        foreach (var slot in Enum.GetValues<HudSlot>()) {
            var suffix = current == slot ? " (active)" : "";
            if (ImGui.Button($"Slot {(int)slot + 1}{suffix}###export-{slot}") && ImportLayoutName != null) {
                Guid id;
                string newName;
                Dictionary<string, Window> windows;
                if (exists) {
                    var overwriting = Plugin.Config.Layouts.First(entry => entry.Value.Name == ImportLayoutName);
                    id = overwriting.Key;
                    newName = overwriting.Value.Name;
                    windows = overwriting.Value.Windows;
                } else {
                    id = Guid.NewGuid();
                    newName = ImportLayoutName;
                    windows = new Dictionary<string, Window>();
                }

                var currentLayout = Hud.ReadLayout(slot);
                var newLayout = new SavedLayout(newName, currentLayout, windows);
                Plugin.Config.Layouts[id] = newLayout;
                Ui.SelectedLayout = id;
                update = true;
                layoutChanged = true;

                ReportImport($"slot {slot}");

                ImGui.CloseCurrentPopup();
            }
        }

        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Clipboard, "Clipboard") && ImportLayoutName != null) {
            SavedLayout? saved;
            try {
                saved = JsonConvert.DeserializeObject<SavedLayout>(ImGui.GetClipboardText());
            } catch (Exception e) {
                saved = null;
                Plugin.ChatGui.PrintError("Failed to import layout from clipboard.");
                Plugin.Log.Information(e, "failed to import from clipboard");
            }

            if (saved != null) {
                saved.Name = ImportLayoutName;

                var id = Guid.NewGuid();
                Plugin.Config.Layouts[id] = saved;
                Ui.SelectedLayout = id;
                update = true;
                layoutChanged = true;

                ReportImport("the clipboard");

                ImGui.CloseCurrentPopup();
            }
        }
        return;

        void ReportImport(string source) {
            Plugin.ChatGui.Print($"Imported from {source} to layout \"{ImportLayoutName}\".");
        }
    }

    private void SetUpExportLayoutPopup() {

        using var popup = ImRaii.Popup(Popups.ExportLayout);
        if (!popup) return;

        if (!Plugin.Config.Layouts.TryGetValue(Ui.SelectedLayout, out var layout)) {
            return;
        }

        ImGui.Text("Write   ");
        ImGui.SameLine(0, 0);
        using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.ParsedGold)) {
            ImGuiExt.DrawLayoutTextPlain(layout.Name);
        }
        ImGui.SameLine(0, 0);
        ImGui.Text("  to:");

        var current = Hud.GetActiveHudSlot();
        foreach (var slot in Enum.GetValues<HudSlot>()) {
            var suffix = current == slot ? " (active)" : "";
            if (ImGui.Button($"Slot {(int)slot + 1}{suffix}###export-{slot}")) {
                Plugin.Hud.WriteEffectiveLayout(slot, Ui.SelectedLayout);
                ReportExport(layout.Name, $"slot {slot}");
                ImGui.CloseCurrentPopup();
            }
        }

        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Clipboard, "Clipboard")) {
            var newLayout = new SavedLayout(layout) {
                Name = string.Empty,
                Parent = Guid.Empty,
            };
            var json = JsonConvert.SerializeObject(newLayout);
            ImGui.SetClipboardText(json);
            ReportExport(layout.Name, "the clipboard");
        }
        return;

        void ReportExport(string layoutName, string dest)
            => Plugin.ChatGui.Print($"Exported layout \"{layoutName}\" to {dest}.");
    }

    private void SetUpOptionsPopup(ref bool update) {
        using var popup = ImRaii.Popup(Popups.LayoutEditorOptions);
        if (!popup) return;

        var dragSpeed = Plugin.Config.DragSpeed;
        if (ImGui.DragFloat("Slider speed", ref dragSpeed, 0.01f, 0.01f, 10f)) {
            Plugin.Config.DragSpeed = dragSpeed;
            update = true;
        }

        using var combo = ImRaii.Combo("Positioning mode", Plugin.Config.PositioningMode.ToString());
        if (combo) {
            foreach (var mode in Enum.GetValues<PositioningMode>()) {
                if (!ImGui.Selectable($"{mode}##positioning", Plugin.Config.PositioningMode == mode)) {
                    continue;
                }

                Plugin.Config.PositioningMode = mode;
                update = true;
            }
        }
    }
}
