using Dalamud.Interface;
using HUD_Manager;
using HUD_Manager.Configuration;
using HUD_Manager.Ui;
using HUDManager.Structs.External;
using ImGuiNET;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace HUDManager.Ui.Editor.Tabs
{
    internal class ExternalElements
    {
        private Plugin plugin { get; init; }

        public ExternalElements(Plugin plugin)
        {
            this.plugin = plugin; 
        }

        internal void Draw(SavedLayout layout, ref bool update) {
            if (ImGuiExt.IconButton(FontAwesomeIcon.Plus, "uimanager-add-browsingway")) {
                layout.BrowsingwayOverlays.Add(new BrowsingwayOverlay());
            }

            ImGui.SameLine();
            ImGui.Text("Browsingway");
            ImGui.SameLine();
            ImGuiExt.HelpMarker("Install the Browsingway plugin before use. You can set up changes to Browsingway overlays using this menu.");

            if (!ImGui.BeginChild("uimanager-overlay-edit", new Vector2(0, 0), true)) {
                return;
            }

            List<BrowsingwayOverlay> toRemove = new();

            foreach (var (overlay, i) in layout.BrowsingwayOverlays.Select((overlay, i) => (overlay, i))) {

                if (!ImGui.CollapsingHeader($"{overlay.CommandName}###bw-overlay-{i}")) {
                    continue;
                }

                const ImGuiTableFlags flags = ImGuiTableFlags.BordersInner
                                              | ImGuiTableFlags.PadOuterX
                                              | ImGuiTableFlags.SizingFixedFit
                                              | ImGuiTableFlags.RowBg;
                if (!ImGui.BeginTable($"bw-overlay-table-{i}", 3, flags)) {
                    continue;
                }

                ImGui.TableSetupColumn("Enabled");
                ImGui.TableSetupColumn("Setting");
                ImGui.TableSetupColumn("Control", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableHeadersRow();

                ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X * 3);
                if (ImGuiExt.IconButton(FontAwesomeIcon.TrashAlt, $"bw-overlay-remove-{i}")) {
                    toRemove.Add(overlay);
                    update = true;
                }

                ImGuiExt.HoverTooltip("Remove this element from this layout");

                ImGui.TableNextRow();

                static void DrawSettingName(string name)
                {
                    ImGui.TextUnformatted(name);
                    ImGui.TableNextColumn();
                }

                void DrawEnabledCheckbox(BrowsingwayOverlay.BrowsingwayOverlayComponent component, ref bool update, bool nextCol = true)
                {
                    if (nextCol) {
                        ImGui.TableNextColumn();
                    }

                    var enabled = overlay[component];
                    if (ImGui.Checkbox($"###bw-{component}-enabled-{i}", ref enabled)) {
                        overlay[component] = enabled;
                        update = true;
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

                void DrawSettingRow(BrowsingwayOverlay.BrowsingwayOverlayComponent component, string settingName, ref bool setting, ref bool update)
                {
                    ImGui.TableSetColumnIndex(0);

                    DrawEnabledCheckbox(component, ref update, false);
                    DrawSettingName(settingName);
                    if (ImGui.Checkbox($"###bw-{settingName}-{i}", ref setting)) {
                        update = true;
                    }
                    ImGui.TableNextRow();
                }

                DrawSettingRow(BrowsingwayOverlay.BrowsingwayOverlayComponent.Hidden, "Hidden", ref overlay.Hidden, ref update);
                DrawSettingRow(BrowsingwayOverlay.BrowsingwayOverlayComponent.Locked, "Locked", ref overlay.Locked, ref update);
                DrawSettingRow(BrowsingwayOverlay.BrowsingwayOverlayComponent.Typethrough, "Typethrough", ref overlay.Typethrough, ref update);
                DrawSettingRow(BrowsingwayOverlay.BrowsingwayOverlayComponent.Clickthrough, "Clickthrough", ref overlay.Clickthrough, ref update);

                ImGui.EndTable();
            }

            foreach (var overlay in toRemove) {
                layout.BrowsingwayOverlays.Remove(overlay);
            }

            ImGui.EndChild();
        }
    }
}
