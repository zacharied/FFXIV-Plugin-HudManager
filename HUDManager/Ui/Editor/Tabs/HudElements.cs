using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Style;
using Dalamud.Interface.Utility;
using Dalamud.Logging;
using HUD_Manager.Configuration;
using HUD_Manager.Structs;
using HUD_Manager.Structs.Options;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace HUD_Manager.Ui.Editor.Tabs
{
    public class HudElements
    {
        private static ElementKind[] AdjustableLayoutKinds = {
            ElementKind.StatusInfoEnhancements,
            ElementKind.StatusInfoEnfeeblements,
            ElementKind.StatusInfoOther,
            ElementKind.StatusInfoConditionalEnhancements
        };

        private static readonly float[] ScaleOptions = {
            2.0f,
            1.8f,
            1.6f,
            1.4f,
            1.2f,
            1.1f,
            1.0f,
            0.9f,
            0.8f,
            0.6f,
        };

        private Plugin Plugin { get; }
        private Interface Ui { get; }
        private LayoutEditor Editor { get; }

        private string? SearchAdd { get; set; }
        private string? SearchEdit { get; set; }

        public HudElements(Plugin plugin, Interface ui, LayoutEditor editor)
        {
            this.Plugin = plugin;
            this.Ui = ui;
            this.Editor = editor;
        }

        internal void Draw(SavedLayout layout, ref bool update)
        {
            if (ImGuiExt.IconButton(FontAwesomeIcon.Plus, "uimanager-add-hud-element")) {
                ImGui.OpenPopup(Popups.AddElement);
            }

            bool HasParent() => layout.Parent != Guid.Empty;

            ImGuiExt.HoverTooltip("Add a new HUD element to this layout");

            if (ImGui.BeginPopup(Popups.AddElement)) {
                var searchAdd = this.SearchAdd ?? string.Empty;
                if (ImGui.InputTextWithHint("##ui-editor-search-add", "Search", ref searchAdd, 100)) {
                    this.SearchAdd = string.IsNullOrWhiteSpace(searchAdd) ? null : searchAdd;
                }

                if (ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows) && !ImGui.IsAnyItemActive() && !ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                    ImGui.SetKeyboardFocusHere(-1);

                ImGui.BeginChild("##ui-editor-scrolling-search-add", ImGuiHelpers.ScaledVector2(0, 400), true,
                    ImGuiWindowFlags.AlwaysVerticalScrollbar | ImGuiWindowFlags.NoBackground);

                var kinds = ElementKindExt.All()
                    .Where(el => el.IsRealElement())
                    .OrderBy(el => el.LocalisedName(this.Plugin.DataManager));
                foreach (var kind in kinds) {
                    var elementClassJob = kind.ClassJob();
                    var isForbiddenElement = elementClassJob != null && !Util.HasUnlockedClass(this.Plugin, elementClassJob);
                    var elementInConfig = this.Plugin.Config.Layouts[this.Ui.SelectedLayout].Elements.ContainsKey(kind);
                    var localisedName = kind.LocalisedName(this.Plugin.DataManager);

                    if (searchAdd == string.Empty || localisedName.ToLowerInvariant().Contains(searchAdd.ToLowerInvariant())) {
                        if (elementInConfig)
                            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.ParsedGreen);
                        var _selected = false;
                        var selectableSelected = ImGui.Selectable($"{localisedName}##{kind}", ref _selected,
                            isForbiddenElement || elementInConfig ? ImGuiSelectableFlags.Disabled : ImGuiSelectableFlags.None);
                        if (elementInConfig)
                            ImGui.PopStyleColor();

                        if (selectableSelected) {
                            try {
                                var currentLayout = this.Plugin.Hud.ReadLayout(this.Plugin.Hud.GetActiveHudSlot());
                                var element = currentLayout.elements.First(el => el.id == kind);
                                this.Plugin.Config.Layouts[this.Ui.SelectedLayout].Elements[kind] = new Element(element);
                            }
                            catch (InvalidOperationException) {
                                ImGui.OpenPopup(Popups.ErrorAddingHudElement);
                                if (elementInConfig)
                                    ImGui.PopStyleColor();
                                break;
                            }

                            update = true;

                            ImGui.CloseCurrentPopup();
                        }
                    }
                }

                ImGui.EndChild();
                ImGui.EndPopup();
            }

            bool popupOpen = true;
            if (ImGui.BeginPopupModal(Popups.ErrorAddingHudElement, ref popupOpen, ImGuiWindowFlags.AlwaysAutoResize)) {
                ImGui.Text("An error has occurred when attempting to add that element."
                    + "\nPlease ensure that element has been visible on your screen at least once."
                    + "\nIf it has been visible and the issue persists, you have found a bug!"
                    + "\nPlease report it on the plugin's GitHub page if possible.");

                if (ImGui.Button("OK")) ImGui.CloseCurrentPopup();
            }

            var searchEdit = this.SearchEdit ?? string.Empty;
            if (ImGui.InputText("Search##ui-editor-search-edit", ref searchEdit, 100)) {
                this.SearchEdit = string.IsNullOrWhiteSpace(searchEdit) ? null : searchEdit;
            }

            if (!ImGui.BeginChild("uimanager-layout-editor-elements", new Vector2(0, 0))) {
                return;
            }

            var toRemove = new List<ElementKind>();

            var sortedElements = layout.Elements
                .Where(entry => !ElementKindExt.Immutable.Contains(entry.Key) && entry.Key.IsRealElement())
                .Select(entry => Tuple.Create(entry.Key, entry.Value, entry.Key.LocalisedName(this.Plugin.DataManager)))
                .OrderBy(tuple => tuple.Item3);
            foreach (var (kind, element, name) in sortedElements) {
                if (this.SearchEdit != null && !name.ContainsIgnoreCase(this.SearchEdit)) {
                    continue;
                }

                if (!ImGui.CollapsingHeader($"{name}##{kind}-{this.Ui.SelectedLayout}")) {
                    continue;
                }

                // Unknown8 seems like it will be null if the element hasn't appeared yet.
                if (element.Unknown8 is null) {
                    ImGui.Text("Unable to configure this element.");
                    ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemInnerSpacing.X - ImGui.GetStyle().ItemSpacing.X * 4 * ImGuiHelpers.GlobalScale);
                    if (ImGuiExt.IconButton(FontAwesomeIcon.TrashAlt, $"uimanager-remove-element-{kind}-unk")) {
                        toRemove.Add(kind);
                        update = true;
                    }
                    ImGui.Text("Please ensure it has been visible on your screen at least once.");
                    continue;
                }

                static void DrawSettingName(string name)
                {
                    ImGui.TextUnformatted(name);
                    ImGui.TableNextColumn();
                }

                static void DrawSettingNameWithHelp(string name, string help)
                {
                    ImGui.TextUnformatted(name);
                    ImGuiComponents.HelpMarker(help);
                    ImGui.TableNextColumn();
                }

                const ImGuiTableFlags flags = ImGuiTableFlags.BordersInner
                                              | ImGuiTableFlags.PadOuterX
                                              | ImGuiTableFlags.SizingFixedFit
                                              | ImGuiTableFlags.RowBg;
                int rowCount = 3 - (HasParent() ? 0 : 1); // Disable "enabled" column for layouts with no parent.
                if (!ImGui.BeginTable($"uimanager-element-table-{kind}", rowCount, flags)) {
                    continue;
                }

                if (HasParent())
                    ImGui.TableSetupColumn("Enabled");
                ImGui.TableSetupColumn("Setting");
                ImGui.TableSetupColumn("Control", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableHeadersRow();

                ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemInnerSpacing.X - ImGui.GetStyle().ItemSpacing.X * 6);

                var previewing = this.Editor.Previews.Elements.Contains(kind);
                if (previewing)
                    ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.ParsedGreen);
                if (ImGuiExt.IconButton(FontAwesomeIcon.Search, $"uimanager-preview-element-{kind}")) {
                    if (previewing) {
                        this.Editor.Previews.Elements.Remove(kind);
                    } else {
                        this.Editor.Previews.Elements.Add(kind);
                    }
                }
                if (previewing)
                    ImGui.PopStyleColor();

                ImGuiExt.HoverTooltip("Toggle a movable preview for this element");

                ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X * 3);
                if (ImGuiExt.IconButtonEnabledWhen(ImGui.GetIO().KeyCtrl, FontAwesomeIcon.TrashAlt, $"uimanager-remove-element-{kind}")) {
                    toRemove.Add(kind);
                    update = true;
                }
                ImGuiExt.HoverTooltip("Remove this element from this layout (hold Control to allow)");

                ImGui.TableNextRow();

                void DrawEnabledCheckboxIfParent(ElementKind kind, ElementComponent component, ref bool update, bool nextCol = true)
                {
                    if (nextCol) {
                        ImGui.TableNextColumn();
                    }

                    if (!HasParent())
                        return;

                    var enabled = element[component];
                    if (ImGui.Checkbox($"###{component}-enabled-{kind}", ref enabled)) {
                        element[component] = enabled;
                        this.Plugin.Config.Save();

                        update = true;
                    }

                    ImGui.TableNextColumn();
                }

                void NextColumnIfParent()
                {
                    if (HasParent())
                        ImGui.TableNextColumn();
                }

                ImGui.TableSetColumnIndex(0);

                DrawEnabledCheckboxIfParent(element.Id, ElementComponent.Visibility, ref update, false);
                DrawSettingName("Visibility");

                bool visibilityUpdate = false;
                var keyboard = element[VisibilityFlags.Keyboard];
                if (ImGuiExt.IconCheckbox(FontAwesomeIcon.Keyboard, ref keyboard, $"{kind}")) {
                    element[VisibilityFlags.Keyboard] = keyboard;
                    update = true;
                    visibilityUpdate = true;
                }

                ImGui.SameLine();
                var gamepad = element[VisibilityFlags.Gamepad];
                if (ImGuiExt.IconCheckbox(FontAwesomeIcon.Gamepad, ref gamepad, $"{kind}")) {
                    element[VisibilityFlags.Gamepad] = gamepad;
                    update = true;
                    visibilityUpdate = true;
                }

                if (visibilityUpdate && !HasParent())
                    element[ElementComponent.Visibility] = true;

                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);

                NextColumnIfParent();

                DrawSettingName("Measured from");

                ImGui.PushItemWidth(-1);
                var measuredFrom = element.MeasuredFrom;
                if (ImGui.BeginCombo($"##measured-from-{kind}", measuredFrom.Name())) {
                    foreach (var measured in (MeasuredFrom[])Enum.GetValues(typeof(MeasuredFrom))) {
                        if (!ImGui.Selectable($"{measured.Name()}##{kind}", measuredFrom == measured)) {
                            continue;
                        }

                        element.MeasuredFrom = measured;
                        update = true;
                    }

                    ImGui.EndCombo();
                }

                ImGui.PopItemWidth();
                ImGui.TableNextRow();

                DrawEnabledCheckboxIfParent(element.Id, ElementComponent.X, ref update);
                DrawSettingName("X");

                bool xUpdate = false, yUpdate = false;
                if (this.Plugin.Config.PositioningMode == PositioningMode.Percentage) {
                    ImGui.PushItemWidth(-1);
                    var x = element.X;
                    if (ImGui.DragFloat($"##x-{kind}", ref x, this.Plugin.Config.DragSpeed)) {
                        element.X = x;
                        update = true;

                        xUpdate = true;

                        if (this.Editor.Previews.Elements.Contains(kind)) {
                            this.Editor.Previews.Update.Add(kind);
                        }
                    }

                    ImGui.PopItemWidth();
                    ImGui.TableNextRow();

                    DrawEnabledCheckboxIfParent(element.Id, ElementComponent.Y, ref update);
                    DrawSettingName("Y");

                    ImGui.PushItemWidth(-1);
                    var y = element.Y;
                    if (ImGui.DragFloat($"##y-{kind}", ref y, this.Plugin.Config.DragSpeed)) {
                        element.Y = y;
                        update = true;

                        yUpdate = true;

                        if (this.Editor.Previews.Elements.Contains(kind)) {
                            this.Editor.Previews.Update.Add(kind);
                        }
                    }

                    ImGui.PopItemWidth();
                } else {
                    var screen = ImGui.GetIO().DisplaySize;

                    ImGui.PushItemWidth(-1);
                    var x = (int)Math.Round(element.X * screen.X / 100);
                    if (ImGui.InputInt($"##x-{kind}", ref x)) {
                        element.X = x / screen.X * 100;
                        update = true;

                        xUpdate = true;

                        if (this.Editor.Previews.Elements.Contains(kind)) {
                            this.Editor.Previews.Update.Add(kind);
                        }
                    }

                    ImGui.PopItemWidth();
                    ImGui.TableNextRow();

                    DrawEnabledCheckboxIfParent(element.Id, ElementComponent.Y, ref update);
                    DrawSettingName("Y");

                    ImGui.PushItemWidth(-1);
                    var y = (int)Math.Round(element.Y * screen.Y / 100);
                    if (ImGui.InputInt($"##y-{kind}", ref y)) {
                        element.Y = y / screen.Y * 100;
                        update = true;

                        yUpdate = true;

                        if (this.Editor.Previews.Elements.Contains(kind)) {
                            this.Editor.Previews.Update.Add(kind);
                        }
                    }

                    ImGui.PopItemWidth();
                }

                if (xUpdate && !HasParent())
                    element[ElementComponent.X] = true;
                if (yUpdate && !HasParent())
                    element[ElementComponent.Y] = true;

                ImGui.TableNextRow();

                DrawEnabledCheckboxIfParent(element.Id, ElementComponent.Scale, ref update);
                DrawSettingName("Scale");

                ImGui.PushItemWidth(-1);
                var currentScale = $"{Math.Floor(element.Scale * 100)}%";
                if (ImGui.BeginCombo($"##scale-{kind}", currentScale)) {
                    foreach (var scale in ScaleOptions) {
                        if (!ImGui.Selectable($"{Math.Floor(scale * 100)}%", Math.Abs(scale - element.Scale) < float.Epsilon)) {
                            continue;
                        }

                        element.Scale = scale;
                        update = true;

                        if (!HasParent())
                            element[ElementComponent.Scale] = true;
                    }

                    ImGui.EndCombo();
                }

                ImGui.PopItemWidth();
                ImGui.TableNextRow();

                if (!kind.IsJobGauge()) {
                    DrawEnabledCheckboxIfParent(element.Id, ElementComponent.Opacity, ref update);
                    DrawSettingName("Opacity");

                    ImGui.PushItemWidth(-1);
                    var opacity = (int)element.Opacity;
                    if (ImGui.DragInt($"##opacity-{kind}", ref opacity, 1, 1, 255)) {
                        element.Opacity = (byte)opacity;
                        update = true;

                        if (!HasParent())
                            element[ElementComponent.Opacity] = true;
                    }

                    ImGui.PopItemWidth();
                    ImGui.TableNextRow();
                }

                if (kind == ElementKind.TargetBar) {
                    if (element.Options is null) {
                        goto EndTargetBar;
                    }
                    var targetBarOpts = new TargetBarOptions(element.Options);

                    NextColumnIfParent();
                    ImGui.TableNextColumn();
                    DrawSettingName("Display target information independently");

                    ImGui.PushItemWidth(-1);
                    var independent = targetBarOpts.ShowIndependently;
                    if (ImGui.Checkbox($"##display-target-info-indep-{kind}", ref independent)) {
                        targetBarOpts.ShowIndependently = independent;
                        update = true;
                    }

                    ImGui.PopItemWidth();
                    ImGui.TableNextRow();

                    EndTargetBar:;
                }

                if (kind == ElementKind.StatusEffects) {
                    if (element.Options is null)
                        goto EndStatusEffects;
                    var statusOpts = new StatusBaseOptions(element.Options);

                    NextColumnIfParent();
                    ImGui.TableNextColumn();
                    DrawSettingNameWithHelp("Alignment", "Only applies if grouping (set below) is set to single element.");

                    ImGui.PushItemWidth(-1);
                    if (ImGui.BeginCombo($"##alignment-{kind}", statusOpts.Alignment.Name())) {
                        foreach (var alignment in (StatusBaseAlignment[])Enum.GetValues(typeof(StatusBaseAlignment))) {
                            if (!ImGui.Selectable($"{alignment.Name()}##{kind}", alignment == statusOpts.Alignment)) {
                                continue;
                            }

                            statusOpts.Alignment = alignment;
                            update = true;
                        }

                        ImGui.EndCombo();
                    }

                    NextColumnIfParent();
                    ImGui.TableNextColumn();
                    DrawSettingName("Grouping");

                    ImGui.PushItemWidth(-1);
                    if (ImGui.BeginCombo($"##grouping-{kind}", statusOpts.Grouping.Name())) {
                        foreach (var grouping in StatusBaseExt.StatusGroupingOrder) {
                            if (!ImGui.Selectable($"{grouping.Name()}##{kind}", grouping == statusOpts.Grouping)) {
                                continue;
                            }

                            statusOpts.Grouping = grouping;
                            update = true;
                        }

                        ImGui.EndCombo();
                    }

                    ImGui.PopItemWidth();
                    ImGui.TableNextRow();

                    EndStatusEffects:;
                }

                if (kind is ElementKind.StatusInfoEnhancements or ElementKind.StatusInfoEnfeeblements or ElementKind.StatusInfoOther or ElementKind.StatusInfoConditionalEnhancements) {
                    if (element.Options is null)
                        goto EndStatusInfo;

                    var statusOpts = new StatusSplitOptions(element);

                    NextColumnIfParent();
                    ImGui.TableNextColumn();
                    DrawSettingName("Layout");

                    ImGui.PushItemWidth(-1);
                    if (ImGui.BeginCombo($"##layout-{kind}", statusOpts.Layout.Name())) {
                        foreach (var sLayout in (StatusSplitLayout[])Enum.GetValues(typeof(StatusSplitLayout))) {
                            if (!ImGui.Selectable($"{sLayout.Name()}##{kind}", sLayout == statusOpts.Layout)) {
                                continue;
                            }

                            statusOpts.Layout = sLayout;
                            update = true;
                        }

                        ImGui.EndCombo();
                    }

                    ImGui.PopItemWidth();
                    ImGui.TableNextRow();

                    NextColumnIfParent();
                    ImGui.TableNextColumn();
                    DrawSettingName("Alignment");

                    ImGui.PushItemWidth(-1);
                    if (ImGui.BeginCombo($"##alignment-{kind}", statusOpts.Alignment.Name())) {
                        foreach (var alignment in (StatusSplitAlignment[])Enum.GetValues(typeof(StatusSplitAlignment))) {
                            if (!ImGui.Selectable($"{alignment.Name()}##{kind}", alignment == statusOpts.Alignment)) {
                                continue;
                            }

                            statusOpts.Alignment = alignment;
                            update = true;
                        }

                        ImGui.EndCombo();
                    }

                    ImGui.PopItemWidth();
                    ImGui.TableNextRow();

                    NextColumnIfParent();
                    ImGui.TableNextColumn();
                    DrawSettingName("Focusable by gamepad");

                    ImGui.PushItemWidth(-1);
                    var focusable = statusOpts.Gamepad == StatusSplitGamepad.Focusable;
                    if (ImGui.Checkbox($"##focusable-by-gamepad-{kind}", ref focusable)) {
                        statusOpts.Gamepad = focusable ? StatusSplitGamepad.Focusable : StatusSplitGamepad.NonFocusable;
                        update = true;
                    }

                    ImGui.PopItemWidth();

                    EndStatusInfo:;
                }

                if (kind.IsHotbar()) {
                    var hotbarOpts = new HotbarOptions(element);

                    if (kind == ElementKind.Hotbar1) {
                        NextColumnIfParent();
                        ImGui.TableNextColumn();
                        DrawSettingName("Hotbar number");

                        var overwriteCycling = (element.LayoutFlags & ElementLayoutFlags.ClobberTransientOptions) != 0;
                        if (ImGui.Checkbox($"Overwrite cycling state##overwrite-cycling-{kind}", ref overwriteCycling)) {
                            if (overwriteCycling) {
                                element.LayoutFlags |= ElementLayoutFlags.ClobberTransientOptions;
                            } else {
                                element.LayoutFlags &= ~ElementLayoutFlags.ClobberTransientOptions;
                            }
                            update = true;
                        }

                        if (overwriteCycling) {
                            ImGui.SameLine();

                            ImGui.PushItemWidth(-1);
                            var hotbarIndex = hotbarOpts.Index + 1;
                            if (ImGui.InputInt($"##hotbar-number-{kind}", ref hotbarIndex)) {
                                hotbarOpts.Index = (byte)Math.Max(0, Math.Min(9, hotbarIndex - 1));
                                update = true;
                            }
                        }

                        ImGui.PopItemWidth();
                        ImGui.TableNextRow();
                    }

                    NextColumnIfParent();
                    ImGui.TableNextColumn();
                    DrawSettingName("Hotbar layout");

                    ImGui.PushItemWidth(-1);
                    if (ImGui.BeginCombo($"##hotbar-layout-{kind}", hotbarOpts.Layout.Name())) {
                        foreach (var hotbarLayout in (HotbarLayout[])Enum.GetValues(typeof(HotbarLayout))) {
                            if (!ImGui.Selectable($"{hotbarLayout.Name()}##{kind}", hotbarLayout == hotbarOpts.Layout)) {
                                continue;
                            }

                            hotbarOpts.Layout = hotbarLayout;
                            update = true;
                        }

                        ImGui.EndCombo();
                    }

                    ImGui.PopItemWidth();
                    ImGui.TableNextRow();
                }

                if (kind.IsJobGauge()) {
                    if (element.Options is null)
                        goto EndJobGauge;

                    NextColumnIfParent();
                    ImGui.TableNextColumn();
                    DrawSettingName("Simple");

                    ImGui.PushItemWidth(-1);

                    var gaugeOpts = new GaugeOptions(element.Options);

                    var simple = gaugeOpts.Style == GaugeStyle.Simple;
                    if (ImGui.Checkbox($"##simple-{kind}", ref simple)) {
                        gaugeOpts.Style = simple ? GaugeStyle.Simple : GaugeStyle.Normal;
                        update = true;
                    }

                    ImGui.PopItemWidth();
                    ImGui.TableNextRow();

                    EndJobGauge:;
                }

                ImGui.EndTable();
            }

            foreach (var remove in toRemove) {
                layout.Elements.Remove(remove);
            }

            if (update) {
                this.Plugin.Hud.WriteEffectiveLayout(this.Plugin.Config.StagingSlot, this.Ui.SelectedLayout);
                this.Plugin.Hud.SelectSlot(this.Plugin.Config.StagingSlot, true);
            }

            ImGui.EndChild();
        }
    }
}
