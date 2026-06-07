using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Services;
using HUDManager.Configuration;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace HUDManager.Ui;

public class CustomConditions : Dalamud.Interface.Windowing.Window
{
    private static uint SwapperSettingsPaneHeight => (uint)(120 * ImGuiHelpers.GlobalScale);

    // UI data container
    private (int selectedIndex, int editIndex, string? previousName, string editBuf, bool focusTextEdit) _ui = (-1, -1, null, string.Empty, false);

    private CustomCondition? ActiveCondition =>
        _ui.selectedIndex >= 0 ?
            Plugin.Config.CustomConditions[_ui.selectedIndex] :
            null;

    private Plugin Plugin { get; }

    private readonly DrawConditionEditMenu_InZone _zoneMenu;
    private readonly DrawConditionEditMenu_MultiCondition _menuMulti;

    public CustomConditions(Plugin plugin) : base("[HUD Manager] Custom Conditions", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking) {
        Plugin = plugin;
        _zoneMenu = new DrawConditionEditMenu_InZone(Plugin.DataManager);
        _menuMulti = new DrawConditionEditMenu_MultiCondition(Plugin);

        Size = new Vector2(605, 700);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(605, 700),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
    }

    private string DefaultConditionName()
    {
        var i = 1;

        string DefaultConditionPattern() => $"Condition{i}";

        while (Plugin.Config.CustomConditions.Exists(c => c.Name == DefaultConditionPattern())) {
            Plugin.Log.Information($"{i}");
            i++;
        }

        return DefaultConditionPattern();
    }

    public override void Draw()
    {
        var update = false;

        DrawConditionSelectorPane(ref update);

        ImGui.SameLine();

        DrawConditionEditMenu(ref update);

        if (update) {
            Plugin.Config.Save();
        }
    }

