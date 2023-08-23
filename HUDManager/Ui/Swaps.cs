using Dalamud.Interface;
using HUD_Manager.Structs;
using HUDManager;
using HUDManager.Configuration;
using HUDManager.Ui;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace HUD_Manager.Ui
{
    public class Swaps
    {
        private Plugin Plugin { get; }

        private int _editingConditionIndex = -1;
        private HudConditionMatch? _editingCondition;
        private bool _scrollToAdd;

        private (CustomConditions window, bool isOpen) _customConditionsMenu;

        public Swaps(Plugin plugin)
        {
            this.Plugin = plugin;
            this._customConditionsMenu = (new CustomConditions(plugin), false);
        }

        internal void Draw()
        {
            if (!ImGui.BeginTabItem("Swapper")) {
                return;
            }

            var enabled = this.Plugin.Config.SwapsEnabled;
            if (ImGui.Checkbox("Enable swaps", ref enabled)) {
                this.Plugin.Config.SwapsEnabled = enabled;
                this.Plugin.Config.Save();

                this.Plugin.Statuses.Update();
                this.Plugin.Statuses.SetHudLayout();
            }

            ImGui.Spacing();
            var staging = ((int)this.Plugin.Config.StagingSlot + 1).ToString();
            if (ImGui.BeginCombo("Staging slot", staging)) {
                foreach (HudSlot slot in Enum.GetValues(typeof(HudSlot))) {
                    if (!ImGui.Selectable(((int)slot + 1).ToString())) {
                        continue;
                    }

                    this.Plugin.Config.StagingSlot = slot;
                    this.Plugin.Config.Save();
                }

                ImGui.EndCombo();
            }

            ImGui.SameLine();
            ImGuiExt.HelpMarker("The staging slot is the HUD layout slot that will be used as your HUD layout. All changes will be written to this slot when swaps are enabled.");

            ImGui.Separator();

            if (this.Plugin.Config.Layouts.Count == 0) {
                ImGui.TextUnformatted("Create at least one layout to begin setting up swaps.");
            } else {
                if (!Plugin.Config.DisableHelpPanels) {
                    ImGui.TextWrapped("Add swap conditions below.\nThe conditions are checked from top to bottom.\nThe first condition that is satisfied will be the layout that is used.");
                    if (Plugin.Config.AdvancedSwapMode) {
                        ImGui.TextWrapped("Setting a row to \"layer\" mode will cause it to be applied on top of the first non-layer condition.");
                    }
                    ImGui.Separator();
                }

                this.DrawConditionsTable();
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

            bool advancedMode = Plugin.Config.AdvancedSwapMode;
            int columns = Plugin.Config.AdvancedSwapMode ? 6 : 5;

            if (!ImGui.BeginTable("uimanager-swaps-table", columns, flags)) {
                return;
            }

            var conditions = new List<HudConditionMatch>(this.Plugin.Config.HudConditionMatches);
            if (this._editingConditionIndex == conditions.Count) {
                conditions.Add(new HudConditionMatch());
            }

            if (advancedMode)
                ImGui.TableSetupColumn("Layer", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Job");
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

                if (this._editingConditionIndex == item.i) {
                    // Editing in progress
                    this._editingCondition ??= new HudConditionMatch();

                    var jobDisplayName = this._editingCondition.ClassJobCategory?.DisplayName(Plugin) ?? "Any";

                    // Column: Layer

                    if (advancedMode) {
                        bool applyLayer = item.cond.IsLayer;
                        if (ImGui.Checkbox($"##condition-layered-{item.i}", ref applyLayer)) {
                            item.cond.IsLayer = applyLayer;
                            update = true;
                        }

                        ImGui.TableNextColumn();
                    }

                    // Column: Job

                    ImGui.PushItemWidth(-1);
                    if (ImGui.BeginCombo("##condition-edit-job", jobDisplayName)) {
                        if (ImGui.Selectable("Any##condition-edit-job")) {
                            this._editingCondition.ClassJobCategory = null;
                        }

                        foreach (var group in ClassJobCategoryIdExtensions.ClassJobCategoryGroupings) {
                            ImGui.Selectable($"⸻⸻", false, ImGuiSelectableFlags.Disabled);
                            foreach (var classJobCat in group)
                            {
                                if (ImGui.Selectable($"{classJobCat.DisplayName(Plugin)}##condition-edit-job")) {
                                    this._editingCondition.ClassJobCategory = classJobCat;
                                }
                            }
                        }

                        ImGui.EndCombo();
                    }

                    ImGui.PopItemWidth();
                    ImGui.TableNextColumn();

                    // Column: Status/Custom condition

                    var statusDisplayName = this._editingCondition.Status?.Name() ?? this._editingCondition.CustomCondition?.DisplayName;

                    ImGui.PushItemWidth(-1);
                    if (ImGui.BeginCombo("##condition-edit-status", statusDisplayName ?? "Any")) {
                        if (ImGui.Selectable("Any##condition-edit-status")) {
                            this._editingCondition.Status = null;
                        }

                        foreach (Status status in Enum.GetValues(typeof(Status))) {
                            if (ImGui.Selectable($"{status.Name()}##condition-edit-status")) {
                                this._editingCondition.CustomCondition = null;
                                this._editingCondition.Status = status;
                            }
                        }

                        foreach (var cond in Plugin.Config.CustomConditions) {
                            if (ImGui.Selectable($"{cond.DisplayName}##condition-edit-status")) {
                                this._editingCondition.CustomCondition = cond;
                                this._editingCondition.Status = null;
                            }
                        }

                        ImGui.EndCombo();
                    }

                    ImGui.PopItemWidth();
                    ImGui.TableNextColumn();

                    ImGui.PushItemWidth(-1);
                    var comboPreview = this._editingCondition.LayoutId == Guid.Empty ? string.Empty : this.Plugin.Config.Layouts[this._editingCondition.LayoutId].Name;
                    if (ImGui.BeginCombo("##condition-edit-layout", comboPreview)) {
                        foreach (var layout in this.Plugin.Config.Layouts) {
                            if (ImGui.Selectable($"{layout.Value.Name}##condition-edit-layout-{layout.Key}")) {
                                this._editingCondition.LayoutId = layout.Key;
                            }
                        }

                        ImGui.EndCombo();
                    }

                    ImGui.PopItemWidth();
                    ImGui.TableNextColumn();

                    if (this._editingCondition.LayoutId != Guid.Empty) {
                        if (ImGuiExt.IconButton(FontAwesomeIcon.Check, "condition-edit")) {
                            addCondition = true;
                        }

                        ImGui.SameLine();
                    }

                    if (ImGuiExt.IconButton(FontAwesomeIcon.Times, "condition-stop")) {
                        this._editingConditionIndex = -1;
                    }

                    if (this._scrollToAdd) {
                        this._scrollToAdd = false;
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

                    this.Plugin.Config.Layouts.TryGetValue(item.cond.LayoutId, out var condLayout);
                    ImGui.TextUnformatted(condLayout?.Name ?? string.Empty);
                    ImGui.TableNextColumn();

                    // Column: Actions

                    if (ImGuiExt.IconButton(FontAwesomeIcon.PencilAlt, $"{item.i}")) {
                        this._editingConditionIndex = item.i;
                        this._editingCondition = item.cond;
                    }

                    ImGui.SameLine();
                    if (ImGuiExt.IconButton(FontAwesomeIcon.Trash, $"{item.i}")) {
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
                        string activeText = string.Empty;
                        if (Plugin.Statuses.ResultantLayout.activeLayout == item.cond) {
                            if (Plugin.Statuses.ConditionHoldTimerIsTicking(item.cond)) {
                                activeText = "▼";
                            } else {
                                activeText = "★";
                            }
                        } else if (Plugin.Statuses.ResultantLayout.layeredLayouts.Contains(item.cond)) {
                            if (Plugin.Statuses.ConditionHoldTimerIsTicking(item.cond)) {
                                activeText = "▽";
                            } else {
                                activeText = "☆";
                            }
                        }
                        if (activeText != string.Empty) {
                            ImGuiExt.CenterColumnText(activeText);
                        }
                    }
                }
            }

            ImGui.EndTable();

            if (ImGuiExt.IconButton(FontAwesomeIcon.Plus, "condition")) {
                this._editingConditionIndex = this.Plugin.Config.HudConditionMatches.Count;
                this._editingCondition = new HudConditionMatch();
                this._scrollToAdd = true;
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
                if (this._editingConditionIndex == this.Plugin.Config.HudConditionMatches.Count && this._editingCondition != null) {
                    this.Plugin.Config.HudConditionMatches.Add(this._editingCondition);
                } else if (this._editingCondition != null) {
                    this.Plugin.Config.HudConditionMatches.RemoveAt(this._editingConditionIndex);
                    this.Plugin.Config.HudConditionMatches.Insert(this._editingConditionIndex, this._editingCondition);
                }

                this.Plugin.Config.Save();
                this._editingConditionIndex = -1;
            }

            if (actionedItemIndex >= 0) {
                update = true;
                if (action == 0) {
                    this.Plugin.Config.HudConditionMatches.RemoveAt(actionedItemIndex);
                } else {
                    if (actionedItemIndex + action >= 0 && actionedItemIndex + action < this.Plugin.Config.HudConditionMatches.Count) {
                        // Move the condition.
                        var c = this.Plugin.Config.HudConditionMatches[actionedItemIndex];
                        this.Plugin.Config.HudConditionMatches.RemoveAt(actionedItemIndex);
                        this.Plugin.Config.HudConditionMatches.Insert(actionedItemIndex + action, c);
                    }
                }

                this.Plugin.Config.Save();
            }

            if (!update) {
                return;
            }

            var player = this.Plugin.ClientState.LocalPlayer;
            if (player == null || !this.Plugin.Config.SwapsEnabled) {
                return;
            }

            this.Plugin.Statuses.Update();
            this.Plugin.Statuses.SetHudLayout();
        }
    }
}
