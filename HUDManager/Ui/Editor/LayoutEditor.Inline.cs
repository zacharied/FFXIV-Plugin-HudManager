using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using HUDManager.Configuration;
using HUDManager.Tree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace HUDManager.Ui.Editor;

public partial class LayoutEditor {

    private const ImGuiComboFlags ImGuiComboFlagsCustomPreview = (ImGuiComboFlags)(1 << 20);

    private void DrawLayoutManagerInlineView(List<Node<SavedLayout>> nodes, ref bool layoutChanged, ref bool update) {
        DrawLayoutManagerSmall(nodes, ref layoutChanged, ref update);

        DrawLayoutContents(ref update);
    }

    private void DrawLayoutManagerSmall(List<Node<SavedLayout>> nodes, ref bool layoutChanged, ref bool update) {
        using var tableStyle = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, new Vector2(0, ImGui.GetStyle().ItemSpacing.Y / 2));
        using var table = ImRaii.Table("layoutInline", 2);
        if (!table) return;

        var textLayout = "Layout";
        var textParent = "Parent";
        var leftColumnWidth = Math.Max(ImGui.CalcTextSize(textLayout).X, ImGui.CalcTextSize(textParent).X) + ImGui.GetStyle().ItemSpacing.X;

        ImGui.TableSetupColumn("Layouts", ImGuiTableColumnFlags.WidthFixed, leftColumnWidth);
        ImGui.TableSetupColumn("Details", ImGuiTableColumnFlags.WidthStretch);

        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.Text(textLayout);
        ImGui.TableNextColumn();

        Plugin.Config.Layouts.TryGetValue(Ui.SelectedLayout, out var savedLayout);

        var iconButtonWidths =
            ImGuiExt.IconButtonWidth(FontAwesomeIcon.Plus)
            + ImGuiExt.IconButtonWidth(FontAwesomeIcon.TrashAlt)
            + ImGuiExt.IconButtonWidth(FontAwesomeIcon.Edit)
            + ImGuiExt.IconButtonWidth(FontAwesomeIcon.FileImport)
            + ImGuiExt.IconButtonWidth(FontAwesomeIcon.FileExport);
        var comboWidth = ImGui.GetContentRegionAvail().X - iconButtonWidths - ImGui.GetStyle().ItemSpacing.X * 5;

        ImGui.SetNextItemWidth(comboWidth);
        using (var combo = ImRaii.Combo("##edit-layout", "" /*selectedName*/, ImGuiComboFlagsCustomPreview)) {
            if (combo) {
                if (ImGui.Selectable("###layoutEditInline:<none>")) {
                    Ui.SelectedLayout = Guid.Empty;
                    layoutChanged = true;
                }
                ImGui.SameLine();
                DrawLayoutText(null, Guid.Empty == Ui.SelectedLayout);

                foreach (var node in nodes) {
                    foreach (var (child, depth) in node.TraverseWithDepth()) {
                        var indent = new string(' ', (int)depth * 4);
                        if (ImGui.Selectable($"{indent}###layoutEditInline:{child.Id}", child.Id == Ui.SelectedLayout)) {
                            Ui.SelectedLayout = child.Id;
                            update = true;
                            layoutChanged = true;
                        }
                        ImGui.SameLine();
                        DrawLayoutText(child.Value, child.Id == Ui.SelectedLayout);
                    }
                }
            }
        }

        if (ImGuiP.BeginComboPreview()) {
            DrawLayoutText(savedLayout, true);
            ImGuiP.EndComboPreview();
        }

        var isNoneSelected = Ui.SelectedLayout == Guid.Empty;

        ImGui.SameLine();
        if (ImGuiExt.IconButton(FontAwesomeIcon.FileCirclePlus, "uimanager-add-layout")) {
            ImGui.OpenPopup(Popups.AddLayout);
        }
        ImGuiExt.HoverTooltip("Create a new layout");
        SetUpAddLayoutPopup(ref update, ref layoutChanged);

        using (ImRaii.Disabled(isNoneSelected)) {
            ImGui.SameLine();
            if (ImGuiExt.IconButton(FontAwesomeIcon.TrashAlt, "uimanager-delete-layout") && Ui.SelectedLayout != Guid.Empty) {
                ImGui.OpenPopup(Popups.DeleteVerify);
            }
        }
        ImGuiExt.HoverTooltip("Delete the selected layout");
        SetUpDeleteVerifyPopup(nodes, ref update, ref layoutChanged);


        using (ImRaii.Disabled(isNoneSelected)) {
            ImGui.SameLine();
            if (ImGuiExt.IconButton(FontAwesomeIcon.Edit, "uimanager-rename-layout") && Ui.SelectedLayout != Guid.Empty) {
                RenameLayoutName = Plugin.Config.Layouts[Ui.SelectedLayout].Name;
                ImGui.OpenPopup(Popups.RenameLayout);
            }
        }
        ImGuiExt.HoverTooltip("Rename the selected layout");
        SetUpRenameLayoutPopup(ref update);