    private void DrawConditionSelectorPane(ref bool update)
    {
        bool ConditionNameIsValid(string? s)
            => !string.IsNullOrWhiteSpace(s) && !Plugin.Config.CustomConditions.Exists(c => c.Name != _ui.previousName && c.Name == s);

        var paneWidth = 170f * ImGuiHelpers.GlobalScale;

        using var group = ImRaii.Group();

        var items = Plugin.Config.CustomConditions.Select(c => c.Name).ToArray();

        using (var listBox = ImRaii.ListBox("##custom-condition-listbox", new Vector2(paneWidth, -1 - ImGui.GetTextLineHeight() * 2))) {
            if (listBox) {
                foreach (var (cond, i) in Plugin.Config.CustomConditions.Select((item, i) => (item, i))) {
                    if (i == _ui.editIndex) {
                        if (ImGui.InputText($"##custom-condition-name-{i}", ref _ui.editBuf, 128, ImGuiInputTextFlags.EnterReturnsTrue)
                            || ImGui.IsItemDeactivated()) {
                            // save users from themselves
                            _ui.editBuf = _ui.editBuf
                                .Trim()
                                .Replace(Commands.QuoteCharacter, "");

                            // This kind of check should really be enforced on the config level but whatever.
                            if (!ConditionNameIsValid(_ui.editBuf)) {
                                cond.Name = ConditionNameIsValid(_ui.previousName) ? _ui.previousName! : DefaultConditionName();
                            } else {
                                cond.Name = _ui.editBuf;
                            }

                            _ui.editIndex = -1;
                            _ui.editBuf = string.Empty;
                            _ui.previousName = null;
                            update = true;
                        }

                        if (_ui.focusTextEdit) {
                            ImGui.SetKeyboardFocusHere(-1);
                            _ui.focusTextEdit = false;
                        }
                    } else {
                        if (ImGui.Selectable($"{cond.Name}##custom-condition-selectable", _ui.selectedIndex == i)) {
                            _menuMulti.ClearEditing();
                            _ui.selectedIndex = i;
                        }
                    }
                }
            }
        }

        if (ImGuiExt.IconButton(FontAwesomeIcon.Plus)) {
            Plugin.Config.CustomConditions.Add(new CustomCondition("<TEMP>", Plugin));

            // Enable edit box
            _ui.editIndex = Plugin.Config.CustomConditions.Count - 1;
            _ui.focusTextEdit = true;

            update = true;
        }
        ImGuiExt.HoverTooltip("Add");

        ImGui.SameLine();

        if (ImGuiExt.IconButton(FontAwesomeIcon.Edit) && _ui.selectedIndex >= 0) {
            _ui.previousName = Plugin.Config.CustomConditions[_ui.selectedIndex].Name;
            _ui.editIndex = _ui.selectedIndex;
            _ui.editBuf = _ui.previousName;
            _ui.focusTextEdit = true;
        }
        ImGuiExt.HoverTooltip("Rename");

        ImGui.SameLine();

        if (ImGuiExt.IconButton(FontAwesomeIcon.ArrowUp) && _ui is { editIndex: < 0, selectedIndex: > 0 }) {
            Plugin.Config.CustomConditions.Reverse(_ui.selectedIndex - 1, 2);
            _ui.selectedIndex -= 1;
            update = true;
        }
        ImGuiExt.HoverTooltip("Move up");

        ImGui.SameLine();

        if (ImGuiExt.IconButton(FontAwesomeIcon.ArrowDown) && _ui.editIndex < 0 && _ui.selectedIndex < Plugin.Config.CustomConditions.Count - 1) {
            Plugin.Config.CustomConditions.Reverse(_ui.selectedIndex, 2);
            _ui.selectedIndex += 1;
            update = true;
        }
        ImGuiExt.HoverTooltip("Move down");

        ImGui.SameLine();

        if (ImGuiExt.IconButtonEnabledWhen(ImGui.GetIO().KeyCtrl, FontAwesomeIcon.TrashAlt) && _ui.selectedIndex >= 0 && _ui.selectedIndex < items.Length) {
            if (Plugin.Config.HudConditionMatches.Exists(c => c.CustomCondition == ActiveCondition)) {
                ImGui.OpenPopup(Popups.CannotRemoveCustomCondition);
            } else {
                Plugin.Config.CustomConditions.RemoveAt(_ui.selectedIndex);
                _ui.selectedIndex = -1;
                update = true;
            }
        }
        ImGuiExt.HoverTooltip("Delete (hold Control to allow)");

        var _b = true;
        using (var popup = ImRaii.PopupModal($"{Popups.CannotRemoveCustomCondition}", ref _b, ImGuiWindowFlags.AlwaysAutoResize)) {
            if (popup) {
                ImGui.Text("There are swap conditions that use this custom condition.");

                if (ImGui.Button("OK##custom-condition-modal-ok")) {
                    ImGui.CloseCurrentPopup();
                }
            }
        }
    }

