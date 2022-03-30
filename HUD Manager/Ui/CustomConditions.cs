using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface;
using HUD_Manager;
using HUD_Manager.Ui;
using HUDManager.Configuration;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static Dalamud.Game.ClientState.Keys.VirtualKeyExtensions;

namespace HUDManager.Ui
{
    public class CustomConditions
    {
        // UI data container
        private (int selectedIndex, int editIndex, string editBuf) ui = (-1, -1, string.Empty);

        private CustomCondition? activeCondition =>
            ui.selectedIndex >= 0  ?
            Plugin.Config.CustomConditions[ui.selectedIndex] :
            null;

        private Plugin Plugin { get; init; }

        private DrawConditionEditMenu_MultiCondition MenuMulti;
        
        public CustomConditions(Plugin plugin)
        {
            Plugin = plugin;
            MenuMulti = new(Plugin);
        }

        public void Draw(ref bool windowOpen)
        {
            bool update = false;

            ImGuiWindowFlags flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking;

            ImGui.SetNextWindowSize(ImGuiHelpers.ScaledVector2(605, 630), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(ImGuiHelpers.ScaledVector2(605, 630), new Vector2(int.MaxValue, int.MaxValue));

            if (!ImGui.Begin("[HUD Manager] Custom Conditions", ref windowOpen, flags)) {
                ImGui.End();
                return;
            }

            DrawConditionSelectorPane(ref update);

            ImGui.SameLine();

            DrawConditionEditMenu(ref update);

            ImGui.End();

            if (update) {
                Plugin.Config.Save();
            }
        }

        private void DrawConditionSelectorPane(ref bool update)
        {
            float PaneWidth = 170f * ImGuiHelpers.GlobalScale;

            ImGui.BeginGroup();

            var items = Plugin.Config.CustomConditions.Select(c => c.Name).ToArray();
            ImGui.BeginListBox("##custom-condition-listbox", new Vector2(PaneWidth, -1 - ImGui.GetTextLineHeight() * 2));
            foreach (var (cond, i) in Plugin.Config.CustomConditions.Select((item, i) => (item, i))) {
                if (i == ui.editIndex) {
                    if (ImGui.InputText($"##custom-condition-name-{i}", ref ui.editBuf, 128, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.CharsNoBlank)
                        || ImGui.IsItemDeactivatedAfterEdit()) {
                        cond.Name = ui.editBuf;
                        ui.editIndex = -1;
                        update = true;
                    }
                } else {
                    if (ImGui.Selectable($"{cond.Name}##custom-condition-{i}", ui.selectedIndex == i)) {
                        ui.selectedIndex = i;
                    }
                }
            }
            ImGui.EndListBox();

            if (ImGuiExt.IconButton(FontAwesomeIcon.Plus)) {
                int i = 1;
                while (Plugin.Config.CustomConditions.Exists(c => c.Name == $"Condition{i}"))
                    i++;

                Plugin.Config.CustomConditions.Add(new CustomCondition($"Condition{i}", Plugin));

                update = true;
            }

            ImGui.SameLine();

            if (ImGuiExt.IconButton(FontAwesomeIcon.Edit) && ui.selectedIndex >= 0) {
                ui.editIndex = ui.selectedIndex;
            }

            ImGui.SameLine();

            if (ImGuiExt.IconButton(FontAwesomeIcon.Trash) && ui.selectedIndex >= 0 && ui.selectedIndex < items.Length) {
                if (Plugin.Config.HudConditionMatches.Exists(c => c.CustomCondition == activeCondition)) {
                    ImGui.OpenPopup(Popups.CannotRemoveCustomCondition);
                } else {
                    Plugin.Config.CustomConditions.RemoveAt(ui.selectedIndex);
                    ui.selectedIndex = -1;
                    update = true;
                }
            }

            bool _b = true;
            if (ImGui.BeginPopupModal($"{Popups.CannotRemoveCustomCondition}", ref _b, ImGuiWindowFlags.AlwaysAutoResize)) {
                ImGui.Text("There are swap conditions that use this custom condition.");

                if (ImGui.Button("OK##custom-condition-modal-ok")) {
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }

            ImGui.EndGroup();
        }

        private void DrawConditionEditMenu(ref bool update)
        {
            ImGui.BeginChild("##condition-menu-child-edit-condition", new Vector2(-1, -1), true);

            if (activeCondition is null) {
                ImGui.Text("Select a custom condition on the left to edit");
                ImGui.EndChild();
                return;
            }

            if (ImGui.BeginCombo("Condition type", activeCondition.ConditionType.DisplayName())) {
                foreach (var type in Enum.GetValues(typeof(CustomConditionType)).Cast<CustomConditionType>()) {
                    if (ImGui.Selectable(type.DisplayName())) {
                        activeCondition.ConditionType = type;
                        update = true;
                    }
                }

                ImGui.EndCombo();
            }

            ImGui.BeginChild("##condition-menu-child-edit-condition-settings", new Vector2(-1, -1), true);

            switch (activeCondition.ConditionType) {
                case CustomConditionType.ConsoleToggle:
                    DrawConditionEditMenu_ConsoleCommand(ref update);
                    break;
                case CustomConditionType.HoldToActivate:
                    DrawConditionEditMenu_Keybind(ref update);
                    break;
                case CustomConditionType.MultiCondition:
                    MenuMulti.Draw(activeCondition!, ref update);
                    break;
            }

            ImGui.EndChild();

            ImGui.EndChild();
        }

        private void DrawConditionEditMenu_ConsoleCommand(ref bool update)
        {
            if (activeCondition is null)
                return;

            if (!Plugin.Statuses.CustomConditionStatus.ContainsKey(activeCondition))
                return;

            if (ImGui.Button("Toggle")) {
                Plugin.Statuses.CustomConditionStatus.Toggle(activeCondition);
            }

            ImGuiExt.VerticalSpace();
            ImGui.Separator();
            ImGuiExt.VerticalSpace();

            ImGui.Text("Example commands:");

            foreach (string cmd in (new[] { "on", "off", "toggle" })) {
                string fullCommand = $"/hudman condition {activeCondition.Name} {cmd}";
                if (ImGui.Button($"Copy##copy-condition-command-{cmd}")) {
                    ImGui.SetClipboardText(fullCommand);
                }
                ImGui.SameLine();
                ImGui.Text(fullCommand);
            }

            ImGuiExt.VerticalSpace();
            ImGui.Separator();
            ImGuiExt.VerticalSpace();

            ImGui.Text($"Current value: {Plugin.Statuses.CustomConditionStatus[activeCondition]}");
        }

        private void DrawConditionEditMenu_Keybind(ref bool update)
        {
            if (activeCondition is null)
                return;

            ImGui.PushItemWidth(100 * ImGuiHelpers.GlobalScale);

            // Modifier key
            var modifierKeyDisplay = activeCondition.ModifierKeyCode.GetFancyName();
            if (ImGui.BeginCombo("Modifier##custom-condition-modifier-key", modifierKeyDisplay)) {
                foreach ((VirtualKey k, int i) in Plugin.Keybinder.ModifierKeys.Select((k, i) => (k, i))) {
                    if (ImGui.Selectable($"{k.GetFancyName()}##custom-condition-modifier-key-op-{i}")) {
                        activeCondition.ModifierKeyCode = k;
                        update = true;
                    }
                }
                ImGui.EndCombo();
            }

            ImGui.PopItemWidth();
            ImGui.PushItemWidth(135 * ImGuiHelpers.GlobalScale);

            // Input key
            var inputKeyDisplay = activeCondition.KeyCode.GetFancyName();
            if (ImGui.BeginCombo("Keybind##custom-condition-input-key", inputKeyDisplay)) {
                foreach ((VirtualKey k, int i) in Plugin.Keybinder.InputKeys.Select((k, i) => (k, i))) {
                    if (ImGui.Selectable($"{k.GetFancyName()}##custom-condition-input-key-op-{i}")) {
                        activeCondition.KeyCode = k;
                        update = true;
                    }
                }
                ImGui.EndCombo();
            }

            ImGuiExt.VerticalSpace();
            ImGui.Separator();
            ImGuiExt.VerticalSpace();

            ImGui.Text($"Input status: {Plugin.Keybinder.KeybindIsPressed(activeCondition.KeyCode, activeCondition.ModifierKeyCode)}");

            ImGui.PopItemWidth();
        }

        private class DrawConditionEditMenu_MultiCondition
        {
            private readonly Plugin Plugin;

            private (
                int editingConditionIndex,
                MultiCondition.MultiConditionItem? editingCondition, 
                bool addCondition,
                int deleteCondition,
                (int index, int direction) moveCondition,
                float savedRowHeight
            ) Ui = (-1, null, false, -1, (-1, 0), 0);

            public DrawConditionEditMenu_MultiCondition(Plugin plugin)
            {
                Plugin = plugin;
            }

            public void Draw(CustomCondition activeCondition, ref bool update)
            {
                if (activeCondition.ConditionType is not CustomConditionType.MultiCondition)
                    return;

                const ImGuiTableFlags flags = ImGuiTableFlags.PadOuterX
                                        | ImGuiTableFlags.RowBg;

                if (!ImGui.BeginChild("##custom-condition-multi-table-child-container", new Vector2(0, ImGui.GetContentRegionAvail().Y - ImGui.GetTextLineHeightWithSpacing() * 5), true))
                    return;

                if (!ImGui.BeginTable("custom-condition-multi-table", 4, flags))
                    return;

                ImGui.TableSetupColumn("##junction", ImGuiTableColumnFlags.WidthFixed, 57 * ImGuiHelpers.GlobalScale);
                ImGui.TableSetupColumn("NOT", ImGuiTableColumnFlags.WidthFixed, 30 * ImGuiHelpers.GlobalScale);
                ImGui.TableSetupColumn("Condition", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, -1);//ImGuiTableColumnFlags.WidthFixed, 93 * ImGuiHelpers.GlobalScale);
                ImGui.TableHeadersRow();
                
                var workingConditions = new List<MultiCondition.MultiConditionItem>(activeCondition.MultiCondition!.AllItems);
                if (Ui.editingConditionIndex == workingConditions.Count)
                    workingConditions.Add(Ui.editingCondition!);

                foreach (var (cond, i) in workingConditions.Select((cond, i) => (cond, i))) { 
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);

                    if (Ui.editingConditionIndex == i) {
                        // We are editing this row
                        if (Ui.editingCondition is null)
                            throw new InvalidOperationException("editingCondition is null for some reason");

                        // Column: Junction

                        ImGui.PushItemWidth(-1);
                        if (i > 0 &&
                            ImGui.BeginCombo($"##multicond-edit-junction-{i}", Ui.editingCondition!.Type.UiName())) {
                            foreach (var junc in Enum.GetValues<MultiConditionJunction>()) {
                                if (ImGui.Selectable(junc.UiName())) {
                                    Ui.editingCondition!.Type = junc;
                                    update = true;
                                }
                            }

                            ImGui.EndCombo();
                        }

                        ImGui.TableNextColumn();

                        // Column: NOT
                        if (ImGui.Checkbox($"##multicondition-negation-{i}", ref Ui.editingCondition.Negation))
                            update = true;

                        ImGui.TableNextColumn();

                        // Column: Condition

                        if (Ui.editingCondition.Condition.CurrentType == typeof(ClassJobCategoryId))
                            ImGui.PushItemWidth(ImGui.GetColumnWidth() / 2);
                        else
                            ImGui.PushItemWidth(ImGui.GetColumnWidth());

                        if (ImGui.BeginCombo($"##multicond-edit-condition-{i}", Ui.editingCondition.Condition.UiName(Plugin, partial: Ui.editingConditionIndex >= 0))) {
                            foreach (Status status in Enum.GetValues(typeof(Status))) {
                                if (ImGui.Selectable($"{status.Name()}##condition-edit-status")) {
                                    Ui.editingCondition.Condition = new CustomConditionUnion(status);
                                    update = true;
                                }
                            }

                            foreach (var custom in Plugin.Config.CustomConditions) {
                                if (ImGui.Selectable($"{custom.Name}##condition-edit-status")) {
                                    Ui.editingCondition.Condition = new CustomConditionUnion(custom);
                                    update = true;
                                }
                            }

                            if (ImGui.Selectable("Class/Job")) {
                                Ui.editingCondition.Condition = new CustomConditionUnion((ClassJobCategoryId)0);
                                update = true;
                            }

                            ImGui.EndCombo();
                        }

                        if (Ui.editingCondition.Condition.CurrentType == typeof(ClassJobCategoryId)) {
                            ImGui.SameLine();

                            // Secondary combo for ClassJob

                            if (ImGui.BeginCombo($"##multicond-edit-condition-classjob-{i}", Ui.editingCondition.Condition.ClassJob!.Value.DisplayName(Plugin))) {
                                bool first = true;
                                foreach (var group in ClassJobCategoryIdExtensions.ClassJobCategoryGroupings) {
                                    if (!first) {
                                        ImGui.Selectable("--", false, ImGuiSelectableFlags.Disabled);
                                    }

                                    if (first) first = false;

                                    foreach (var classJob in group) {
                                        if (ImGui.Selectable($"{classJob.DisplayName(Plugin)}##condition-edit-status-classjob-{classJob}")) {
                                            Ui.editingCondition.Condition = new CustomConditionUnion(classJob);
                                            update = true;
                                        }
                                    }

                                }

                                ImGui.EndCombo();
                            }
                        }

                        ImGui.PopItemWidth();

                        ImGui.TableNextColumn();

                        // Column: Actions

                        if (!(cond.Condition.CurrentType == typeof(ClassJobCategoryId) && cond.Condition.ClassJob!.Value == 0) 
                            && ImGuiExt.IconButton(FontAwesomeIcon.Check, "multicond-confirm")) {
                            Ui.addCondition = true;
                        }

                        ImGui.SameLine();

                        if (ImGuiExt.IconButton(FontAwesomeIcon.Times, "multicond-cancel")) {
                            Ui.editingConditionIndex = -1;
                            Ui.editingCondition = null;
                        }

                        if (Ui.savedRowHeight == 0)
                            Ui.savedRowHeight = ImGui.GetTextLineHeightWithSpacing();

                        ImGui.PopItemWidth();

                        ImGui.TableNextColumn();
                    } else {
                        // Just displaying the information

                        // Column: Junction

                        if (i > 0)
                            ImGui.TextUnformatted(cond.Type.UiName());
                        ImGui.TableNextColumn();

                        // Column: NOT

                        ImGui.PushFont(UiBuilder.IconFont);
                        ImGui.TextUnformatted(cond.Negation ? FontAwesomeIcon.Check.ToIconString() : string.Empty);
                        ImGui.PopFont();
                        ImGui.TableNextColumn();

                        // Column: Condition

                        ImGui.Text(cond.Condition.IsActive(Plugin) ? "●" : "○");
                        ImGui.SameLine();
                        ImGui.TextUnformatted(cond.Condition.UiName(Plugin));
                        ImGui.TableNextColumn();

                        // Column: Actions

                        if (Ui.editingCondition is null) {
                            if (ImGuiExt.IconButton(FontAwesomeIcon.PencilAlt, $"{i}")) {
                                Ui.editingConditionIndex = i;
                                Ui.editingCondition = cond;
                            }

                            ImGui.SameLine();
                            if (ImGuiExt.IconButton(FontAwesomeIcon.Trash, $"{i}")) {
                                Ui.deleteCondition = i;
                            }

                            ImGui.SameLine();
                            if (ImGuiExt.IconButton(FontAwesomeIcon.ArrowUp, $"{i}")) {
                                Ui.moveCondition.index = i;
                                Ui.moveCondition.direction = -1;
                            }

                            ImGui.SameLine();
                            if (ImGuiExt.IconButton(FontAwesomeIcon.ArrowDown, $"{i}")) {
                                Ui.moveCondition.index = i;
                                Ui.moveCondition.direction = 1;
                            }

                            if (Ui.savedRowHeight == 0)
                                Ui.savedRowHeight = ImGui.GetTextLineHeightWithSpacing();
                        } else {
                            // Create dummy to fill in the space
                            ImGui.Dummy(new Vector2(0, Ui.savedRowHeight));
                        }
                    }
                }

                ImGui.EndTable();

                if (ImGuiExt.IconButton(FontAwesomeIcon.Plus, "condition")) {
                    Ui.editingConditionIndex = activeCondition.MultiCondition.Count;
                    Ui.editingCondition = new MultiCondition.MultiConditionItem()
                        { 
                            Type = MultiConditionJunction.LogicalAnd, 
                            Condition = new CustomConditionUnion(Status.WeaponDrawn)
                        };
                } else if (ImGui.IsItemHovered()) {
                    ImGui.SetTooltip("Add a new condition");
                }

                ImGui.EndChild();

                ImGui.BeginGroup();

                ImGui.TextUnformatted($"Current status: {activeCondition.IsMet(Plugin)}");

                if (Ui.addCondition) {
                    update = true;

                    bool addSuccess = false;

                    if (Ui.editingConditionIndex == activeCondition.MultiCondition.Count) {
                        addSuccess |= activeCondition.MultiCondition.AddCondition(Ui.editingCondition!);
                    } else {
                        activeCondition.MultiCondition.RemoveCondition(Ui.editingConditionIndex);
                        addSuccess |= activeCondition.MultiCondition.AddCondition(Ui.editingCondition!, Ui.editingConditionIndex);
                    }

                    if (!addSuccess)
                        ImGui.OpenPopup(Popups.AddedConditionWouldCreateLoop);

                    Ui.addCondition = false;
                    Ui.editingConditionIndex = -1;
                    Ui.editingCondition = null;
                }

                if (Ui.moveCondition.index >= 0) {
                    update = true;

                    if (Ui.moveCondition.index + Ui.moveCondition.direction >= 0
                      && Ui.moveCondition.index + Ui.moveCondition.direction < activeCondition.MultiCondition.Count) {
                        var c = activeCondition.MultiCondition[Ui.moveCondition.index];
                        var newPosition = Ui.moveCondition.index + Ui.moveCondition.direction;
                        activeCondition.MultiCondition.RemoveCondition(Ui.moveCondition.index);
                        activeCondition.MultiCondition.AddCondition(c, Ui.moveCondition.index + Ui.moveCondition.direction);
                    }

                    Ui.moveCondition = (-1, 0);
                }

                if (Ui.deleteCondition >= 0) {
                    update = true;

                    activeCondition.MultiCondition.RemoveCondition(Ui.deleteCondition);

                    Ui.deleteCondition = -1;
                }

                // Popups

                if (ImGui.BeginPopupModal(Popups.AddedConditionWouldCreateLoop)) {
                    ImGui.Text("Adding that condition would result in an infinite loop.");
                    if (ImGui.Button("OK"))
                        ImGui.CloseCurrentPopup();

                    ImGui.EndPopup();
                }
            }
        }
    }
}
