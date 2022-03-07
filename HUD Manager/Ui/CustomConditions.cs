using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface;
using HUD_Manager;
using HUD_Manager.Ui;
using HUDManager.Configuration;
using ImGuiNET;
using System;
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
        public CustomConditions(Plugin plugin)
        {
            Plugin = plugin;
        }

        public void Draw(ref bool windowOpen)
        {
            bool update = false;

            ImGuiWindowFlags flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking;
            ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, ImGuiHelpers.ScaledVector2(465, 630));
            if (!ImGui.Begin("[HUD Manager] Custom Conditions", ref windowOpen, flags)) {
                ImGui.End();
                ImGui.PopStyleVar();
                return;
            }
            ImGui.PopStyleVar();

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
            int PaneWidth = (int)(170f * ImGuiHelpers.GlobalScale);

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

            ImGui.Text("No settings available for this condition type.");

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
    }
}