    private void DrawConditionEditMenu(ref bool update)
    {
        using var editChild = ImRaii.Child("##condition-menu-child-edit-condition", new Vector2(-1, -1), true);

        if (ActiveCondition is null) {
            ImGui.Text("Select a custom condition on the left to edit");
            return;
        }

        ImGui.Separator();

        using (var combo = ImRaii.Combo("Condition type", ActiveCondition.ConditionType.DisplayName())) {
            if (combo) {
                foreach (var type in Enum.GetValues<CustomConditionType>()
                             .OrderBy(t => t.DisplayOrder())) {
                    if (ImGui.Selectable(type.DisplayName())) {
                        ActiveCondition.ConditionType = type;
                        update = true;
                    }
                }
            }
        }

        ImGui.Spacing();

        var valueChildBgColor = ActiveCondition.IsMet(Plugin) ? ImGuiColors.HealerGreen : ImGuiColors.DPSRed;

        using (ImRaii.PushColor(ImGuiCol.ChildBg, valueChildBgColor - new Vector4(0, 0, 0, 0.82f)))
        using (var child = ImRaii.Child("##condition-edit-display-value-child", new Vector2(-1, ImGui.GetTextLineHeightWithSpacing() + ImGui.GetStyle().ItemInnerSpacing.Y * 2 + 4), true, ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs)) {
            if (child) {
                ImGui.Text("Current value:");
                ImGui.SameLine();

                var state = ActiveCondition.IpcState(Plugin);
                if (state is >= ConditionState.ErrorPluginUnavailable) {
                    var text = state switch {
                        ConditionState.ErrorPluginUnavailable => "× QoL Bar not loaded",
                        ConditionState.ErrorConditionRemoved => "× Condition removed",
                        ConditionState.ErrorConditionNotFound => "× Condition not found",
                        ConditionState.ErrorUnknown => "× Error getting condition state",
                        _ => string.Empty,
                    };
                    ImGui.TextColored(ImGuiColors.ParsedPurple, text);
                } else {
                    if (ActiveCondition.IsMet(Plugin)) {
                        ImGui.TextColored(ImGuiColors.ParsedGreen, "✓ TRUE");
                    } else {
                        ImGui.TextColored(ImGuiColors.DalamudRed, "× FALSE");
                    }
                }
            }
        }

        ImGui.Spacing();

        using (var child = ImRaii.Child("##condition-menu-child-edit-condition-settings-main", new Vector2(-1, ImGui.GetContentRegionAvail().Y - ImGui.GetTextLineHeight() - SwapperSettingsPaneHeight))) {
            if (child) {
                ImGui.Spacing();
                switch (ActiveCondition.ConditionType) {
                    case CustomConditionType.ConsoleToggle:
                        DrawConditionEditMenu_ConsoleCommand();
                        break;
                    case CustomConditionType.HoldToActivate:
                        DrawConditionEditMenu_Keybind(ref update);
                        break;
                    case CustomConditionType.InZone:
                        _zoneMenu.Draw(ActiveCondition, ref update);
                        break;
                    case CustomConditionType.QoLBarCondition:
                        DrawConditionEditMenu_QoLBar(ref update);
                        break;
                    case CustomConditionType.MultiCondition:
                        _menuMulti.Draw(ActiveCondition, ref update);
                        break;
                }
            }
        }

        using (var child = ImRaii.Child("##condition-menu-child-sub-settings", new Vector2(-1, SwapperSettingsPaneHeight), true)) {
            if (child) {
                DrawConditionEditMenuSwapSettings(ref update);
            }
        }
    }

    private void DrawConditionEditMenuSwapSettings(ref bool update)
    {
        if (ActiveCondition is null)
            return;

        ImGui.Text("Swapper Settings");

        var enableHoldTime = ActiveCondition.HoldTime > 0;
        if (ImGui.Checkbox("Delay layout change when deactivating", ref enableHoldTime)) {
            ActiveCondition.HoldTime = enableHoldTime ? 0.1f : 0;
        }

        ImGui.SameLine();
        ImGuiExt.HelpMarker("Sets a duration (in seconds) to delay changing the layout once this condition is no longer satisfied." +
            "For example, this can be used to keep your combat layout up for a few seconds after combat ends." +
            "\n\nNote that the delay will only be applied when the condition is activated as a Swap condition." +
            "It will be ignored when activating the condition in this menu.");

        if (enableHoldTime) {
            var holdTimeInput = ActiveCondition.HoldTime;

            using (ImRaii.PushIndent())
            using (ImRaii.ItemWidth(180)) {
                if (ImGui.InputFloat("Delay duration", ref holdTimeInput, 0.05f, 0.2f, "%.2f")) {
                    if (holdTimeInput > 0) {
                        ActiveCondition.HoldTime = Math.Max(0, holdTimeInput);
                        update = true;
                    }
                }
            }
        }
    }

