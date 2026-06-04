using Dalamud.Interface;
using HUDManager.Configuration;
using HUDManager.Structs.External;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HUDManager.Ui.Editor.Tabs.External;

public sealed class Browsingway : IExternalElement
{
    private readonly Plugin _plugin;

    public Browsingway(Plugin plugin)
    {
        _plugin = plugin;
    }

    public bool Available() => _plugin.Interface.InstalledPlugins.Any(state => state is { Name: "Browsingway" });

    public void AddButtonToList(SavedLayout layout, ref bool update, bool avail)
    {
        if (ImGuiExt.IconButton(FontAwesomeIcon.Plus, "uimanager-add-browsingway")) {
            layout.BrowsingwayOverlays.Add(new BrowsingwayOverlay());
        }

        ImGui.SameLine();

        if (avail) {
            ImGui.Text("Browsingway");
        } else {
            ImGui.TextDisabled("Browsingway (not installed)");
        }

        ImGui.SameLine();
        ImGuiExt.HelpMarker("Install the Browsingway plugin before use. You can set up changes to Browsingway overlays using this menu.");
    }
    public void DrawControls(SavedLayout layout, ref bool update)
    {
        List<BrowsingwayOverlay> toRemove = new();

        foreach (var (overlay, i) in layout.BrowsingwayOverlays.Select((overlay, i) => (overlay, i))) {
            if (!ImGui.CollapsingHeader($"Browsingway: {overlay.CommandName}###bw-overlay-{i}")) {
                continue;
            }

            const ImGuiTableFlags flags = ImGuiTableFlags.BordersInner
                | ImGuiTableFlags.PadOuterX
                | ImGuiTableFlags.SizingFixedFit
                | ImGuiTableFlags.RowBg;

            using var table = ImRaii.Table($"bw-overlay-table-{i}", 3, flags);
            if (!table) continue;

            ImGui.TableSetupColumn("Enabled");
            ImGui.TableSetupColumn("Setting");
            ImGui.TableSetupColumn("Control", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableHeadersRow();

            ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X * 3);
            if (ImGuiExt.IconButtonEnabledWhen(ImGui.GetIO().KeyCtrl, FontAwesomeIcon.TrashAlt, $"bw-overlay-remove-{i}")) {
                toRemove.Add(overlay);
                update = true;
            }

            ImGuiExt.HoverTooltip("Remove this element from this layout (hold Control to allow)");

            ImGui.TableNextRow();

            static void DrawSettingName(string name)
            {
                ImGui.TextUnformatted(name);
                ImGui.TableNextColumn();
            }

            void DrawEnabledCheckbox(BrowsingwayOverlay.BrowsingwayOverlayComponent component, ref bool update1,
                bool nextCol = true)
            {
                if (nextCol) {
                    ImGui.TableNextColumn();
                }

                var enabled = overlay[component];
                if (ImGui.Checkbox($"###bw-{component}-enabled-{i}", ref enabled)) {
                    overlay[component] = enabled;
                    update1 = true;
                }

                ImGui.TableNextColumn();
            }

            // Name setting
            ImGui.TableSetColumnIndex(1);
            DrawSettingName("Name");
            if (ImGui.InputText($"###bw-name-{i}", ref overlay.CommandName, 128)) {
                update = true;
            }

            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);

            void DrawSettingRow(BrowsingwayOverlay.BrowsingwayOverlayComponent component, string settingName,
                ref bool setting, ref bool update2)
            {
                ImGui.TableSetColumnIndex(0);

                DrawEnabledCheckbox(component, ref update2, false);
                DrawSettingName(settingName);
                if (ImGui.Checkbox($"###bw-{settingName}-{i}", ref setting)) {
                    update2 = true;
                }

                ImGui.TableNextRow();
            }

            DrawSettingRow(BrowsingwayOverlay.BrowsingwayOverlayComponent.Hidden, "Hidden", ref overlay.Hidden, ref update);
            DrawSettingRow(BrowsingwayOverlay.BrowsingwayOverlayComponent.Locked, "Locked", ref overlay.Locked, ref update);
            DrawSettingRow(BrowsingwayOverlay.BrowsingwayOverlayComponent.Typethrough, "Typethrough", ref overlay.Typethrough, ref update);
            DrawSettingRow(BrowsingwayOverlay.BrowsingwayOverlayComponent.Clickthrough, "Clickthrough", ref overlay.Clickthrough, ref update);
        }

        foreach (var overlay in toRemove) {
            layout.BrowsingwayOverlays.Remove(overlay);
        }
    }

}
