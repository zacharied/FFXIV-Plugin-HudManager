using Dalamud.Interface;
using HUDManager.Configuration;
using HUDManager.Structs;
using HUDManager.Tree;
using HUDManager.Ui.Editor.Tabs;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace HUDManager.Ui.Editor;

public class LayoutEditor
{
    private Plugin Plugin { get; }
    private Interface Ui { get; }
    internal Previews Previews { get; }
    private HudElements HudElements { get; }
    private WindowElements Windows { get; }
    private ExternalElements ExternalElements { get; }

    private string? RenameLayoutName { get; set; }
    private string? NewLayoutName { get; set; }
    private string? ImportLayoutName { get; set; }

    public LayoutEditor(Plugin plugin, Interface ui)
    {
        Plugin = plugin;
        Ui = ui;

        Previews = new Previews(plugin, ui);
        HudElements = new HudElements(plugin, ui, this);
        Windows = new WindowElements(plugin);
        ExternalElements = new ExternalElements(plugin, ui);
    }

    internal void Draw() {
        using var layoutEditorTab = ImRaii.TabItem("Layout Editor");
        if (!layoutEditorTab) {
            Plugin.Swapper.SetEditLock(false);
            return;
        }

        // Lock enabled on this frame, so if swaps are enabled:
        // - check the if the active layout changed,
        // - set it and clear any left over previews if it did
        if (Plugin.Swapper.SetEditLock(true)
            && Plugin.Config.SwapsEnabled
            && Plugin.Statuses.ResultantLayout.activeLayout is { LayoutId: var newLayout }
            && Ui.SelectedLayout != newLayout) {
            Ui.SelectedLayout = newLayout;
            Previews.Clear();
        }

        var update = false;
        var layoutChanged = false;

        if (Util.IsCharacterConfigOpen()) {
            ImGui.TextUnformatted("Please close the Character Configuration window before continuing.");
            return;
        }

        if (!Plugin.Config.DisableHelpPanels) {
            ImGui.TextUnformatted("Note that swaps are disabled while this menu is open.");
        }

        Previews.Draw(ref update);

        ImGui.TextUnformatted("Layout");

        var nodes = Node<SavedLayout>.BuildTree(Plugin.Config.Layouts);

        Plugin.Config.Layouts.TryGetValue(Ui.SelectedLayout, out var selected);
        var selectedName = selected?.Name ?? "<none>";

        using (var combo = ImRaii.Combo("##edit-layout", selectedName)) {
            if (combo) {
                if (ImGui.Selectable("<none>")) {
                    Ui.SelectedLayout = Guid.Empty;
                    layoutChanged = true;
                }

                foreach (var node in nodes) {
                    foreach (var (child, depth) in node.TraverseWithDepth()) {
                        var indent = new string(' ', (int)depth * 4);
                        if (!ImGui.Selectable($"{indent}{child.Value.Name}##edit-{child.Id}", child.Id == Ui.SelectedLayout)) {
                            continue;
                        }

                        Ui.SelectedLayout = child.Id;
                        update = true;
                        layoutChanged = true;
                    }
                }
            }
        }

        ImGui.SameLine();
        if (ImGuiExt.IconButton(FontAwesomeIcon.Plus, "uimanager-add-layout")) {
            ImGui.OpenPopup(Popups.AddLayout);
        }

        ImGuiExt.HoverTooltip("Add a new layout");

        SetUpAddLayoutPopup(ref update, ref layoutChanged);

        ImGui.SameLine();
        if (ImGuiExt.IconButton(FontAwesomeIcon.TrashAlt, "uimanager-delete-layout") && Ui.SelectedLayout != Guid.Empty) {
            ImGui.OpenPopup(Popups.DeleteVerify);
        }

        SetUpDeleteVerifyPopup(nodes, ref update, ref layoutChanged);

        ImGuiExt.HoverTooltip("Delete the selected layout");

        ImGui.SameLine();
        if (ImGuiExt.IconButton(FontAwesomeIcon.Edit, "uimanager-rename-layout") && Ui.SelectedLayout != Guid.Empty) {
            RenameLayoutName = Plugin.Config.Layouts[Ui.SelectedLayout].Name;
            ImGui.OpenPopup(Popups.RenameLayout);
        }

        ImGuiExt.HoverTooltip("Rename the selected layout");

        SetUpRenameLayoutPopup(ref update);

        ImGui.SameLine();
        if (ImGuiExt.IconButton(FontAwesomeIcon.FileImport, "uimanager-import-layout")) {
            ImGui.OpenPopup(Popups.ImportLayout);
        }

        ImGuiExt.HoverTooltip("Import a layout from an in-game HUD slot or the clipboard");

        SetUpImportLayoutPopup(ref update, ref layoutChanged);

        ImGui.SameLine();
        if (ImGuiExt.IconButton(FontAwesomeIcon.FileExport, "uimanager-export-layout")) {
            ImGui.OpenPopup(Popups.ExportLayout);
        }

        ImGuiExt.HoverTooltip("Export a layout to an in-game HUD slot or the clipboard");

        SetUpExportLayoutPopup();

        if (Ui.SelectedLayout != Guid.Empty) {
            DrawMainSection(nodes, ref update);

            SetUpOptionsPopup(ref update);

            ImGui.Indent();
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Cog, "Positioning settings")) {
                ImGui.OpenPopup(Popups.LayoutEditorOptions);
            }
        }

        if (layoutChanged) {
            // Kill all previews so they don't fuck up the new layout.
            Previews.Clear();
        }
        if (update) {
            Plugin.Config.Save();
        }
    }

    private void DrawMainSection(List<Node<SavedLayout>> nodes, ref bool update) {
        var height = ImGui.GetContentRegionAvail().Y - ImGui.GetFrameHeight() - ImGui.GetStyle().ItemSpacing.Y;

        using var editorChild = ImRaii.Child("##layout-editor-main", new Vector2(-1, height), true);
        if (!editorChild) return;

        var layout = Plugin.Config.Layouts[Ui.SelectedLayout];

        Plugin.Config.Layouts.TryGetValue(layout.Parent, out var parent);
        var parentName = parent?.Name ?? "<none>";

        var ourChildren = nodes.Find(Ui.SelectedLayout)?.Traverse().Select(el => el.Id).ToArray() ?? [];

        using (var combo = ImRaii.Combo("Parent", parentName)) {
            if (combo) {
                if (ImGui.Selectable("<none>")) {
                    layout.Parent = Guid.Empty;
                    Plugin.Config.Save();
                }

                foreach (var node in nodes) {
                    foreach (var (child, depth) in node.TraverseWithDepth()) {
                        var selectedParent = child.Id == Ui.SelectedLayout;
                        var disabled = selectedParent || ourChildren.Contains(child.Id);
                        var flags = disabled ? ImGuiSelectableFlags.Disabled : ImGuiSelectableFlags.None;

                        var indent = new string(' ', (int)depth * 4);
                        if (!ImGui.Selectable($"{indent}{child.Value.Name}##parent-{child.Id}", selectedParent, flags)) {
                            continue;
                        }

                        layout.Parent = child.Id;
                        Plugin.Config.Save();
                    }
                }
            }
        }

        ImGui.SameLine();
        ImGuiExt.HelpMarker("A layout will inherit its parameters from its parent if it has one."
                            + "\n\nWhen a parent layout is set, the \"Enabled\" column will be visible for each parameter of an element."
                            + "\n\nA parameter must be enabled for it to have any effect. If it is not enabled, the value from the parent layout will be used instead.");

        using (var tabBar = ImRaii.TabBar("uimanager-positioning")) {
            if (tabBar) {
                using (var tabItem = ImRaii.TabItem("HUD Elements")) {
                    if (tabItem) {
                        HudElements.Draw(layout, ref update);
                    }
                }

                using (var tabItem = ImRaii.TabItem("Windows")) {
                    if (tabItem) {
                        Windows.Draw(layout, ref update);
                    }
                }

                using (var tabItem = ImRaii.TabItem("External Elements")) {
                    if (tabItem) {
                        ExternalElements.Draw(layout, ref update);
                    }
                }
            }
        }
    }

    private void SetUpAddLayoutPopup(ref bool update, ref bool layoutChanged) {
        var popup = ImRaii.Popup(Popups.AddLayout);
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
        } else if (ImGui.Button("Add") && NewLayoutName != null) {
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

    private void SetUpRenameLayoutPopup(ref bool update)
    {
        var popup = ImRaii.Popup(Popups.RenameLayout);
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

        var importName = ImportLayoutName ?? "";
        if (ImGui.InputText("Imported layout name", ref importName, 100)) {
            ImportLayoutName = string.IsNullOrWhiteSpace(importName) ? null : importName;
        }

        void ReportImport(string source)
        {
            Plugin.ChatGui.Print($"Imported from {source} to layout \"{ImportLayoutName}\".");
        }

        var exists = Plugin.Config.Layouts.Values.Any(layout => layout.Name == ImportLayoutName);
        if (exists) {
            using (ImRaii.PushColor(ImGuiCol.Text, new Vector4(1f, .8f, .2f, 1f))) {
                ImGui.Text("This will overwrite an existing layout.");
            }
        }

        var current = Hud.GetActiveHudSlot();
        foreach (var slot in Enum.GetValues<HudSlot>()) {
            var name = current == slot ? $"({(int)slot + 1})" : $"{(int)slot + 1}";
            if (ImGui.Button($"{name}##import-{slot}") && ImportLayoutName != null) {
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

            ImGui.SameLine();
        }

        if (ImGuiExt.IconButton(FontAwesomeIcon.Clipboard, "import-clipboard") && ImportLayoutName != null) {
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
    }

    private void SetUpExportLayoutPopup()
    {
        void ReportExport(string layoutName, string dest)
            => Plugin.ChatGui.Print($"Exported layout \"{layoutName}\" to {dest}.");

        using var popup = ImRaii.Popup(Popups.ExportLayout);
        if (!popup) return;

        if (!Plugin.Config.Layouts.TryGetValue(Ui.SelectedLayout, out var layout)) {
            return;
        }

        var current = Hud.GetActiveHudSlot();
        foreach (var slot in Enum.GetValues<HudSlot>()) {
            var name = current == slot ? $"({(int)slot + 1})" : $"{(int)slot + 1}";
            if (ImGui.Button($"{name}##export-{slot}")) {
                Plugin.Hud.WriteEffectiveLayout(slot, Ui.SelectedLayout);
                ReportExport(layout.Name, $"slot {slot}");

                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();
        }

        if (ImGuiExt.IconButton(FontAwesomeIcon.Clipboard, "export-clipboard")) {
            var newLayout = new SavedLayout(layout)
            {
                Name = string.Empty,
                Parent = Guid.Empty,
            };
            var json = JsonConvert.SerializeObject(newLayout);
            ImGui.SetClipboardText(json);
            ReportExport(layout.Name, "the clipboard");
        }
    }

    private void SetUpOptionsPopup(ref bool update) {
        using var popup = ImRaii.Popup(Popups.LayoutEditorOptions);
        if (!popup) return;

        var dragSpeed = Plugin.Config.DragSpeed;
        if (ImGui.DragFloat("Slider speed", ref dragSpeed, 0.01f, 0.01f, 10f)) {
            Plugin.Config.DragSpeed = dragSpeed;
            update = true;
        }

        using (var combo = ImRaii.Combo("Positioning mode", Plugin.Config.PositioningMode.ToString())) {
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
}