    private void DrawConditionEditMenu_ConsoleCommand()
    {
        if (ActiveCondition is null)
            return;

        if (!Plugin.Statuses.CustomConditionStatus.ContainsKey(ActiveCondition))
            return;

        if (ImGui.Button("Toggle")) {
            Plugin.Statuses.CustomConditionStatus.Toggle(ActiveCondition);
        }

        ImGuiExt.VerticalSpace();
        ImGui.Separator();
        ImGuiExt.VerticalSpace();

        ImGui.Text("Example commands:");

        foreach (var cmd in new[] { "on", "off", "toggle" }) {
            var name = ActiveCondition.Name.Any(char.IsWhiteSpace) ? $"\"{ActiveCondition.Name}\"" : ActiveCondition.Name;
            var fullCommand = $"/hudman condition {name} {cmd}";
            if (ImGui.Button($"Copy##copy-condition-command-{cmd}")) {
                ImGui.SetClipboardText(fullCommand);
            }
            ImGui.SameLine();
            ImGui.Text(fullCommand);
        }
    }

    private void DrawConditionEditMenu_Keybind(ref bool update)
    {
        if (ActiveCondition is null)
            return;

        using var itemWidth = ImRaii.ItemWidth(135 * ImGuiHelpers.GlobalScale);


        // Modifier key
        var modifierKeyDisplay = ActiveCondition.ModifierKeyCode.GetFancyName();
        using (var combo = ImRaii.Combo("Modifier##custom-condition-modifier-key", modifierKeyDisplay)) {
            if (combo) {
                foreach (var k in Plugin.Keybinder.ModifierKeys) {
                    if (ImGui.Selectable($"{k.GetFancyName()}##custom-condition-modifier-key-op")) {
                        ActiveCondition.ModifierKeyCode = k;
                        update = true;
                    }
                }
            }
        }

        // Input key
        var inputKeyDisplay = ActiveCondition.KeyCode.GetFancyName();
        using (var combo = ImRaii.Combo("Keybind##custom-condition-input-key", inputKeyDisplay)) {
            if (combo) {
                foreach (var k in Plugin.Keybinder.InputKeys) {
                    if (ImGui.Selectable($"{k.GetFancyName()}##custom-condition-input-key-op")) {
                        ActiveCondition.KeyCode = k;
                        update = true;
                    }
                }
            }
        }
    }

    private void DrawConditionEditMenu_QoLBar(ref bool update)
    {
        if (ActiveCondition is null)
            return;

        if (Plugin.QoLBarIpc.Enabled) {
            using var itemWidth = ImRaii.ItemWidth(250 * ImGuiHelpers.GlobalScale);

            var selected = ActiveCondition.ExternalIndex;
            var conditions = Plugin.QoLBarIpc.GetConditionSets();

            string selectedName;
            if (selected >= conditions.Length) {
                selectedName = $"Invalid condition [{selected}]";
            } else {
                selectedName = ActiveCondition.ExternalIndex < 0 ? "No condition" : $"[{selected}] {conditions[selected]}";
            }

            using (var combo = ImRaii.Combo("Condition##qol-bar-condition", selectedName)) {
                if (combo) {
                    for (var i = 0; i < conditions.Length; i++) {
                        var name = $"[{i}] {conditions[i]}";
                        if (ImGui.Selectable($"{name}##custom-condition-modifier-key-op")) {
                            ActiveCondition.ExternalIndex = i;
                            Plugin.QoLBarIpc.ClearCache();
                            update = true;
                        }
                    }
                }
            }

            var negate = ActiveCondition.Negate;
            if (ImGui.Checkbox("NOT##qol-bar-condition-negation", ref negate)) {
                ActiveCondition.Negate = negate;
                Plugin.QoLBarIpc.ClearCache();
                update = true;
            }

        }

        ImGuiExt.VerticalSpace();
        ImGui.Separator();
        ImGuiExt.VerticalSpace();

        ImGui.PushTextWrapPos();

        ImGui.TextUnformatted("QoL Bar conditions require that the \"QoL Bar\" plugin by UnknownX is installed and enabled.");
        ImGuiExt.VerticalSpace();
        ImGui.TextUnformatted("Please note that QoL Bar conditions are saved according to their index number. This means "
            + "that if the order of QoL Bar conditions changes while HUD Manager is disabled, an incorrect index may be "
            + "used the next time HUD Manager is enabled. Keep this in mind if your QoL Bar conditions seem to be behaving "
            + "strangely. A broken condition can be repaired by selecting a new condition above.");
        ImGuiExt.VerticalSpace();
        ImGui.TextUnformatted("There is a small performance penalty to fetching the QoL Bar condition state from another "
            + "plugin, therefore it is recommended to create complex hybrid conditions on the QoL Bar side if possible, "
            + "instead of checking many such conditions from HUD Manager.");

        ImGui.PopTextWrapPos();
    }