        ImGui.SameLine();
        if (ImGuiExt.IconButton(FontAwesomeIcon.FileImport, "uimanager-import-layout")) {
            ImGui.OpenPopup(Popups.ImportLayout);
        }
        ImGuiExt.HoverTooltip("Import a layout from an in-game HUD slot or the clipboard");
        SetUpImportLayoutPopup(ref update, ref layoutChanged);

        using (ImRaii.Disabled(isNoneSelected)) {
            ImGui.SameLine();
            if (ImGuiExt.IconButton(FontAwesomeIcon.FileExport, "uimanager-export-layout")) {
                ImGui.OpenPopup(Popups.ExportLayout);
            }
        }
        ImGuiExt.HoverTooltip("Export a layout to an in-game HUD slot or the clipboard");
        SetUpExportLayoutPopup();


        if (savedLayout != null) {
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.Text(textParent);
            ImGui.TableNextColumn();

            DrawLayoutManagerSmallParent(nodes, savedLayout, comboWidth, ref layoutChanged, ref update);
            ImGui.SameLine();
        } else {
            ImGui.TableNextColumn();
            ImGui.TableNextColumn();
        }
        ImCursor.X += ImGui.GetContentRegionAvail().X - ImGuiComponents.GetIconButtonWithTextWidth(FontAwesomeIcon.FolderTree, "Tree view");
        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.FolderTree, "Tree view")) {
            Plugin.Config.UseLayoutListTreeView = true;
            update = true;
        }
        ImGuiExt.HoverTooltip("Open layout tree view");

        ImGui.Spacing();
    }

    private void DrawLayoutManagerSmallParent(List<Node<SavedLayout>> nodes, SavedLayout savedLayout, float comboWidth, ref bool layoutChanged, ref bool update) {
        Plugin.Config.Layouts.TryGetValue(savedLayout.Parent, out var parent);

        var ourChildren = nodes.Find(Ui.SelectedLayout)?.Traverse().Select(el => el.Id).ToArray() ?? [];

        ImGui.SetNextItemWidth(comboWidth);
        using (var combo = ImRaii.Combo("###parent", "", ImGuiComboFlagsCustomPreview)) {
            if (combo) {
                if (ImGui.Selectable("###parent:<none>")) {
                    savedLayout.Parent = Guid.Empty;
                    layoutChanged = true;
                    update = true;
                }
                ImGui.SameLine();
                DrawLayoutText(null, false);

                foreach (var node in nodes) {
                    foreach (var (child, depth) in node.TraverseWithDepth()) {
                        var selectedParent = child.Id == Ui.SelectedLayout;
                        var disabled = selectedParent || ourChildren.Contains(child.Id);
                        var flags = disabled ? ImGuiSelectableFlags.Disabled : ImGuiSelectableFlags.None;

                        var indent = new string(' ', (int)depth * 4);
                        if (ImGui.Selectable($"{indent}###parent:{child.Id}", selectedParent, flags)) {
                            savedLayout.Parent = child.Id;
                            layoutChanged = true;
                            update = true;
                        }
                        ImGui.SameLine();
                        using (ImRaii.PushColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled), disabled)) {
                            DrawLayoutText(child.Value, selectedParent);
                        }
                    }
                }
            }
        }
        if (ImGuiP.BeginComboPreview()) {
            DrawLayoutText(parent, false);
            ImGuiP.EndComboPreview();
        }

        ImGui.SameLine();
        ImGuiExt.HelpMarker("A layout will inherit its parameters from its parent if it has one."
                            + "\n\nWhen a parent layout is set, the \"Enabled\" column will be visible for each parameter of an element."
                            + "\n\nA parameter must be enabled for it to have any effect. If it is not enabled, the value from the parent layout will be used instead.");
    }

    private static void DrawLayoutText(SavedLayout? node, bool isSelected) {
        if (node == null) {
            var iconColor = isSelected ? ImGuiColors.InfoForeground : ImGuiColors.DalamudGrey3;
            using (ImRaii.PushFont(UiBuilder.IconFontFixedWidth))
            using (ImRaii.PushColor(ImGuiCol.Text, iconColor)) {
                ImGui.Text(FontAwesomeIcon.BorderNone.ToIconString());
            }
            ImGui.SameLine(0, 0);
            ImGui.Text($" <none>");
        } else {
            var iconColor = isSelected ? ImGuiColors.InfoForeground : ImGuiColors.DalamudGrey3;
            using (ImRaii.PushFont(UiBuilder.IconFontFixedWidth))
            using (ImRaii.PushColor(ImGuiCol.Text, iconColor)) {
                ImGui.Text(FontAwesomeIcon.LayerGroup.ToIconString());
            }
            ImGui.SameLine(0, 0);
            ImGui.Text($" {node.Name}");
        }
    }

    private static void DrawLayoutTextPlain(string name) {
        using (ImRaii.PushFont(UiBuilder.IconFontFixedWidth)) {
            ImGui.Text(FontAwesomeIcon.LayerGroup.ToIconString());
        }
        ImGui.SameLine(0, 0);
        ImGui.Text($" {name}");
    }
}
