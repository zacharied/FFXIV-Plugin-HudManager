using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace HUD_Manager.Ui {
    public class Swaps {
        private Plugin Plugin { get; }

        private int _editingConditionIndex = -1;
        private HudConditionMatch? _editingCondition;
        private bool _scrollToAdd;

        public Swaps(Plugin plugin) {
            this.Plugin = plugin;
        }

        internal void Draw() {
            if (!ImGui.BeginTabItem("Swaps")) {
                return;
            }

            var enabled = this.Plugin.Config.SwapsEnabled;
            if (ImGui.Checkbox("Enable swaps", ref enabled)) {
                this.Plugin.Config.SwapsEnabled = enabled;
                this.Plugin.Config.Save();

                this.Plugin.Statuses.SetHudLayout(this.Plugin.Interface.ClientState.LocalPlayer, true);
            }

            ImGui.TextUnformatted("Note: Disable swaps when editing your HUD.");

            ImGui.Spacing();
            var staging = ((int) this.Plugin.Config.StagingSlot + 1).ToString();
            if (ImGui.BeginCombo("Staging slot", staging)) {
                foreach (HudSlot slot in Enum.GetValues(typeof(HudSlot))) {
                    if (!ImGui.Selectable(((int) slot + 1).ToString())) {
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
                ImGui.TextUnformatted("Add swap conditions below.\nThe first condition that is satisfied will be the layout that is used.");
                ImGui.Separator();
                this.DrawConditionsTable();
            }

            ImGui.EndTabItem();
        }

        private void DrawConditionsTable() {
            ImGui.PushFont(UiBuilder.IconFont);
            var height = ImGui.GetContentRegionAvail().Y - ImGui.CalcTextSize(FontAwesomeIcon.Plus.ToIconString()).Y - ImGui.GetStyle().ItemSpacing.Y - ImGui.GetStyle().ItemInnerSpacing.Y * 2;
            ImGui.PopFont();
            if (!ImGui.BeginChild("##conditions-table", new Vector2(-1, height))) {
                return;
            }

            ImGui.Columns(4);

            var conditions = new List<HudConditionMatch>(this.Plugin.Config.HudConditionMatches);
            if (this._editingConditionIndex == conditions.Count) {
                conditions.Add(new HudConditionMatch());
            }

            ImGui.TextUnformatted("Job");
            ImGui.NextColumn();

            ImGui.TextUnformatted("State");
            ImGui.NextColumn();

            ImGui.TextUnformatted("Layout");
            ImGui.NextColumn();

            ImGui.TextUnformatted("Options");
            ImGui.NextColumn();

            ImGui.Separator();

            var addCondition = false;
            var actionedItemIndex = -1;
            var action = 0; // 0 for delete, otherwise move.
            foreach (var item in conditions.Select((cond, i) => new {cond, i})) {
                if (this._editingConditionIndex == item.i) {
                    this._editingCondition ??= new HudConditionMatch();
                    ImGui.PushItemWidth(-1);
                    if (ImGui.BeginCombo("##condition-edit-job", this._editingCondition.ClassJob ?? "Any")) {
                        if (ImGui.Selectable("Any##condition-edit-job")) {
                            this._editingCondition.ClassJob = null;
                        }

                        foreach (var job in this.Plugin.Interface.Data.GetExcelSheet<ClassJob>().Skip(1)) {
                            if (ImGui.Selectable($"{job.Abbreviation}##condition-edit-job")) {
                                this._editingCondition.ClassJob = job.Abbreviation;
                            }
                        }

                        ImGui.EndCombo();
                    }

                    ImGui.PopItemWidth();
                    ImGui.NextColumn();

                    ImGui.PushItemWidth(-1);
                    if (ImGui.BeginCombo("##condition-edit-status", this._editingCondition.Status?.Name() ?? "Any")) {
                        if (ImGui.Selectable("Any##condition-edit-status")) {
                            this._editingCondition.Status = null;
                        }

                        foreach (Status status in Enum.GetValues(typeof(Status))) {
                            if (ImGui.Selectable($"{status.Name()}##condition-edit-status")) {
                                this._editingCondition.Status = status;
                            }
                        }

                        ImGui.EndCombo();
                    }

                    ImGui.PopItemWidth();
                    ImGui.NextColumn();

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
                    ImGui.NextColumn();

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
                } else {
                    ImGui.TextUnformatted(item.cond.ClassJob ?? string.Empty);
                    ImGui.NextColumn();

                    ImGui.TextUnformatted(item.cond.Status?.Name() ?? string.Empty);
                    ImGui.NextColumn();

                    this.Plugin.Config.Layouts.TryGetValue(item.cond.LayoutId, out var condLayout);
                    ImGui.TextUnformatted(condLayout?.Name ?? string.Empty);
                    ImGui.NextColumn();

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
                }

                ImGui.NextColumn();
            }

            ImGui.Columns(1);

            ImGui.Separator();

            ImGui.EndChild();

            if (ImGuiExt.IconButton(FontAwesomeIcon.Plus, "condition")) {
                this._editingConditionIndex = this.Plugin.Config.HudConditionMatches.Count;
                this._editingCondition = new HudConditionMatch();
                this._scrollToAdd = true;
            }

            var recalculate = false;

            if (addCondition) {
                recalculate = true;
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
                recalculate = true;
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

            if (!recalculate) {
                return;
            }

            var player = this.Plugin.Interface.ClientState.LocalPlayer;
            if (player == null || !this.Plugin.Config.SwapsEnabled) {
                return;
            }

            this.Plugin.Statuses.Update(player);
            this.Plugin.Statuses.SetHudLayout(null);
        }
    }
}
