using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using HUDManager.Configuration;
using HUDManager.Tree;
using HUDManager.Ui.DragDrop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace HUDManager.Ui.Editor;

public partial class LayoutEditor {
    private readonly TreeDragDrop treeDragDrop = new("TREE");

    private void DrawLayoutManagerTreeView(List<Node<SavedLayout>> nodes, ref bool layoutChanged, ref bool update) {
        var defaultCellPadding = ImGui.GetStyle().CellPadding;
        using var tableStyle = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, ImGuiHelpers.ScaledVector2(4, 0));
        using var table = ImRaii.Table("layoutTree", 2, ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerV);
        if (table) {
            ImGui.TableSetupColumn("Layouts", ImGuiTableColumnFlags.WidthFixed, 160 * ImGuiHelpers.GlobalScale);
            ImGui.TableSetupColumn("Details", ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            using (ImRaii.PushStyle(ImGuiStyleVar.CellPadding, defaultCellPadding)) {
                DrawLayoutManagerTree(nodes, ref layoutChanged, ref update);
                DrawLayoutManagerTreeButtons(ref layoutChanged, ref update);
            }

            ImGui.TableNextColumn();
            using (ImRaii.PushStyle(ImGuiStyleVar.CellPadding, defaultCellPadding)) {
                DrawLayoutManagerTreeDetails(nodes, ref layoutChanged, ref update);
            }
        }
    }

    private void DrawLayoutManagerTreeDetails(List<Node<SavedLayout>> nodes, ref bool layoutChanged, ref bool update) {
        if (Ui.SelectedLayout == Guid.Empty)
            return;

        var layout = Plugin.Config.Layouts[Ui.SelectedLayout];

        var iconButtonWidths =
            ImGuiExt.IconButtonWidth(FontAwesomeIcon.Edit)
            + ImGuiExt.IconButtonWidth(FontAwesomeIcon.TrashAlt)
            + ImGuiExt.IconButtonWidth(FontAwesomeIcon.FileExport);
        var buttonStart = ImGui.GetContentRegionMax().X - iconButtonWidths - ImGui.GetStyle().ItemSpacing.X * 2;

        // ImGui.Text(layout.Name);

        ImGuiExt.DrawLayoutText(layout.Name, false);

        ImGui.SameLine();
        ImCursor.X = buttonStart;

        if (ImGuiExt.IconButton(FontAwesomeIcon.Edit, "uimanager-rename-layout") && Ui.SelectedLayout != Guid.Empty) {
            RenameLayoutName = Plugin.Config.Layouts[Ui.SelectedLayout].Name;
            ImGui.OpenPopup(Popups.RenameLayout);
        }
        ImGuiExt.HoverTooltip("Rename the selected layout");
        SetUpRenameLayoutPopup(ref update);

        ImGui.SameLine();
        if (ImGuiExt.IconButton(FontAwesomeIcon.TrashAlt, "uimanager-delete-layout") && Ui.SelectedLayout != Guid.Empty) {
            ImGui.OpenPopup(Popups.DeleteVerify);
        }
        ImGuiExt.HoverTooltip("Delete the selected layout");
        SetUpDeleteVerifyPopup(nodes, ref update, ref layoutChanged);

        ImGui.SameLine();
        if (ImGuiExt.IconButton(FontAwesomeIcon.FileExport, "uimanager-export-layout")) {
            ImGui.OpenPopup(Popups.ExportLayout);
        }
        ImGuiExt.HoverTooltip("Export a layout to an in-game HUD slot or the clipboard");
        SetUpExportLayoutPopup();

        DrawLayoutContents(ref update);
    }

