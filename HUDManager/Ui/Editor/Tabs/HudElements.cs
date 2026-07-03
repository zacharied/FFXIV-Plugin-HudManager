using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using HUDManager.Configuration;
using HUDManager.Structs;
using HUDManager.Structs.Options;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace HUDManager.Ui.Editor.Tabs;

public class HudElements {
    private static readonly float[] ScaleOptions = [
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
    ];

    private Plugin Plugin { get; }
    private Interface Ui { get; }
    private LayoutEditor Editor { get; }

    private string? SearchAdd { get; set; }
    private string? SearchEdit { get; set; }

    public HudElements(Plugin plugin, Interface ui, LayoutEditor editor) {
        Plugin = plugin;
        Ui = ui;
        Editor = editor;
    }

    internal void Draw(SavedLayout layout, ref bool update) {
        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Plus, "Add element##uimanager-add-hud-element")) {
            ImGui.OpenPopup(Popups.AddElement);
        }
        ImGuiExt.HoverTooltip("Add a new HUD element to this layout");

        DrawAddElementPopup(ref update);
        DrawAddElementErrorPopup();

        var searchEdit = SearchEdit ?? string.Empty;
        if (ImGui.InputText("Search##ui-editor-search-edit", ref searchEdit, 100)) {
            SearchEdit = string.IsNullOrWhiteSpace(searchEdit) ? null : searchEdit;
        }

        using (var child = ImRaii.Child("uimanager-layout-editor-elements", new Vector2(0, 0))) {
            if (child) {
                DrawElements(layout, ref update);
            }
        }
    }

    private void DrawAddElementPopup(ref bool update) {
        using var popup = ImRaii.Popup(Popups.AddElement);
        if (!popup) return;

        var searchAdd = SearchAdd ?? string.Empty;
        if (ImGui.InputTextWithHint("##ui-editor-search-add", "Search", ref searchAdd, 100)) {
            SearchAdd = string.IsNullOrWhiteSpace(searchAdd) ? null : searchAdd;
        }

        if (ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows) && !ImGui.IsAnyItemActive() && !ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            ImGui.SetKeyboardFocusHere(-1);

        using var child = ImRaii.Child("##ui-editor-scrolling-search-add", ImGuiHelpers.ScaledVector2(0, 400), true, ImGuiWindowFlags.AlwaysVerticalScrollbar | ImGuiWindowFlags.NoBackground);
        if (!child) return;

        var kinds = ElementKindExt.All()
            .Where(el => el.IsRealElement())
            .OrderBy(el => Util.ZeroPadNumbers(el.LocalisedName(Plugin.DataManager)));
        foreach (var kind in kinds) {
            var elementClassJob = kind.ClassJob();
            var isForbiddenElement = elementClassJob != null && !Util.HasUnlockedClass(elementClassJob.Value);
            var elementInConfig = Plugin.Config.Layouts[Ui.SelectedLayout].Elements.ContainsKey(kind);
            var localisedName = kind.LocalisedName(Plugin.DataManager);

            if (searchAdd == string.Empty || localisedName.Contains(searchAdd, StringComparison.InvariantCultureIgnoreCase)) {
                using var color = ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.ParsedGreen, elementInConfig);

                var _selected = false;
                var selectableSelected = ImGui.Selectable($"{localisedName}##{kind}", ref _selected,
                    isForbiddenElement || elementInConfig ? ImGuiSelectableFlags.Disabled : ImGuiSelectableFlags.None);

                if (selectableSelected) {
                    try {
                        var currentLayout = Hud.ReadLayout(Hud.GetActiveHudSlot());
                        var element = currentLayout.elements.First(el => el.id == kind);
                        Plugin.Config.Layouts[Ui.SelectedLayout].Elements[kind] = new Element(element);
                    } catch (InvalidOperationException) {
                        ImGui.OpenPopup(Popups.ErrorAddingHudElement);
                        break;
                    }

                    update = true;

                    ImGui.CloseCurrentPopup();
                }
            }
        }
    }


    private void DrawAddElementErrorPopup() {
        var popupOpen = true;
        using var errorPopup = ImRaii.PopupModal(Popups.ErrorAddingHudElement, ref popupOpen, ImGuiWindowFlags.AlwaysAutoResize);
        if (!errorPopup) return;

        ImGui.Text("An error has occurred when attempting to add that element."
                   + "\nPlease ensure that element has been visible on your screen at least once."
                   + "\nIf it has been visible and the issue persists, you have found a bug!"
                   + "\nPlease report it on the plugin's GitHub page if possible.");

        if (ImGui.Button("OK")) ImGui.CloseCurrentPopup();
    }

    private void DrawElements(SavedLayout layout, ref bool update) {

        var toRemove = new List<ElementKind>();

        var sortedElements = layout.Elements
            .Where(entry => !ElementKindExt.Immutable.Contains(entry.Key) && entry.Key.IsRealElement())
            .Select(entry => Tuple.Create(entry.Key, entry.Value, entry.Key.LocalisedName(Plugin.DataManager)))
            .OrderBy(tuple => Util.ZeroPadNumbers(tuple.Item3));
        foreach (var (kind, element, name) in sortedElements) {
            if (SearchEdit != null && !name.ContainsIgnoreCase(SearchEdit)) {
                continue;
            }

            var header = ImGui.CollapsingHeader($"{name}##{kind}-{Ui.SelectedLayout}");
            if (Plugin.Config.UseLayoutListTreeView) {
                using var drag = Editor.ElementDragDrop.Drag();
                if (drag) {
                    Editor.ElementDragDrop.SourceLayout = layout;
                    Editor.ElementDragDrop.SourceElement = element.Clone();
                    Editor.ElementDragDrop.SourceName = element.Id.LocalisedName(Plugin.DataManager);
                }
            }
            if (header)
                DrawElementTable(layout, element, kind, toRemove, ref update);
        }

        foreach (var remove in toRemove) {
            layout.Elements.Remove(remove);
        }
    }

    private void DrawElementTable(SavedLayout layout, Element element, ElementKind kind, List<ElementKind> toRemove, ref bool update) {
        bool HasParent() => layout.Parent != Guid.Empty;

        static void DrawSettingName(string name) {
            ImGui.AlignTextToFramePadding();
            ImGui.Text(name);
            ImGui.TableNextColumn();
        }

        static void DrawSettingNameWithHelp(string name, string help) {
            ImGui.AlignTextToFramePadding();
            ImGui.Text(name);
            ImGuiComponents.HelpMarker(help);
            ImGui.TableNextColumn();
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
            return;
        }

        var rowCount = 3 - (HasParent() ? 0 : 1); // Disable "enabled" column for layouts with no parent.
        using var table = ImRaii.Table($"uimanager-element-table-{kind}", rowCount, ImGuiTableFlags.BordersInner | ImGuiTableFlags.PadOuterX | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg);
        if (!table) return;

        if (HasParent())
            ImGui.TableSetupColumn("Enabled");
        ImGui.TableSetupColumn("Setting");
        ImGui.TableSetupColumn("Control", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableHeadersRow();

        ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetFrameHeight() * 2 - ImGui.GetStyle().ItemSpacing.X);

        var previewing = Editor.Previews.Elements.Contains(kind);
        using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.ParsedGreen, previewing)) {
            if (ImGuiExt.IconButton(FontAwesomeIcon.Search, $"uimanager-preview-element-{kind}")) {
                if (previewing) {
                    Editor.Previews.Elements.Remove(kind);
                } else {
                    Editor.Previews.Elements.Add(kind);
                }
            }
        }

        ImGuiExt.HoverTooltip("Toggle a movable preview for this element");

        ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetFrameHeight());
        if (ImGuiExt.IconButtonEnabledWhen(ImGui.GetIO().KeyCtrl, FontAwesomeIcon.TrashAlt, $"uimanager-remove-element-{kind}")) {
            toRemove.Add(kind);
            update = true;
        }
        ImGuiExt.HoverTooltip("Remove this element from this layout (hold Control to allow)");

        ImGui.TableNextRow();

        void DrawEnabledCheckboxIfParent(ElementKind kind, ElementComponent component, ref bool update, bool nextCol = true) {
            if (nextCol) {
                ImGui.TableNextColumn();
            }

            if (!HasParent())
                return;

            var enabled = element[component];
            ImCursor.ToNestedRect(new Vector2(ImGui.GetFrameHeight(), 0), new Vector2(ImGui.GetColumnWidth(), 0), ImAlign.Top);
            if (ImGui.Checkbox($"###{component}-enabled-{kind}", ref enabled)) {
                element[component] = enabled;
                Plugin.Config.Save();

                update = true;
            }

            ImGui.TableNextColumn();
        }

        void NextColumnIfParent() {
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

        var measuredFrom = element.MeasuredFrom;
        using (ImRaii.ItemWidth(-1)) {
            if (ImGuiExt.EnumCombo($"##measured-from:{kind}", ref measuredFrom)) {
                element.MeasuredFrom = measuredFrom;
                update = true;
            }
        }

        ImGui.TableNextRow();

        DrawEnabledCheckboxIfParent(element.Id, ElementComponent.X, ref update);
        DrawSettingName("X");

        bool xUpdate = false, yUpdate = false;
        if (Plugin.Config.PositioningMode == PositioningMode.Percentage) {
            using (ImRaii.ItemWidth(-1)) {
                var x = element.X;
                if (ImGui.DragFloat($"##x-{kind}", ref x, Plugin.Config.DragSpeed)) {
                    element.X = x;
                    update = true;

                    xUpdate = true;

                    if (Editor.Previews.Elements.Contains(kind)) {
                        Editor.Previews.Update.Add(kind);
                    }
                }
            }
            ImGui.TableNextRow();

            DrawEnabledCheckboxIfParent(element.Id, ElementComponent.Y, ref update);
            DrawSettingName("Y");

            using (ImRaii.ItemWidth(-1)) {
                var y = element.Y;
                if (ImGui.DragFloat($"##y-{kind}", ref y, Plugin.Config.DragSpeed)) {
                    element.Y = y;
                    update = true;

                    yUpdate = true;

                    if (Editor.Previews.Elements.Contains(kind)) {
                        Editor.Previews.Update.Add(kind);
                    }
                }
            }
        } else {
            var screen = ImGui.GetIO().DisplaySize;

            using (ImRaii.ItemWidth(-1)) {
                var x = (int)Math.Round(element.X * screen.X / 100);
                if (ImGui.InputInt($"##x-{kind}", ref x)) {
                    element.X = x / screen.X * 100;
                    update = true;

                    xUpdate = true;

                    if (Editor.Previews.Elements.Contains(kind)) {
                        Editor.Previews.Update.Add(kind);
                    }
                }
            }
            ImGui.TableNextRow();

            DrawEnabledCheckboxIfParent(element.Id, ElementComponent.Y, ref update);
            DrawSettingName("Y");

            using (ImRaii.ItemWidth(-1)) {
                var y = (int)Math.Round(element.Y * screen.Y / 100);
                if (ImGui.InputInt($"##y-{kind}", ref y)) {
                    element.Y = y / screen.Y * 100;
                    update = true;

                    yUpdate = true;

                    if (Editor.Previews.Elements.Contains(kind)) {
                        Editor.Previews.Update.Add(kind);
                    }
                }
            }
        }

        if (xUpdate && !HasParent())
            element[ElementComponent.X] = true;
        if (yUpdate && !HasParent())
            element[ElementComponent.Y] = true;

        ImGui.TableNextRow();

        DrawEnabledCheckboxIfParent(element.Id, ElementComponent.Scale, ref update);
        DrawSettingName("Scale");

        var currentScale = $"{Math.Floor(element.Scale * 100)}%";
        using (ImRaii.ItemWidth(-1))
        using (var combo = ImRaii.Combo($"##scale-{kind}", currentScale)) {
            if (combo) {
                foreach (var scale in ScaleOptions) {
                    if (!ImGui.Selectable($"{Math.Floor(scale * 100)}%", Math.Abs(scale - element.Scale) < float.Epsilon)) {
                        continue;
                    }

                    element.Scale = scale;
                    update = true;

                    if (!HasParent())
                        element[ElementComponent.Scale] = true;
                }
            }
        }

        ImGui.TableNextRow();

        if (kind.ClassJob() == null) {
            DrawEnabledCheckboxIfParent(element.Id, ElementComponent.Opacity, ref update);
            DrawSettingName("Opacity");

            using (ImRaii.ItemWidth(-1)) {
                var opacity = (int)element.Opacity;
                if (ImGui.DragInt($"##opacity-{kind}", ref opacity, 1, 1, 255)) {
                    element.Opacity = (byte)opacity;
                    update = true;

                    if (!HasParent())
                        element[ElementComponent.Opacity] = true;
                }
            }

            ImGui.TableNextRow();
        }

        if (kind == ElementKind.TargetBar && element.Options is not null) {
            var targetBarOpts = new TargetBarOptions(element.Options);

            NextColumnIfParent();
            ImGui.TableNextColumn();
            DrawSettingName("Display target information independently");

            using (ImRaii.ItemWidth(-1)) {
                var independent = targetBarOpts.ShowIndependently;
                if (ImGui.Checkbox($"##display-target-info-indep-{kind}", ref independent)) {
                    targetBarOpts.ShowIndependently = independent;
                    update = true;
                }
            }

            ImGui.TableNextRow();
        }

        if (kind == ElementKind.StatusEffects && element.Options is not null) {
            var statusOpts = new StatusEffectsOptions(element.Options);

            NextColumnIfParent();
            ImGui.TableNextColumn();
            DrawSettingNameWithHelp("Alignment", "Only applies if grouping (set below) is set to single element.");

            using (ImRaii.ItemWidth((-1))) {
                var alignment = statusOpts.Alignment;
                if (ImGuiExt.EnumCombo($"##alignment:{kind}", ref alignment)) {
                    statusOpts.Alignment = alignment;
                    update = true;
                }
            }

            NextColumnIfParent();
            ImGui.TableNextColumn();
            DrawSettingName("Grouping");

            using (ImRaii.ItemWidth(-1)) {
                var grouping = statusOpts.Grouping;
                if (ImGuiExt.EnumCombo($"##grouping:{kind}", ref grouping)) {
                    statusOpts.Grouping = grouping;
                    update = true;
                }
            }

            ImGui.TableNextRow();
        }

        if (kind is ElementKind.StatusInfoEnhancements && element.Options is not null) {
            var statusOpts = new StatusInfoEnhancementsOptions(element);

            NextColumnIfParent();
            ImGui.TableNextColumn();
            DrawSettingName("Layout");

            using (ImRaii.ItemWidth(-1)) {
                var statusLayout = statusOpts.Layout;
                if (ImGuiExt.EnumCombo($"##grouping:{kind}", ref statusLayout)) {
                    statusOpts.Layout = statusLayout;
                    update = true;

                    if (Editor.Previews.Elements.Contains(kind)) {
                        Editor.Previews.Update.Add(kind);
                    }
                }
            }

            ImGui.TableNextRow();

            NextColumnIfParent();
            ImGui.TableNextColumn();
            DrawSettingName("Display settings");

            using (ImRaii.ItemWidth(-1)) {
                var displaySettings = statusOpts.DisplaySettings;
                if (ImGuiExt.EnumCombo($"##display-settings:{kind}", ref displaySettings)) {
                    statusOpts.DisplaySettings = displaySettings;
                    update = true;
                }
            }

            ImGui.TableNextRow();

            NextColumnIfParent();
            ImGui.TableNextColumn();
            DrawSettingName("Focusable by gamepad");

            using (ImRaii.ItemWidth(-1)) {
                var focusable = statusOpts.Gamepad == GamepadFocusType.Focusable;
                if (ImGui.Checkbox($"##focusable-by-gamepad:{kind}", ref focusable)) {
                    statusOpts.Gamepad = focusable ? GamepadFocusType.Focusable : GamepadFocusType.NonFocusable;
                    update = true;
                }
            }

            ImGui.TableNextRow();
        }

        if (kind is ElementKind.StatusInfoConditionalEnhancements && element.Options is not null) {
            var statusOpts = new StatusInfoConditionalOptions(element);

            NextColumnIfParent();
            ImGui.TableNextColumn();
            DrawSettingName("Layout");

            using (ImRaii.ItemWidth(-1)) {
                var statusLayout = statusOpts.Layout;
                if (ImGuiExt.EnumCombo($"##grouping:{kind}", ref statusLayout)) {
                    statusOpts.Layout = statusLayout;
                    update = true;

                    if (Editor.Previews.Elements.Contains(kind)) {
                        Editor.Previews.Update.Add(kind);
                    }
                }
            }

            ImGui.TableNextRow();

            NextColumnIfParent();
            ImGui.TableNextColumn();
            DrawSettingName("Focusable by gamepad");

            using (ImRaii.ItemWidth(-1)) {
                var focusable = statusOpts.Gamepad == GamepadFocusType.Focusable;
                if (ImGui.Checkbox($"##focusable-by-gamepad:{kind}", ref focusable)) {
                    statusOpts.Gamepad = focusable ? GamepadFocusType.Focusable : GamepadFocusType.NonFocusable;
                    update = true;
                }
            }

            ImGui.TableNextRow();
        }

        if (kind is ElementKind.StatusInfoEnfeeblements && element.Options is not null) {
            var statusOpts = new StatusInfoEnfeeblementsOptions(element);

            NextColumnIfParent();
            ImGui.TableNextColumn();
            DrawSettingName("Layout");

            using (ImRaii.ItemWidth(-1)) {
                var statusLayout = statusOpts.Layout;
                if (ImGuiExt.EnumCombo($"##grouping:{kind}", ref statusLayout)) {
                    statusOpts.Layout = statusLayout;
                    update = true;

                    if (Editor.Previews.Elements.Contains(kind)) {
                        Editor.Previews.Update.Add(kind);
                    }
                }
            }

            ImGui.TableNextRow();

            NextColumnIfParent();
            ImGui.TableNextColumn();
            DrawSettingName("Focusable by gamepad");

            using (ImRaii.ItemWidth(-1)) {
                var focusable = statusOpts.Gamepad == GamepadFocusType.Focusable;
                if (ImGui.Checkbox($"##focusable-by-gamepad:{kind}", ref focusable)) {
                    statusOpts.Gamepad = focusable ? GamepadFocusType.Focusable : GamepadFocusType.NonFocusable;
                    update = true;
                }
            }

            ImGui.TableNextRow();
        }

        if (kind is ElementKind.StatusInfoOther && element.Options is not null) {
            var statusOpts = new StatusInfoOtherOptions(element);

            NextColumnIfParent();
            ImGui.TableNextColumn();
            DrawSettingName("Layout");

            using (ImRaii.ItemWidth(-1)) {
                var statusLayout = statusOpts.Layout;
                if (ImGuiExt.EnumCombo($"##grouping:{kind}", ref statusLayout)) {
                    statusOpts.Layout = statusLayout;
                    update = true;

                    if (Editor.Previews.Elements.Contains(kind)) {
                        Editor.Previews.Update.Add(kind);
                    }
                }
            }

            ImGui.TableNextRow();

            NextColumnIfParent();
            ImGui.TableNextColumn();
            DrawSettingName("Focusable by gamepad");

            using (ImRaii.ItemWidth(-1)) {
                var focusable = statusOpts.Gamepad == GamepadFocusType.Focusable;
                if (ImGui.Checkbox($"##focusable-by-gamepad:{kind}", ref focusable)) {
                    statusOpts.Gamepad = focusable ? GamepadFocusType.Focusable : GamepadFocusType.NonFocusable;
                    update = true;
                }
            }

            ImGui.TableNextRow();
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

                    using (ImRaii.ItemWidth(-1)) {
                        var hotbarIndex = hotbarOpts.Index + 1;
                        if (ImGui.InputInt($"##hotbar-number-{kind}", ref hotbarIndex, 1, 1)) {
                            hotbarOpts.Index = (byte)Math.Clamp(hotbarIndex - 1, 0, 9);
                            update = true;
                        }
                    }
                }

                ImGui.TableNextRow();
            }

            NextColumnIfParent();
            ImGui.TableNextColumn();
            DrawSettingName("Hotbar layout");

            using (ImRaii.ItemWidth(-1)) {
                var hotbarLayout = hotbarOpts.Layout;
                if (ImGuiExt.EnumCombo($"##grouping:{kind}", ref hotbarLayout)) {
                    hotbarOpts.Layout = hotbarLayout;
                    update = true;

                    if (Editor.Previews.Elements.Contains(kind)) {
                        Editor.Previews.Update.Add(kind);
                    }
                }
            }

            ImGui.TableNextRow();
        }

        if (kind.ClassJob() != null && element.Options is not null) {
            NextColumnIfParent();
            ImGui.TableNextColumn();
            DrawSettingName("Simple");

            var gaugeOpts = new GaugeOptions(element.Options);
            var simple = gaugeOpts.Style == GaugeStyle.Simple;
            using (ImRaii.ItemWidth(-1)) {
                if (ImGui.Checkbox($"##simple-{kind}", ref simple)) {
                    gaugeOpts.Style = simple ? GaugeStyle.Simple : GaugeStyle.Normal;
                    update = true;
                }
            }

            ImGui.TableNextRow();
        }

        if (kind is ElementKind.PartyList && element.Options is not null) {
            NextColumnIfParent();
            ImGui.TableNextColumn();
            DrawSettingName("Alignment");

            var partyListOpts = new PartyListOptions(element.Options);

            using (ImRaii.ItemWidth(-1)) {
                var partyListAlignment = partyListOpts.Alignment;
                if (ImGuiExt.EnumCombo($"##partylist-alignment:{kind}", ref partyListAlignment)) {
                    partyListOpts.Alignment = partyListAlignment;
                    update = true;
                }
            }

            ImGui.TableNextRow();
        }
    }
}
