using Dalamud.Interface;
using Dalamud.Bindings.ImGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace HUDManager.Ui;

public class Swaps
{
    private Plugin Plugin { get; }

    private int _editingConditionIndex = -1;
    private HudConditionMatch? _editingCondition;
    private bool _scrollToAdd;

    private (CustomConditions window, bool isOpen) _customConditionsMenu;

    public Swaps(Plugin plugin)
    {
        Plugin = plugin;
        _customConditionsMenu = (new CustomConditions(plugin), false);
    }

    internal void Draw()
    {
        if (!ImGui.BeginTabItem("Swapper")) {
            return;
        }

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
        if (ImGui.BeginCombo("Staging slot", staging)) {
            foreach (HudSlot slot in Enum.GetValues(typeof(HudSlot))) {
                if (!ImGui.Selectable(((int)slot + 1).ToString())) {
                    continue;
                }

                Plugin.Config.StagingSlot = slot;
                Plugin.Config.Save();
            }

            ImGui.EndCombo();
        }

        ImGui.SameLine();
        ImGuiExt.HelpMarker("The staging slot is the HUD layout slot that will be used as your HUD layout. All changes will be written to this slot when swaps are enabled.");

        ImGui.Separator();

        if (Plugin.Config.Layouts.Count == 0) {
            ImGui.TextUnformatted("Create at least one layout to begin setting up swaps.");
        } else {
            if (!Plugin.Config.DisableHelpPanels) {
                ImGui.TextWrapped("Add swap conditions below.\nThe conditions are checked from top to bottom.\nThe first condition that is satisfied will be the layout that is used.");
                if (Plugin.Config.AdvancedSwapMode) {
                    ImGui.TextWrapped("Setting a row to \"layer\" mode will cause it to be applied on top of the first non-layer condition.");
                }
                ImGui.Separator();
            }

            DrawConditionsTable();
        }

        ImGui.EndTabItem();
    }