    private void DrawLayoutManagerTree(List<Node<SavedLayout>> nodes, ref bool layoutChanged, ref bool update) {
        using var listChild = ImRaii.Child("layoutTree", new Vector2(-1, -GetTableButtonSpace(2)), false, ImGuiWindowFlags.NoSavedSettings);
        if (!listChild) return;

        TreeAction? treeAction = null;
        using (new ImRaii.StyleDisposable()
                   .Push(ImGuiStyleVar.ItemSpacing, Vector2.Zero)
                   .Push(ImGuiStyleVar.FramePadding, ImGuiHelpers.ScaledVector2(0, 3))) {
            DrawTreeNodeNone(ref treeAction);
            foreach (var node in nodes) {
                DrawTreeNode(node, true, ref treeAction, ref update);
            }
            DrawTreeNodeEnd(ref treeAction);
        }

        if (treeAction is { } action) {
            var config = Plugin.Config;
            switch (action) {
                case TreeAction.Activate activate:
                    Ui.SelectedLayout = activate.Id;
                    layoutChanged = true;
                    update = true;
                    break;

                case TreeAction.PlaceAfter placeAfter:
                    if (SetParentFromSibling(placeAfter.Id, placeAfter.SiblingId))
                        update = true;
                    if (config.Layouts.SlideAfter(placeAfter.Id, placeAfter.SiblingId))
                        update = true;
                    break;

                case TreeAction.PlaceBefore placeBefore:
                    if (SetParentFromSibling(placeBefore.Id, placeBefore.SiblingId))
                        update = true;
                    if (config.Layouts.SlideBefore(placeBefore.Id, placeBefore.SiblingId))
                        update = true;
                    break;

                case TreeAction.PlaceEnd placeEnd:
                    if (SetParentTo(placeEnd.Id, Guid.Empty))
                        update = true;
                    if (config.Layouts.SlideToEnd(placeEnd.Id))
                        update = true;
                    break;

                case TreeAction.PlaceStart placeStart:
                    if (SetParentTo(placeStart.Id, Guid.Empty))
                        update = true;
                    if (config.Layouts.SlideToStart(placeStart.Id))
                        update = true;
                    break;

                case TreeAction.SetParent setParent:
                    if (SetParentTo(setParent.Id, setParent.ParentId)) {
                        update = true;
                        SlideToLastAmongChildren(setParent.Id);
                    }
                    break;
            }
        }

        treeDragDrop.EndFrame();
    }

    private bool SetParentFromSibling(Guid targetId, Guid siblingId) {
        var dict = Plugin.Config.Layouts;
        if (dict.TryGetValue(targetId, out var target) && dict.TryGetValue(siblingId, out var sibling)) {
            if (target.Parent != sibling.Parent) {
                target.Parent = sibling.Parent;
                return true;
            }
        }
        return false;
    }

    private bool SetParentTo(Guid targetId, Guid parentId) {
        var dict = Plugin.Config.Layouts;
        if (dict.TryGetValue(targetId, out var target)) {
            if (target.Parent != parentId) {
                target.Parent = parentId;
                return true;
            }
        }
        return false;
    }

    private bool SlideToLastAmongChildren(Guid needle) {
        var tree = Node<SavedLayout>.BuildTree(Plugin.Config.Layouts);
        foreach (var rootNode in tree) {
            if (rootNode.Traverse().Find(needle) is { Parent: { } parent }) {
                if (parent.Children.Last() is { } lastChild) {
                    return Plugin.Config.Layouts.SlideAfter(needle, lastChild.Id);
                }
            }
        }
        return false;
    }

