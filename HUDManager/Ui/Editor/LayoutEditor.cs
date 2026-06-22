using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using HUDManager.Configuration;
using HUDManager.Tree;
using HUDManager.Ui.DragDrop;
using HUDManager.Ui.Editor.Tabs;
using System;
using System.Numerics;

namespace HUDManager.Ui.Editor;

public partial class LayoutEditor {
    internal readonly ElementDragDrop ElementDragDrop;

    private Plugin Plugin { get; }
    private Interface Ui { get; }
    internal Previews Previews { get; }
    private HudElements HudElements { get; }
    private WindowElements Windows { get; }
    private ExternalElements ExternalElements { get; }

    private string? RenameLayoutName { get; set; }
    private string? NewLayoutName { get; set; }
    private string? ImportLayoutName { get; set; }

    public LayoutEditor(Plugin plugin, Interface ui) {
        Plugin = plugin;
        Ui = ui;

        ElementDragDrop = new ElementDragDrop("ELEMENT");

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
            ImGui.Text("Note that swaps are disabled while this menu is open.");
            ImGui.Separator();
        }

        Previews.Draw(ref update);

        var nodes = Node<SavedLayout>.BuildTree(Plugin.Config.Layouts);

        if (Plugin.Config.UseLayoutListTreeView) {
            DrawLayoutManagerTreeView(nodes, ref layoutChanged, ref update);
        } else {
            DrawLayoutManagerInlineView(nodes, ref layoutChanged, ref update);
        }

        ElementDragDrop.EndFrame();

        if (layoutChanged) {
            // Kill all previews so they don't fuck up the new layout.
            Previews.Clear();
        }
        if (update) {
            if (Plugin.PlayerState.IsLoaded) {
                Plugin.Hud.WriteEffectiveLayout(Plugin.Config.StagingSlot, Ui.SelectedLayout);
                Plugin.Hud.SelectSlot(Plugin.Config.StagingSlot, true);
            }

            Plugin.Config.Save();
        }
    }

    private void DrawLayoutContents(ref bool update) {
        if (Ui.SelectedLayout == Guid.Empty)
            return;

        DrawMainSection(ref update);

        ImGui.Separator();
        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Cog, "Positioning settings")) {
            ImGui.OpenPopup(Popups.LayoutEditorOptions);
        }

        SetUpOptionsPopup(ref update);
    }

    private void DrawMainSection(ref bool update) {
        using var editorChild = ImRaii.Child("##layout-editor-main", new Vector2(-1, -GetTableButtonSpace(1)), false);
        if (!editorChild) return;

        var layout = Plugin.Config.Layouts[Ui.SelectedLayout];

        using var tabBar = ImRaii.TabBar("uimanager-positioning");
        if (!tabBar) return;

        using (var tabItem = ImRaii.TabItem("HUD Elements")) {
            if (tabItem)
                HudElements.Draw(layout, ref update);
        }

        using (var tabItem = ImRaii.TabItem("Windows")) {
            if (tabItem)
                Windows.Draw(layout, ref update);
        }

        using (var tabItem = ImRaii.TabItem("External Elements")) {
            if (tabItem)
                ExternalElements.Draw(layout, ref update);
        }
    }

    private static float GetTableButtonSpace(int rows) {
        return ImGui.GetFrameHeight() * rows
               + ImGui.GetStyle().ItemSpacing.Y * (1 + rows);
    }
}
