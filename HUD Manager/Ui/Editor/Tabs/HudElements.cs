using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using HUD_Manager.Configuration;
using HUD_Manager.Structs;
using HUD_Manager.Structs.Options;
using ImGuiNET;

namespace HUD_Manager.Ui.Editor.Tabs {
    public class HudElements {
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

        private string? Search { get; set; }

        public HudElements(Plugin plugin, Interface ui, LayoutEditor editor) {
            this.Plugin = plugin;
            this.Ui = ui;
            this.Editor = editor;
        }

        internal void Draw(SavedLayout layout, ref bool update) {
            if (ImGuiExt.IconButton(FontAwesomeIcon.Plus, "uimanager-add-hud-element")) {
                ImGui.OpenPopup(Popups.AddElement);
            }

            ImGuiExt.HoverTooltip("Add a new HUD element to this layout");

            if (ImGui.BeginPopup(Popups.AddElement)) {
                var kinds = ElementKindExt.All()
                    .OrderBy(el => el.LocalisedName(this.Plugin.Interface.Data));
                foreach (var kind in kinds) {
                    if (!ImGui.Selectable($"{kind.LocalisedName(this.Plugin.Interface.Data)}##{kind}")) {
                        continue;
                    }

                    var currentLayout = this.Plugin.Hud.ReadLayout(this.Plugin.Hud.GetActiveHudSlot());
                    var element = currentLayout.elements.FirstOrDefault(el => el.id == kind);
                    this.Plugin.Config.Layouts[this.Ui.SelectedLayout].Elements[kind] = new Element(element);

                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }

            var search = this.Search ?? string.Empty;
            if (ImGui.InputText("Search##ui-editor-search", ref search, 100)) {
                this.Search = string.IsNullOrWhiteSpace(search) ? null : search;
            }

            if (!ImGui.BeginChild("uimanager-layout-editor-elements", new Vector2(0, 0))) {
                return;
            }

            var toRemove = new List<ElementKind>();

            var sortedElements = layout.Elements
                .Where(entry => !ElementKindExt.Immutable.Contains(entry.Key))
                .Select(entry => Tuple.Create(entry.Key, entry.Value, entry.Key.LocalisedName(this.Plugin.Interface.Data)))
                .OrderBy(tuple => tuple.Item3);
            foreach (var (kind, element, name) in sortedElements) {
                if (this.Search != null && !name.ContainsIgnoreCase(this.Search)) {
                    continue;
                }

                if (!ImGui.CollapsingHeader($"{name}##{kind}-{this.Ui.SelectedLayout}")) {
                    continue;
                }

                var maxSettingWidth = 0f;

                void DrawSettingName(string name) {
                    maxSettingWidth = Math.Max(maxSettingWidth, ImGui.CalcTextSize(name).X);
                    ImGui.TextUnformatted(name);
                    ImGui.NextColumn();
                }

                ImGui.Columns(3);
                ImGui.SetColumnWidth(0, ImGui.CalcTextSize("Enabled").X + ImGui.GetStyle().ItemSpacing.X * 2);

                ImGui.TextUnformatted("Enabled");
                ImGui.NextColumn();

                DrawSettingName("Setting");

                ImGui.TextUnformatted("Control");

                ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemInnerSpacing.X - ImGui.GetStyle().ItemSpacing.X * 6);
                if (ImGuiExt.IconButton(FontAwesomeIcon.Search, $"uimanager-preview-element-{kind}")) {
                    if (this.Editor.Previews.Elements.Contains(kind)) {
                        this.Editor.Previews.Elements.Remove(kind);
                    } else {
                        this.Editor.Previews.Elements.Add(kind);
                    }
                }

                ImGuiExt.HoverTooltip("Toggle a movable preview for this element");

                ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X * 3);
                if (ImGuiExt.IconButton(FontAwesomeIcon.TrashAlt, $"uimanager-remove-element-{kind}")) {
                    toRemove.Add(kind);
                }

                ImGuiExt.HoverTooltip("Remove this element from this layout");

                ImGui.Separator();

                void DrawEnabledCheckbox(ElementKind kind, ElementComponent component, ref bool update) {
                    ImGui.NextColumn();

                    var enabled = element[component];
                    if (ImGui.Checkbox($"###{component}-enabled-{kind}", ref enabled)) {
                        element[component] = enabled;
                        this.Plugin.Config.Save();

                        update = true;
                    }

                    ImGui.NextColumn();
                }

                if (!kind.IsJobGauge()) {
                    DrawEnabledCheckbox(element.Id, ElementComponent.Visibility, ref update);
                    DrawSettingName("Visibility");

                    var keyboard = element[VisibilityFlags.Keyboard];
                    if (ImGuiExt.IconCheckbox(FontAwesomeIcon.Keyboard, ref keyboard, $"{kind}")) {
                        element[VisibilityFlags.Keyboard] = keyboard;
                        update = true;
                    }

                    ImGui.SameLine();
                    var gamepad = element[VisibilityFlags.Gamepad];
                    if (ImGuiExt.IconCheckbox(FontAwesomeIcon.Gamepad, ref gamepad, $"{kind}")) {
                        element[VisibilityFlags.Gamepad] = gamepad;
                        update = true;
                    }
                }

                ImGui.NextColumn();
                ImGui.NextColumn();

                DrawSettingName("Measured from");

                ImGui.PushItemWidth(-1);
                var measuredFrom = element.MeasuredFrom;
                if (ImGui.BeginCombo($"##measured-from-{kind}", measuredFrom.Name())) {
                    foreach (var measured in (MeasuredFrom[]) Enum.GetValues(typeof(MeasuredFrom))) {
                        if (!ImGui.Selectable($"{measured.Name()}##{kind}", measuredFrom == measured)) {
                            continue;
                        }

                        element.MeasuredFrom = measured;
                        update = true;
                    }

                    ImGui.EndCombo();
                }

                ImGui.PopItemWidth();

                DrawEnabledCheckbox(element.Id, ElementComponent.X, ref update);
                DrawSettingName("X");

                if (this.Plugin.Config.PositioningMode == PositioningMode.Percentage) {
                    ImGui.PushItemWidth(-1);
                    var x = element.X;
                    if (ImGui.DragFloat($"##x-{kind}", ref x, this.Editor.DragSpeed)) {
                        element.X = x;
                        update = true;
                        if (this.Editor.Previews.Elements.Contains(kind)) {
                            this.Editor.Previews.Update.Add(kind);
                        }
                    }

                    ImGui.PopItemWidth();

                    DrawEnabledCheckbox(element.Id, ElementComponent.Y, ref update);
                    DrawSettingName("Y");

                    ImGui.PushItemWidth(-1);
                    var y = element.Y;
                    if (ImGui.DragFloat($"##y-{kind}", ref y, this.Editor.DragSpeed)) {
                        element.Y = y;
                        update = true;
                        if (this.Editor.Previews.Elements.Contains(kind)) {
                            this.Editor.Previews.Update.Add(kind);
                        }
                    }

                    ImGui.PopItemWidth();
                } else {
                    var screen = ImGui.GetIO().DisplaySize;

                    ImGui.PushItemWidth(-1);
                    var x = (int) Math.Round(element.X * screen.X / 100);
                    if (ImGui.InputInt($"##x-{kind}", ref x)) {
                        element.X = x / screen.X * 100;
                        update = true;
                        if (this.Editor.Previews.Elements.Contains(kind)) {
                            this.Editor.Previews.Update.Add(kind);
                        }
                    }

                    ImGui.PopItemWidth();

                    DrawEnabledCheckbox(element.Id, ElementComponent.Y, ref update);
                    DrawSettingName("Y");

                    ImGui.PushItemWidth(-1);
                    var y = (int) Math.Round(element.Y * screen.Y / 100);
                    if (ImGui.InputInt($"##y-{kind}", ref y)) {
                        element.Y = y / screen.Y * 100;
                        update = true;
                        if (this.Editor.Previews.Elements.Contains(kind)) {
                            this.Editor.Previews.Update.Add(kind);
                        }
                    }

                    ImGui.PopItemWidth();
                }

                DrawEnabledCheckbox(element.Id, ElementComponent.Scale, ref update);
                DrawSettingName("Scale");

                ImGui.PushItemWidth(-1);
                var currentScale = $"{element.Scale * 100}%";
                if (ImGui.BeginCombo($"##scale-{kind}", currentScale)) {
                    foreach (var scale in ScaleOptions) {
                        if (!ImGui.Selectable($"{scale * 100}%", Math.Abs(scale - element.Scale) < float.Epsilon)) {
                            continue;
                        }

                        element.Scale = scale;
                        update = true;
                    }

                    ImGui.EndCombo();
                }

                ImGui.PopItemWidth();

                if (!kind.IsJobGauge()) {
                    DrawEnabledCheckbox(element.Id, ElementComponent.Opacity, ref update);
                    DrawSettingName("Opacity");

                    ImGui.PushItemWidth(-1);
                    var opacity = (int) element.Opacity;
                    if (ImGui.DragInt($"##opacity-{kind}", ref opacity, 1, 1, 255)) {
                        element.Opacity = (byte) opacity;
                        update = true;
                    }

                    ImGui.PopItemWidth();
                }

                if (kind == ElementKind.TargetBar) {
                    var targetBarOpts = new TargetBarOptions(element.Options);

                    ImGui.NextColumn();
                    ImGui.NextColumn();
                    DrawSettingName("Display target information independently");

                    ImGui.PushItemWidth(-1);
                    var independent = targetBarOpts.ShowIndependently;
                    if (ImGui.Checkbox($"##display-target-info-indep-{kind}", ref independent)) {
                        targetBarOpts.ShowIndependently = independent;
                        update = true;
                    }

                    ImGui.PopItemWidth();
                }

                if (kind == ElementKind.StatusEffects) {
                    var statusOpts = new StatusOptions(element.Options);

                    ImGui.NextColumn();
                    ImGui.NextColumn();
                    DrawSettingName("Style");

                    ImGui.PushItemWidth(-1);
                    if (ImGui.BeginCombo($"##style-{kind}", statusOpts.Style.Name())) {
                        foreach (var style in (StatusStyle[]) Enum.GetValues(typeof(StatusStyle))) {
                            if (!ImGui.Selectable($"{style.Name()}##{kind}", style == statusOpts.Style)) {
                                continue;
                            }

                            statusOpts.Style = style;
                            update = true;
                        }

                        ImGui.EndCombo();
                    }

                    ImGui.PopItemWidth();
                }

                if (kind == ElementKind.StatusInfoEnhancements || kind == ElementKind.StatusInfoEnfeeblements || kind == ElementKind.StatusInfoOther) {
                    var statusOpts = new StatusInfoOptions(kind, element.Options);

                    ImGui.NextColumn();
                    ImGui.NextColumn();
                    DrawSettingName("Layout");

                    ImGui.PushItemWidth(-1);
                    if (ImGui.BeginCombo($"##layout-{kind}", statusOpts.Layout.Name())) {
                        foreach (var sLayout in (StatusLayout[]) Enum.GetValues(typeof(StatusLayout))) {
                            if (!ImGui.Selectable($"{sLayout.Name()}##{kind}", sLayout == statusOpts.Layout)) {
                                continue;
                            }

                            statusOpts.Layout = sLayout;
                            update = true;
                        }

                        ImGui.EndCombo();
                    }

                    ImGui.PopItemWidth();

                    ImGui.NextColumn();
                    ImGui.NextColumn();
                    DrawSettingName("Alignment");

                    ImGui.PushItemWidth(-1);
                    if (ImGui.BeginCombo($"##alignment-{kind}", statusOpts.Alignment.Name())) {
                        foreach (var alignment in (StatusAlignment[]) Enum.GetValues(typeof(StatusAlignment))) {
                            if (!ImGui.Selectable($"{alignment.Name()}##{kind}", alignment == statusOpts.Alignment)) {
                                continue;
                            }

                            statusOpts.Alignment = alignment;
                            update = true;
                        }

                        ImGui.EndCombo();
                    }

                    ImGui.PopItemWidth();

                    ImGui.NextColumn();
                    ImGui.NextColumn();
                    DrawSettingName("Focusable by gamepad");

                    ImGui.PushItemWidth(-1);
                    var focusable = statusOpts.Gamepad == StatusGamepad.Focusable;
                    if (ImGui.Checkbox($"##focusable-by-gamepad-{kind}", ref focusable)) {
                        statusOpts.Gamepad = focusable ? StatusGamepad.Focusable : StatusGamepad.NonFocusable;
                        update = true;
                    }

                    ImGui.PopItemWidth();
                }

                if (kind.IsHotbar()) {
                    var hotbarOpts = new HotbarOptions(element);

                    if (kind != ElementKind.PetHotbar) {
                        ImGui.NextColumn();
                        ImGui.NextColumn();
                        DrawSettingName("Hotbar number");

                        ImGui.PushItemWidth(-1);
                        var hotbarIndex = hotbarOpts.Index + 1;
                        if (ImGui.InputInt($"##hotbar-number-{kind}", ref hotbarIndex)) {
                            hotbarOpts.Index = (byte) Math.Max(0, Math.Min(9, hotbarIndex - 1));
                            update = true;
                        }

                        ImGui.PopItemWidth();
                    }

                    ImGui.NextColumn();
                    ImGui.NextColumn();
                    DrawSettingName("Hotbar layout");


                    ImGui.PushItemWidth(-1);
                    if (ImGui.BeginCombo($"##hotbar-layout-{kind}", hotbarOpts.Layout.Name())) {
                        foreach (var hotbarLayout in (HotbarLayout[]) Enum.GetValues(typeof(HotbarLayout))) {
                            if (!ImGui.Selectable($"{hotbarLayout.Name()}##{kind}", hotbarLayout == hotbarOpts.Layout)) {
                                continue;
                            }

                            hotbarOpts.Layout = hotbarLayout;
                            update = true;
                        }

                        ImGui.EndCombo();
                    }

                    ImGui.PopItemWidth();
                }

                if (kind.IsJobGauge()) {
                    var gaugeOpts = new GaugeOptions(element.Options);

                    ImGui.NextColumn();
                    ImGui.NextColumn();
                    DrawSettingName("Simple");

                    ImGui.PushItemWidth(-1);
                    var simple = gaugeOpts.Style == GaugeStyle.Simple;
                    if (ImGui.Checkbox($"##simple-{kind}", ref simple)) {
                        gaugeOpts.Style = simple ? GaugeStyle.Simple : GaugeStyle.Normal;
                        update = true;
                    }

                    ImGui.PopItemWidth();
                }

                ImGui.SetColumnWidth(1, maxSettingWidth + ImGui.GetStyle().ItemSpacing.X * 2);

                ImGui.Columns();
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