    private class DrawConditionEditMenu_InZone
    {

        private record ZoneListData(uint MapId, string Name);

        private string _zoneNameFilterInput = string.Empty;
        private readonly List<ZoneListData> _allZones;

        private int _selectedZonesSelection = -1;
        private int _allZonesSelection = -1;

        public DrawConditionEditMenu_InZone(IDataManager data)
        {
            _allZones = Map.GetZoneMaps(data).Select(map => new ZoneListData(map.RowId, map.Name)).ToList();
        }

        public void Draw(CustomCondition? activeCondition, ref bool update)
        {
            if (activeCondition is null)
                return;

            var listBoxSize = new Vector2(ImGui.GetContentRegionAvail().X - 100 * ImGuiHelpers.GlobalScale,
                (ImGui.GetContentRegionAvail().Y - ImGui.GetTextLineHeight() * 5) / 2);
            using (var listBox = ImRaii.ListBox("Selected zones", listBoxSize)) {
                if (listBox) {
                    var conditionZoneItems = activeCondition.MapIds.Select(mid => new ZoneListData(mid, _allZones.First(zone => zone.MapId == mid).Name));
                    foreach (var (zone, i) in conditionZoneItems.Select((z, i) => (z, i))) {
                        if (ImGui.Selectable($"{zone.Name}##selected-{i}", _selectedZonesSelection == i)) {
                            _selectedZonesSelection = i;
                        }
                    }
                }
            }

            ImGui.Separator();

            if (ImGuiExt.IconButton(FontAwesomeIcon.ArrowDown, "down") && _selectedZonesSelection >= 0) {
                activeCondition.MapIds.RemoveAt(_selectedZonesSelection);
                update = true;

                _selectedZonesSelection = -1;
            } else if (ImGui.IsItemHovered()) {
                ImGui.SetTooltip("Remove the selected zone from the list above.");
            }
            ImGui.SameLine();
            if (ImGuiExt.IconButton(FontAwesomeIcon.ArrowUp, "up") && _allZonesSelection >= 0) {
                activeCondition.MapIds.Add(_allZones[_allZonesSelection].MapId);
                update = true;

                _allZonesSelection = -1;
            } else if (ImGui.IsItemHovered()) {
                ImGui.SetTooltip("Add the selected zone from the list below.");
            }

            ImGui.Separator();

            using (ImRaii.ItemWidth(listBoxSize.X)) {
                if (ImGui.InputText("Filter", ref _zoneNameFilterInput, 256)) {
                    _allZonesSelection = -1;
                }
            }

            using (var listBox = ImRaii.ListBox("All zones", listBoxSize)) {
                if (listBox) {
                    foreach (var (zone, i) in _allZones
                                 .Select((z, i) => (z, i))
                                 .Where(zi => zi.z.Name.Contains(_zoneNameFilterInput, StringComparison.InvariantCultureIgnoreCase))
                                 .ExceptBy(activeCondition.MapIds, zi => zi.z.MapId)) {
                        if (ImGui.Selectable($"{zone.Name}##selected-all-{i}", _allZonesSelection == i)) {
                            _allZonesSelection = i;
                        }
                    }
                }
            }
        }
    }