    private void DrawTreeNodeNone(ref TreeAction? treeAction) {
        var isSelected = Ui.SelectedLayout == Guid.Empty;

        {
            using var color = new ImRaii.ColorDisposable()
                .Push(ImGuiCol.Text, Vector4.Zero)
                .Push(ImGuiCol.HeaderActive, Vector4.Zero, ImGuiP.IsDragDropActive())
                .Push(ImGuiCol.HeaderHovered, Vector4.Zero, ImGuiP.IsDragDropActive());
            using var treeNode = ImRaii.TreeNode($"##treeNodeNone", ImGuiTreeNodeFlags.SpanAvailWidth | ImGuiTreeNodeFlags.FramePadding | ImGuiTreeNodeFlags.Leaf);
            if (TreeNodeClicked() && (!isSelected)) {
                treeAction = new TreeAction.Activate(Guid.Empty);
            }
        }

        var itemRectMin = ImGui.GetItemRectMin();
        var itemRectMax = ImGui.GetItemRectMax();

        using (var drop = treeDragDrop.Drop(Guid.Empty, "")) {
            if (drop.Hovered) {
                treeDragDrop.PreviewInsertAfter(itemRectMin, itemRectMax);
            } else if (drop.Dropped) {
                treeAction = new TreeAction.PlaceStart(treeDragDrop.SourceId);
            }
        }

        if (isSelected) {
            ImGui.GetWindowDrawList().AddRectFilled(itemRectMin with { X = itemRectMin.X - 4 }, itemRectMax with { X = itemRectMax.X + 4 }, ImGui.ColorConvertFloat4ToU32(ImGuiColors.InfoBackground), 6f);
        }

        ImGui.SameLine();
        using (ImCursor.Excursion()) {
            ImCursor.X -= ImGui.GetTreeNodeToLabelSpacing();
            var iconColor = isSelected ? ImGuiColors.InfoForeground : ImGuiColors.DalamudGrey3;
            using (ImRaii.PushFont(UiBuilder.IconFontFixedWidth))
            using (ImRaii.PushColor(ImGuiCol.Text, iconColor)) {
                ImGui.Text(FontAwesomeIcon.BorderNone.ToIconString());
            }
        }

        ImGui.SameLine();
        using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudGrey3)) {
            ImGui.Text($" <none>");
        }
    }

    private void DrawTreeNode(Node<SavedLayout>? node, bool allowDrop, ref TreeAction? treeAction, ref bool update) {
        if (node == null)
            return;

        var hasChildren = node.Children.Count != 0;
        var isSelected = node.Id == Ui.SelectedLayout;
        var flags = ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.SpanAvailWidth | ImGuiTreeNodeFlags.FramePadding | ImGuiTreeNodeFlags.OpenOnArrow;
        if (!hasChildren)
            flags |= ImGuiTreeNodeFlags.Leaf;

        using var _ = new ImRaii.ColorDisposable()
            .Push(ImGuiCol.HeaderActive, Vector4.Zero, ImGuiP.IsDragDropActive())
            .Push(ImGuiCol.HeaderHovered, Vector4.Zero, ImGuiP.IsDragDropActive());

        var transparentColor = ImRaii.PushColor(ImGuiCol.Text, Vector4.Zero);
        using var treeNode = ImRaii.TreeNode($"##treeNode:{node.Id}", flags);
        transparentColor.Dispose();
        var isExpanded = hasChildren && treeNode.Success;

        if (TreeNodeClicked() && !isSelected) {
            treeAction = new TreeAction.Activate(node.Id);
        }

        using (treeDragDrop.Drag(node.Id, node.Value.Name)) { }

        var itemRectMin = ImGui.GetItemRectMin();
        var itemRectMax = ImGui.GetItemRectMax();
        var isDraggingThis = treeDragDrop.IsSource(node.Id);

        if (allowDrop) {
            using var drop = treeDragDrop.Drop(node.Id, node.Value.Name);
            var action = (treeDragDrop.GetDragMode(), isExpanded, ImGuiExt.GetMousePosInRect(itemRectMin, itemRectMax).Y) switch {
                (TreeDragMode.Insert, true, _) => TreeActionKind.InsertBefore,
                (TreeDragMode.Insert, _, < 0.50f) => TreeActionKind.InsertBefore,
                (TreeDragMode.Insert, _, _) => TreeActionKind.InsertAfter,
                (TreeDragMode.Attach, _, _) => TreeActionKind.Attach,
                (_, true, < 0.50f) => TreeActionKind.InsertBefore,
                (_, true, _) => TreeActionKind.Attach,
                (_, false, < 0.25f) => TreeActionKind.InsertBefore,
                (_, false, > 0.75f) => TreeActionKind.InsertAfter,
                (_, false, _) => TreeActionKind.Attach
            };

            if (drop.Hovered) {
                treeDragDrop.HoverName = node.Value.Name;
                if (action == TreeActionKind.InsertBefore) {
                    treeDragDrop.PreviewInsertBefore(itemRectMin, itemRectMax);
                } else if (action == TreeActionKind.InsertAfter) {
                    treeDragDrop.PreviewInsertAfter(itemRectMin, itemRectMax);
                } else {
                    treeDragDrop.PreviewAttach(itemRectMin, itemRectMax);
                }
            } else if (drop.Dropped) {
                if (action == TreeActionKind.InsertBefore) {
                    treeAction = new TreeAction.PlaceBefore(treeDragDrop.SourceId, node.Id);
                } else if (action == TreeActionKind.InsertAfter) {
                    treeAction = new TreeAction.PlaceAfter(treeDragDrop.SourceId, node.Id);
                } else {
                    treeAction = new TreeAction.SetParent(treeDragDrop.SourceId, node.Id);
                }
            }
        }

        if (!isSelected) {
            using var drop = ElementDragDrop.Drop();
            if (ElementDragDrop.SourceElement is { } sourceElement) {
                if (drop.Any) {
                    ElementDragDrop.ElementExists = node.Value.Elements.ContainsKey(sourceElement.Id);
                }
                if (drop.Dropped && ElementDragDrop.AllowWrite) {
                    node.Value.Elements[sourceElement.Id] = sourceElement;
                    if (ElementDragDrop.GetActionKind() == ElementActionKind.Move && ElementDragDrop.SourceLayout is { } sourceLayout) {
                        sourceLayout.Elements.Remove(sourceElement.Id);
                    }
                    update = true;
                }
            }
        }

        if (isSelected) {
            ImGui.GetWindowDrawList().AddRectFilled(itemRectMin with { X = itemRectMin.X - 4 }, itemRectMax with { X = itemRectMax.X + 4 }, ImGui.ColorConvertFloat4ToU32(ImGuiColors.InfoBackground), 6f);
        }

        ImGui.SameLine();
        using (ImCursor.Excursion()) {
            ImCursor.X -= ImGui.GetTreeNodeToLabelSpacing();
            var iconColor = isSelected ? ImGuiColors.InfoForeground : ImGuiColors.DalamudGrey3;
            if (isDraggingThis)
                iconColor = ImGuiColors.WarningForeground;
            if (hasChildren && !isExpanded)
                iconColor *= new Vector4(1, 1, 1, 0.4f);
            using (ImRaii.PushFont(UiBuilder.IconFontFixedWidth))
            using (ImRaii.PushColor(ImGuiCol.Text, iconColor)) {
                ImGui.Text(FontAwesomeIcon.LayerGroup.ToIconString());
            }
        }

        ImGui.SameLine();

        var color = Vector4.One;
        var useColor = false;
        if (!allowDrop) {
            color = ImGuiColors.DalamudGrey2;
            useColor = true;
        } else if (isDraggingThis) {
            color = ImGuiColors.WarningForeground;
            useColor = true;
        }
        using (ImRaii.PushColor(ImGuiCol.Text, color, useColor)) {
            ImGui.Text($" {node.Value.Name}");
        }

        if (isExpanded) {
            foreach (var child in node.Children) {
                DrawTreeNode(child, allowDrop && !isDraggingThis, ref treeAction, ref update);
            }
        }
    }

    private void DrawTreeNodeEnd(ref TreeAction? action) {
        using var color = new ImRaii.ColorDisposable()
            .Push(ImGuiCol.Text, Vector4.Zero)
            .Push(ImGuiCol.HeaderActive, Vector4.Zero)
            .Push(ImGuiCol.HeaderHovered, Vector4.Zero);
        using (ImRaii.TreeNode($"##treeNodeEndZone", ImGuiTreeNodeFlags.SpanAvailWidth | ImGuiTreeNodeFlags.FramePadding | ImGuiTreeNodeFlags.Leaf)) { }

        using (var drop = treeDragDrop.Drop(Guid.AllBitsSet, "")) {
            if (drop.Hovered) {
                treeDragDrop.PreviewInsertBefore(ImGui.GetItemRectMin(), ImGui.GetItemRectMax());
            } else if (drop.Dropped) {
                action = new TreeAction.PlaceEnd(treeDragDrop.SourceId);
            }
        }
    }

    private static bool TreeNodeClicked() {
        if (ImGui.IsItemClicked() && ImGui.IsItemToggledOpen()) {
            ImGuiP.ClearActiveID();
            ImGuiP.ClearDragDrop();
            return false;
        }
        if (ImGui.IsItemDeactivated()) {
            return !ImGuiP.IsMouseDragPastThreshold(ImGuiMouseButton.Left);
        }
        return false;
    }

    public abstract record TreeAction {
        public record Activate(Guid Id) : TreeAction;
        public record SetParent(Guid Id, Guid ParentId) : TreeAction;
        public record PlaceBefore(Guid Id, Guid SiblingId) : TreeAction;
        public record PlaceAfter(Guid Id, Guid SiblingId) : TreeAction;
        public record PlaceStart(Guid Id) : TreeAction;
        public record PlaceEnd(Guid Id) : TreeAction;
    }

    private void DrawLayoutManagerTreeButtons(ref bool layoutChanged, ref bool update) {
        ImGui.Separator();

        var topButtonWidth = (ImGui.GetContentRegionAvail().X - (ImGui.GetStyle().ItemSpacing.X * 1)) / 2;
        var topButtonSize = new Vector2(topButtonWidth, ImGui.GetFrameHeight());

        if (ImGuiExt.IconButtonWithCenteredText(FontAwesomeIcon.FileCirclePlus, "Create", topButtonSize, centerIcon: true)) {
            ImGui.OpenPopup(Popups.AddLayout);
        }
        ImGuiExt.HoverTooltip("Create a new layout");
        SetUpAddLayoutPopup(ref update, ref layoutChanged);

        ImGui.SameLine();
        if (ImGuiExt.IconButtonWithCenteredText(FontAwesomeIcon.FileImport, "Import", topButtonSize, centerIcon: true)) {
            ImGui.OpenPopup(Popups.ImportLayout);
        }
        ImGuiExt.HoverTooltip("Import a layout from an in-game HUD slot or the clipboard");
        SetUpImportLayoutPopup(ref update, ref layoutChanged);

        if (ImGuiExt.IconButtonWithCenteredText(FontAwesomeIcon.ListUl, "Compact view", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight()), centerIcon: true)) {
            Plugin.Config.UseLayoutListTreeView = false;
            update = true;
        }
        ImGuiExt.HoverTooltip("Use compact layout view");
    }
}
