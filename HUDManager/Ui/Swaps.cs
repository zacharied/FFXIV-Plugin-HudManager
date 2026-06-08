using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace HUDManager.Ui;

public class Swaps {
    private Plugin Plugin { get; }

    private int _editingConditionIndex = -1;
    private HudConditionMatch? _editingCondition;
    private bool _scrollToAdd;

    public Swaps(Plugin plugin) {
        Plugin = plugin;
    }

    internal void Draw() {
        using var tabItem = ImRaii.TabItem("Swapper");
        if (!tabItem) return;

        var enabled = Plugin.Config.SwapsEnabled;
        if (ImGui.Checkbox("Enable swaps", ref enabled)) {
            Plugin.Config.SwapsEnabled = enabled;
            Plugin.Config.Save();

            Plugin.Statuses.NeedsForceUpdate = Statuses.ForceState.SwapSettingChanged;
            Plugin.Statuses.Update();
            Plugin.Statuses.SetHudLayout();
        }

        ImGui.Spacing();
        var staging = ((int)Plugin.Config.StagingSlot + 1).ToString();
        using (var combo = ImRaii.Combo("Staging slot", staging)) {
            if (combo) {
                foreach (var slot in Enum.GetValues<HudSlot>()) {
                    if (!ImGui.Selectable(((int)slot + 1).ToString())) {
                        continue;
                    }

                    Plugin.Config.StagingSlot = slot;
                    Plugin.Config.Save();
                }
            }
        }

        ImGui.SameLine();
        ImGuiExt.HelpMarker("The staging slot is the HUD layout slot that will be used as your HUD layout. All changes will be written to this slot when swaps are enabled.");

        ImGui.Separator();

        if (Plugin.Config.Layouts.Count == 0) {
            ImGui.TextUnformatted("Create at least one layout to begin setting up swaps.");
            return;
        }

        if (!Plugin.Config.DisableHelpPanels) {
            ImGui.TextWrapped("Add swap conditions below.\nThe conditions are checked from top to bottom.\nThe first condition that is satisfied will be the layout that is used.");
            if (Plugin.Config.AdvancedSwapMode) {
                ImGui.TextWrapped("Setting a row to \"layer\" mode will cause it to be applied on top of the first non-layer condition.");
            }
            ImGui.Separator();
        }

        var height = ImGui.GetContentRegionAvail().Y - ImGui.GetFrameHeight() - ImGui.GetStyle().ItemSpacing.Y;

        var update = false;

        using (var child = ImRaii.Child("##conditions-table", new Vector2(-1, height))) {
            if (child) {
                DrawConditionTable(ref update);

                if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Plus, "Add swap condition")) {
                    _editingConditionIndex = Plugin.Config.HudConditionMatches.Count;
                    _editingCondition = new HudConditionMatch();
                    _scrollToAdd = true;
                } else if (ImGui.IsItemHovered()) {
                    ImGui.SetTooltip("Add a new swap condition");
                }
            }
        }

        ImGui.Indent();

        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Flag, "Custom conditions")) {
            Plugin.WindowManager.CustomConditions.Toggle();
        } else if (ImGui.IsItemHovered()) {
            ImGui.SetTooltip("Open the Custom Conditions menu");
        }

        ImGui.SameLine();

        var advancedMode = Plugin.Config.AdvancedSwapMode;
        if (ImGui.Checkbox("Advanced mode##swap-advanced-check", ref advancedMode)) {
            Plugin.Config.AdvancedSwapMode = advancedMode;
            update = true;
        }

        if (update) {
            Plugin.Config.Save();

            if (Plugin.ObjectTable.LocalPlayer != null && Plugin.Config.SwapsEnabled) {
                Plugin.Statuses.Update();
                Plugin.Statuses.SetHudLayout();
            }
        }
    }

    private void DrawConditionTable(ref bool update) {
        var columns = Plugin.Config.AdvancedSwapMode ? 6 : 5;
        using var table = ImRaii.Table("uimanager-swaps-table", columns, (ImGuiTableFlags.Borders & ~ImGuiTableFlags.BordersOuterV) | ImGuiTableFlags.PadOuterX | ImGuiTableFlags.RowBg);
        if (!table) return;

        var advancedMode = Plugin.Config.AdvancedSwapMode;
        var addCondition = false;
        var actionedItemIndex = -1;
        var action = 0; // 0 for delete, otherwise move.

        var conditions = new List<HudConditionMatch>(Plugin.Config.HudConditionMatches);
        if (_editingConditionIndex == conditions.Count) {
            conditions.Add(new HudConditionMatch());
        }

        var width = ImGuiExt.IconButtonWidth(FontAwesomeIcon.PencilAlt)
                    + ImGuiExt.IconButtonWidth(FontAwesomeIcon.TrashAlt)
                    + ImGuiExt.IconButtonWidth(FontAwesomeIcon.ArrowUp)
                    + ImGuiExt.IconButtonWidth(FontAwesomeIcon.ArrowDown)
                    + ImGui.GetStyle().ItemSpacing.X * 3;

        if (advancedMode)
            ImGui.TableSetupColumn("Layer", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Class/Job");
        ImGui.TableSetupColumn("State");
        ImGui.TableSetupColumn("Layout");
        ImGui.TableSetupColumn("Options", ImGuiTableColumnFlags.WidthFixed, width);
        ImGui.TableSetupColumn("Active", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableHeadersRow();

        foreach (var item in conditions.Select((cond, i) => new { cond, i })) {
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);

            if (_editingConditionIndex == item.i) {
                // Editing in progress
                _editingCondition ??= new HudConditionMatch();

                var jobDisplayName = _editingCondition.ClassJobCategory?.DisplayName(Plugin) ?? "Any";

                // Column: Layer

                if (advancedMode) {
                    var applyLayer = _editingCondition.IsLayer;

                    ImCursor.ToNestedRect(new Vector2(ImGui.GetFrameHeight(), 0), new Vector2(ImGui.GetColumnWidth(), 0), ImAlign.Top);
                    if (ImGui.Checkbox($"##condition-layered-{item.i}", ref applyLayer)) {
                        _editingCondition.IsLayer = applyLayer;
                        update = true;
                    }

                    ImGui.TableNextColumn();
                }

                // Column: Job

                using (ImRaii.ItemWidth(-1))
                using (var combo = ImRaii.Combo("##condition-edit-job", jobDisplayName)) {
                    if (combo) {
                        if (ImGui.Selectable("Any##condition-edit-job")) {
                            _editingCondition.ClassJobCategory = null;
                        }

                        foreach (var group in ClassJobCategoryIdExtensions.ClassJobCategoryGroupings) {
                            ImGui.Selectable("⸻⸻", false, ImGuiSelectableFlags.Disabled);
                            foreach (var classJobCat in group) {
                                if (ImGui.Selectable($"{classJobCat.DisplayName(Plugin)}##condition-edit-job")) {
                                    _editingCondition.ClassJobCategory = classJobCat;
                                }
                            }
                        }
                    }
                }

                ImGui.TableNextColumn();

                // Column: Status/Custom condition

                var statusDisplayName = _editingCondition.Status?.GetDisplayName() ?? _editingCondition.CustomCondition?.DisplayName;

                using (ImRaii.ItemWidth(-1))
                using (var combo = ImRaii.Combo("##condition-edit-status", statusDisplayName ?? "Any")) {
                    if (combo) {
                        if (ImGui.Selectable("Any##condition-edit-status")) {
                            _editingCondition.Status = null;
                        }

                        foreach (var status in Enum.GetValues<Status>()) {
                            if (ImGui.Selectable($"{status.GetDisplayName()}##condition-edit-status")) {
                                _editingCondition.CustomCondition = null;
                                _editingCondition.Status = status;
                            }
                        }

                        foreach (var cond in Plugin.Config.CustomConditions) {
                            if (ImGui.Selectable($"{cond.DisplayName}##condition-edit-status")) {
                                _editingCondition.CustomCondition = cond;
                                _editingCondition.Status = null;
                            }
                        }
                    }
                }

                ImGui.TableNextColumn();

                var comboPreview = _editingCondition.LayoutId == Guid.Empty ? string.Empty : Plugin.Config.Layouts[_editingCondition.LayoutId].Name;
                using (ImRaii.ItemWidth(-1))
                using (var combo = ImRaii.Combo("##condition-edit-layout", comboPreview)) {
                    if (combo) {
                        foreach (var layout in Plugin.Config.Layouts) {
                            if (ImGui.Selectable($"{layout.Value.Name}##condition-edit-layout-{layout.Key}")) {
                                _editingCondition.LayoutId = layout.Key;
                            }
                        }
                    }
                }

                ImGui.TableNextColumn();

                if (_editingCondition.LayoutId != Guid.Empty) {
                    if (ImGuiExt.IconButton(FontAwesomeIcon.Check, "condition-edit")) {
                        addCondition = true;
                    }

                    ImGui.SameLine();
                }

                if (ImGuiExt.IconButton(FontAwesomeIcon.Times, "condition-stop")) {
                    _editingConditionIndex = -1;
                }

                if (_scrollToAdd) {
                    _scrollToAdd = false;
                    ImGui.SetScrollHereY();
                }

                ImGui.TableNextColumn();
            } else {
                // Column: Layer

                if (advancedMode) {
                    if (item.cond.IsLayer) {
                        using (ImRaii.PushFont(UiBuilder.IconFont)) {
                            var text = FontAwesomeIcon.Check.ToIconString();
                            ImCursor.ToNestedRect(ImGui.CalcTextSize(text), new Vector2(ImGui.GetColumnWidth(), 0), ImAlign.Top);
                            ImGui.AlignTextToFramePadding();
                            ImGui.Text(text);
                        }
                    }
                    ImGui.TableNextColumn();
                }

                // Column: Job

                var jobDisplayName = item.cond.ClassJobCategory?.DisplayName(Plugin) ?? String.Empty;

                ImGui.AlignTextToFramePadding();
                ImGui.Text(jobDisplayName);
                ImGui.TableNextColumn();

                // Column: Status/Custom condition

                var statusDisplayName = item.cond.Status?.GetDisplayName() ?? item.cond.CustomCondition?.DisplayName;

                ImGui.AlignTextToFramePadding();
                ImGui.Text(statusDisplayName ?? string.Empty);
                ImGui.TableNextColumn();

                // Column: Layout

                Plugin.Config.Layouts.TryGetValue(item.cond.LayoutId, out var condLayout);
                ImGui.AlignTextToFramePadding();
                ImGui.Text(condLayout?.Name ?? string.Empty);
                ImGui.TableNextColumn();

                // Column: Actions

                if (ImGuiExt.IconButton(FontAwesomeIcon.PencilAlt, $"{item.i}")) {
                    _editingConditionIndex = item.i;
                    _editingCondition = item.cond.Clone();
                }

                ImGui.SameLine();
                if (ImGuiExt.IconButton(FontAwesomeIcon.TrashAlt, $"{item.i}")) {
                    actionedItemIndex = item.i;
                }

                ImGui.SameLine();
                if (ImGuiExt.IconButton(FontAwesomeIcon.ArrowUp, $"{item.i}")) {
                    actionedItemIndex = item.i;
                    action = -1;
                }

                ImGui.SameLine();
                if (ImGuiExt.IconButton(FontAwesomeIcon.ArrowDown, $"{item.i}")) {
                    actionedItemIndex = item.i;
                    action = 1;
                }

                // Column: Active

                ImGui.TableNextColumn();
                if (Plugin.Config.SwapsEnabled) {
                    var activeText = string.Empty;
                    if (Plugin.Statuses.ResultantLayout.activeLayout == item.cond) {
                        activeText = Plugin.Statuses.ConditionHoldTimerIsTicking(item.cond) ? "▼" : "★";
                    } else if (Plugin.Statuses.ResultantLayout.layeredLayouts.Contains(item.cond)) {
                        activeText = Plugin.Statuses.ConditionHoldTimerIsTicking(item.cond) ? "▽" : "☆";
                    }
                    if (activeText != string.Empty) {
                        ImCursor.ToNestedRect(ImGui.CalcTextSize(activeText), new Vector2(ImGui.GetColumnWidth(), 0), ImAlign.Top);
                        ImGui.AlignTextToFramePadding();
                        ImGui.Text(activeText);
                    }
                }
            }
        }

        if (addCondition) {
            if (_editingConditionIndex == Plugin.Config.HudConditionMatches.Count && _editingCondition != null) {
                Plugin.Config.HudConditionMatches.Add(_editingCondition);
            } else if (_editingCondition != null) {
                Plugin.Config.HudConditionMatches.RemoveAt(_editingConditionIndex);
                Plugin.Config.HudConditionMatches.Insert(_editingConditionIndex, _editingCondition);
            }
            _editingConditionIndex = -1;
            update = true;
        }

        if (actionedItemIndex >= 0) {
            if (action == 0) {
                Plugin.Config.HudConditionMatches.RemoveAt(actionedItemIndex);
            } else {
                if (actionedItemIndex + action >= 0 && actionedItemIndex + action < Plugin.Config.HudConditionMatches.Count) {
                    // Move the condition.
                    var c = Plugin.Config.HudConditionMatches[actionedItemIndex];
                    Plugin.Config.HudConditionMatches.RemoveAt(actionedItemIndex);
                    Plugin.Config.HudConditionMatches.Insert(actionedItemIndex + action, c);
                }
            }
            update = true;
        }
    }
}