    private void DrawConditionsTable()
    {
        ImGui.PushFont(UiBuilder.IconFont);
        var height = ImGui.GetContentRegionAvail().Y - ImGui.GetTextLineHeightWithSpacing() - ImGui.GetStyle().ItemInnerSpacing.Y;
        ImGui.PopFont();
        if (!ImGui.BeginChild("##conditions-table", new Vector2(-1, height))) {
            return;
        }

        var update = false;

        const ImGuiTableFlags flags = ImGuiTableFlags.Borders
            & ~ImGuiTableFlags.BordersOuterV
            | ImGuiTableFlags.PadOuterX
            | ImGuiTableFlags.RowBg;

        var advancedMode = Plugin.Config.AdvancedSwapMode;
        var columns = Plugin.Config.AdvancedSwapMode ? 6 : 5;

        if (!ImGui.BeginTable("uimanager-swaps-table", columns, flags)) {
            return;
        }

        var conditions = new List<HudConditionMatch>(Plugin.Config.HudConditionMatches);
        if (_editingConditionIndex == conditions.Count) {
            conditions.Add(new HudConditionMatch());
        }

        if (advancedMode)
            ImGui.TableSetupColumn("Layer", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Class/Job");
        ImGui.TableSetupColumn("State");
        ImGui.TableSetupColumn("Layout");
        ImGui.TableSetupColumn("Options");
        ImGui.TableSetupColumn("Active", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableHeadersRow();

        var addCondition = false;
        var actionedItemIndex = -1;
        var action = 0; // 0 for delete, otherwise move.
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
                    if (ImGui.Checkbox($"##condition-layered-{item.i}", ref applyLayer)) {
                        _editingCondition.IsLayer = applyLayer;
                        update = true;
                    }

                    ImGui.TableNextColumn();
                }

                // Column: Job

                ImGui.PushItemWidth(-1);
                if (ImGui.BeginCombo("##condition-edit-job", jobDisplayName)) {
                    if (ImGui.Selectable("Any##condition-edit-job")) {
                        _editingCondition.ClassJobCategory = null;
                    }

                    foreach (var group in ClassJobCategoryIdExtensions.ClassJobCategoryGroupings) {
                        ImGui.Selectable("⸻⸻", false, ImGuiSelectableFlags.Disabled);
                        foreach (var classJobCat in group)
                        {
                            if (ImGui.Selectable($"{classJobCat.DisplayName(Plugin)}##condition-edit-job")) {
                                _editingCondition.ClassJobCategory = classJobCat;
                            }
                        }
                    }

                    ImGui.EndCombo();
                }

                ImGui.PopItemWidth();
                ImGui.TableNextColumn();

                // Column: Status/Custom condition

                var statusDisplayName = _editingCondition.Status?.Name() ?? _editingCondition.CustomCondition?.DisplayName;

                ImGui.PushItemWidth(-1);
                if (ImGui.BeginCombo("##condition-edit-status", statusDisplayName ?? "Any")) {
                    if (ImGui.Selectable("Any##condition-edit-status")) {
                        _editingCondition.Status = null;
                    }

                    foreach (Status status in Enum.GetValues(typeof(Status))) {
                        if (ImGui.Selectable($"{status.Name()}##condition-edit-status")) {
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

                    ImGui.EndCombo();
                }

                ImGui.PopItemWidth();
                ImGui.TableNextColumn();

                ImGui.PushItemWidth(-1);
                var comboPreview = _editingCondition.LayoutId == Guid.Empty ? string.Empty : Plugin.Config.Layouts[_editingCondition.LayoutId].Name;
                if (ImGui.BeginCombo("##condition-edit-layout", comboPreview)) {
                    foreach (var layout in Plugin.Config.Layouts) {
                        if (ImGui.Selectable($"{layout.Value.Name}##condition-edit-layout-{layout.Key}")) {
                            _editingCondition.LayoutId = layout.Key;
                        }
                    }

                    ImGui.EndCombo();
                }

                ImGui.PopItemWidth();
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
                        ImGui.PushFont(UiBuilder.IconFont);
                        ImGuiExt.CenterColumnText(FontAwesomeIcon.Check.ToIconString());
                        ImGui.PopFont();
                    }
                    ImGui.TableNextColumn();
                }

                // Column: Job

                var jobDisplayName = item.cond.ClassJobCategory?.DisplayName(Plugin) ?? String.Empty;

                ImGui.TextUnformatted(jobDisplayName);
                ImGui.TableNextColumn();

                // Column: Status/Custom condition

                var statusDisplayName = item.cond.Status?.Name() ?? item.cond.CustomCondition?.DisplayName;

                ImGui.TextUnformatted(statusDisplayName ?? string.Empty);
                ImGui.TableNextColumn();

                // Column: Layout

                Plugin.Config.Layouts.TryGetValue(item.cond.LayoutId, out var condLayout);
                ImGui.TextUnformatted(condLayout?.Name ?? string.Empty);
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
                        ImGuiExt.CenterColumnText(activeText);
                    }
                }
            }
        }

        ImGui.EndTable();

        if (ImGuiExt.IconButton(FontAwesomeIcon.Plus, "condition")) {
            _editingConditionIndex = Plugin.Config.HudConditionMatches.Count;
            _editingCondition = new HudConditionMatch();
            _scrollToAdd = true;
        } else if (ImGui.IsItemHovered()) {
            ImGui.SetTooltip("Add a new swap condition");
        }

        ImGui.EndChild();

        ImGui.Indent();

        if (ImGuiExt.IconButton(FontAwesomeIcon.Flag, "customconditions")) {
            _customConditionsMenu.isOpen = true;
        } else if (ImGui.IsItemHovered()) {
            ImGui.SetTooltip("Open the Custom Conditions menu");
        }

        if (_customConditionsMenu.isOpen)
            _customConditionsMenu.window.Draw(ref _customConditionsMenu.isOpen);

        ImGui.SameLine();

        if (ImGui.Checkbox("Advanced mode##swap-advanced-check", ref advancedMode)) {
            Plugin.Config.AdvancedSwapMode = advancedMode;
            Plugin.Config.Save();
            update = true;
        }

        if (addCondition) {
            update = true;
            if (_editingConditionIndex == Plugin.Config.HudConditionMatches.Count && _editingCondition != null) {
                Plugin.Config.HudConditionMatches.Add(_editingCondition);
            } else if (_editingCondition != null) {
                Plugin.Config.HudConditionMatches.RemoveAt(_editingConditionIndex);
                Plugin.Config.HudConditionMatches.Insert(_editingConditionIndex, _editingCondition);
            }

            Plugin.Config.Save();
            _editingConditionIndex = -1;
        }

        if (actionedItemIndex >= 0) {
            update = true;
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

            Plugin.Config.Save();
        }

        if (!update) {
            return;
        }

        var player = Plugin.ClientState.LocalPlayer;
        if (player == null || !Plugin.Config.SwapsEnabled) {
            return;
        }

        Plugin.Statuses.Update();
        Plugin.Statuses.SetHudLayout();
    }
}