    private class DrawConditionEditMenu_MultiCondition
    {
        private readonly Plugin _plugin;

        private (
            int editingConditionIndex,
            MultiCondition.MultiConditionItem? editingCondition,
            bool addCondition,
            int deleteCondition,
            (int index, int direction) moveCondition,
            float savedRowHeight
            ) _ui;

        public DrawConditionEditMenu_MultiCondition(Plugin plugin)
        {
            _plugin = plugin;
            ClearEditing();
        }

        public void ClearEditing()
        {
            _ui = (
                editingConditionIndex: -1,
                editingCondition: null,
                addCondition: false,
                deleteCondition: -1,
                moveCondition: (-1, 0),
                savedRowHeight: 0
            );
        }

        public void Draw(CustomCondition activeCondition, ref bool update)
        {
            if (activeCondition.ConditionType is not CustomConditionType.MultiCondition)
                return;

            var usedConditionLoopPopup = false;

            DrawTable(activeCondition, ref usedConditionLoopPopup, ref update);

            DrawOthers(activeCondition, usedConditionLoopPopup, ref update);
        }

        public void DrawTable(CustomCondition activeCondition, ref bool usedConditionLoopPopup, ref bool update) {
            using var table = ImRaii.Table("custom-condition-multi-table", 4, ImGuiTableFlags.PadOuterX | ImGuiTableFlags.RowBg);
            if (!table) return;

            ImGui.TableSetupColumn("##junction", ImGuiTableColumnFlags.WidthFixed, 57 * ImGuiHelpers.GlobalScale);
            ImGui.TableSetupColumn("NOT", ImGuiTableColumnFlags.WidthFixed, 30 * ImGuiHelpers.GlobalScale);
            ImGui.TableSetupColumn("Condition", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, -1);//ImGuiTableColumnFlags.WidthFixed, 93 * ImGuiHelpers.GlobalScale);
            ImGui.TableHeadersRow();

            var workingConditions = new List<MultiCondition.MultiConditionItem>(activeCondition.MultiCondition.AllItems);
            if (_ui.editingConditionIndex == workingConditions.Count)
                workingConditions.Add(_ui.editingCondition!);

            foreach (var (cond, i) in workingConditions.Select((cond, i) => (cond, i))) {
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);

                if (_ui.editingConditionIndex == i) {
                    // We are editing this row
                    if (_ui.editingCondition is null)
                        throw new InvalidOperationException("editingCondition is null for some reason");

                    // Column: Junction

                    using (ImRaii.ItemWidth(-1)) {
                        if (i > 0) {
                            using (var combo = ImRaii.Combo($"##multicond-edit-junction-{i}", _ui.editingCondition!.Type.UiName())) {
                                if (combo) {
                                    foreach (var junc in Enum.GetValues<MultiConditionJunction>()) {
                                        if (ImGui.Selectable(junc.UiName())) {
                                            _ui.editingCondition!.Type = junc;
                                            update = true;
                                        }
                                    }
                                }
                            }
                        }

                        ImGui.TableNextColumn();

                        // Column: NOT
                        if (ImGui.Checkbox($"##multicondition-negation-{i}", ref _ui.editingCondition.Negation))
                            update = true;

                        ImGui.TableNextColumn();

                        // Column: Condition

                        var conditionWidth = ImGui.GetColumnWidth();
                        if (_ui.editingCondition.Condition.CurrentType == typeof(ClassJobCategoryId))
                            conditionWidth /= 2;

                        using (ImRaii.ItemWidth(conditionWidth)) {
                            using (var conditionCombo = ImRaii.Combo($"##multicond-edit-condition-{i}", _ui.editingCondition.Condition.UiName(_plugin, partial: _ui.editingConditionIndex >= 0))) {
                                if (conditionCombo) {
                                    foreach (var status in Enum.GetValues<Status>()) {
                                        if (ImGui.Selectable($"{status.Name()}##condition-edit-status")) {
                                            _ui.editingCondition.Condition = new CustomConditionUnion(status);
                                            update = true;
                                        }
                                    }

                                    foreach (var custom in _plugin.Config.CustomConditions) {
                                        if (ImGui.Selectable($"{custom.DisplayName}##condition-edit-status")) {
                                            var prevCondition = _ui.editingCondition.Condition;

                                            _ui.editingCondition.Condition = new CustomConditionUnion(custom);

                                            if (!_ui.editingCondition.Condition.Custom!.MultiCondition.Validate()) {
                                                // Revert to previous condition
                                                _ui.editingCondition.Condition = prevCondition;
                                                usedConditionLoopPopup = true;
                                            }

                                            update = true;
                                        }
                                    }

                                    if (ImGui.Selectable("Class/Job")) {
                                        _ui.editingCondition.Condition = new CustomConditionUnion((ClassJobCategoryId)0);
                                        update = true;
                                    }
                                }
                            }

                            if (_ui.editingCondition.Condition.CurrentType == typeof(ClassJobCategoryId)) {
                                ImGui.SameLine();

                                // Secondary combo for ClassJob
                                using var classJobCombo = ImRaii.Combo($"##multicond-edit-condition-classjob-{i}", _ui.editingCondition.Condition.ClassJob!.Value.DisplayName(_plugin));
                                if (classJobCombo) {
                                    var first = true;
                                    foreach (var group in ClassJobCategoryIdExtensions.ClassJobCategoryGroupings) {
                                        if (first)
                                            first = false;
                                        else
                                            ImGui.Selectable("--", false, ImGuiSelectableFlags.Disabled);

                                        foreach (var classJob in group) {
                                            if (ImGui.Selectable($"{classJob.DisplayName(_plugin)}##condition-edit-status-classjob-{classJob}")) {
                                                _ui.editingCondition.Condition = new CustomConditionUnion(classJob);
                                                update = true;
                                            }
                                        }

                                    }
                                }
                            }
                        }

                        ImGui.TableNextColumn();

                        // Column: Actions

                        if (!(cond.Condition.CurrentType == typeof(ClassJobCategoryId) && cond.Condition.ClassJob!.Value == 0)
                            && ImGuiExt.IconButton(FontAwesomeIcon.Check, "multicond-confirm")) {
                            _ui.addCondition = true;
                        }

                        ImGui.SameLine();

                        if (ImGuiExt.IconButton(FontAwesomeIcon.Times, "multicond-cancel")) {
                            _ui.editingConditionIndex = -1;
                            _ui.editingCondition = null;
                        }

                        if (_ui.savedRowHeight == 0)
                            _ui.savedRowHeight = ImGui.GetTextLineHeightWithSpacing();

                    }

                    ImGui.TableNextColumn();
                } else {
                    // Just displaying the information

                    // Column: Junction

                    if (i > 0)
                        ImGui.Text(cond.Type.UiName());
                    ImGui.TableNextColumn();

                    // Column: NOT

                    using (ImRaii.PushFont(UiBuilder.IconFont)) {
                        ImGui.Text(cond.Negation ? FontAwesomeIcon.Check.ToIconString() : string.Empty);
                    }
                    ImGui.TableNextColumn();

                    // Column: Condition

                    var thisConditionActive = cond.Condition.IsActive(_plugin) ^ cond.Negation;
                    ImGui.Text(thisConditionActive ? "●" : "○");
                    ImGui.SameLine();
                    ImGui.Text(cond.Condition.UiName(_plugin));
                    ImGui.TableNextColumn();

                    // Column: Actions

                    if (_ui.editingCondition is null) {
                        if (ImGuiExt.IconButton(FontAwesomeIcon.PencilAlt, $"{i}")) {
                            _ui.editingConditionIndex = i;
                            _ui.editingCondition = cond;
                        }

                        ImGui.SameLine();
                        if (ImGuiExt.IconButton(FontAwesomeIcon.TrashAlt, $"{i}")) {
                            _ui.deleteCondition = i;
                        }

                        ImGui.SameLine();
                        if (ImGuiExt.IconButton(FontAwesomeIcon.ArrowUp, $"{i}")) {
                            _ui.moveCondition.index = i;
                            _ui.moveCondition.direction = -1;
                        }

                        ImGui.SameLine();
                        if (ImGuiExt.IconButton(FontAwesomeIcon.ArrowDown, $"{i}")) {
                            _ui.moveCondition.index = i;
                            _ui.moveCondition.direction = 1;
                        }

                        if (_ui.savedRowHeight == 0)
                            _ui.savedRowHeight = ImGui.GetTextLineHeightWithSpacing();
                    } else {
                        // Create dummy to fill in the space
                        ImGui.Dummy(new Vector2(0, _ui.savedRowHeight));
                    }
                }
            }
        }

        private void DrawOthers(CustomCondition activeCondition, bool usedConditionLoopPopup, ref bool update) {
            if (ImGuiExt.IconButton(FontAwesomeIcon.Plus, "condition")) {
                _ui.editingConditionIndex = activeCondition.MultiCondition.Count;
                _ui.editingCondition = new MultiCondition.MultiConditionItem
                {
                    Type = MultiConditionJunction.LogicalAnd,
                    Condition = new CustomConditionUnion(Status.WeaponDrawn),
                };
            } else if (ImGui.IsItemHovered()) {
                ImGui.SetTooltip("Add a new condition");
            }

            if (_ui.addCondition) {
                update = true;

                var addSuccess = false;

                if (_ui.editingConditionIndex == activeCondition.MultiCondition.Count) {
                    addSuccess |= activeCondition.MultiCondition.AddCondition(_ui.editingCondition!);
                } else {
                    activeCondition.MultiCondition.RemoveCondition(_ui.editingConditionIndex);
                    addSuccess |= activeCondition.MultiCondition.AddCondition(_ui.editingCondition!, _ui.editingConditionIndex);
                }

                if (!addSuccess)
                    ImGui.OpenPopup(Popups.AddedConditionWouldCreateLoop);

                _ui.addCondition = false;
                _ui.editingConditionIndex = -1;
                _ui.editingCondition = null;
            }

            if (_ui.moveCondition.index >= 0) {
                update = true;

                var newPosition = _ui.moveCondition.index + _ui.moveCondition.direction;
                if (newPosition >= 0 && newPosition < activeCondition.MultiCondition.Count) {
                    var c = activeCondition.MultiCondition[_ui.moveCondition.index];
                    activeCondition.MultiCondition.RemoveCondition(_ui.moveCondition.index);
                    activeCondition.MultiCondition.AddCondition(c, newPosition);
                }

                _ui.moveCondition = (-1, 0);
            }

            if (_ui.deleteCondition >= 0) {
                update = true;

                activeCondition.MultiCondition.RemoveCondition(_ui.deleteCondition);

                _ui.deleteCondition = -1;
            }

            if (usedConditionLoopPopup) {
                ImGui.OpenPopup(Popups.UsedConditionWouldCreateLoop);
            }

            // Popups

            var _ready = true;
            using (var popup = ImRaii.PopupModal(Popups.AddedConditionWouldCreateLoop, ref _ready, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize)) {
                if (popup) {
                    ImGui.Text("Adding that condition would result in an infinite loop.");
                    if (ImGui.Button("OK"))
                        ImGui.CloseCurrentPopup();
                }
            }

            _ready = true;
            using (var popup = ImRaii.PopupModal(Popups.UsedConditionWouldCreateLoop, ref _ready, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize)) {
                if (popup) {
                    ImGui.Text("Using that condition would result in an infinite loop.");
                    if (ImGui.Button("OK"))
                        ImGui.CloseCurrentPopup();
                }
            }
        }
    }
}
