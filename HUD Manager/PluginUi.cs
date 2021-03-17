using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Interface;
using Dalamud.Plugin;
using HUD_Manager.Configuration;
using HUD_Manager.Structs;
using HUD_Manager.Structs.Options;
using HUD_Manager.Tree;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;

// TODO: Zone swaps?

namespace HUD_Manager {
    public class PluginUi {
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

        private bool _settingsVisible;

        public bool SettingsVisible {
            get => this._settingsVisible;
            set => this._settingsVisible = value;
        }

        private string? _editorSearch;
        private float _dragSpeed = 1.0f;

        private string? _importName;

        private string? _newLayoutName;
        private string? _renameLayout;
        private Guid _selectedEditLayout = Guid.Empty;

        private int _editingConditionIndex = -1;
        private HudConditionMatch? _editingCondition;
        private bool _scrollToAdd;

        private HashSet<ElementKind> Previews { get; } = new();
        private HashSet<ElementKind> UpdatePreviews { get; } = new();

        public PluginUi(Plugin plugin) {
            this.Plugin = plugin;
        }

        public void ConfigUi(object sender, EventArgs args) {
            this.SettingsVisible = true;
        }

        private static Tuple<Vector2, Vector2> CalcPosAndSize(Element element) {
            // get X & Y coords from the element, which are percentages (0 - 100)
            var percentagePos = new Vector2(element.X, element.Y);

            // get size in pixels
            var size = new Vector2(element.Width, element.Height);
            // scale size according to the element's scale
            size.X = (float) Math.Round(size.X * element.Scale);
            size.Y = (float) Math.Round(size.Y * element.Scale);

            // convert the percentages into pixels
            var screen = ImGui.GetIO().DisplaySize;
            var pixelPos = new Vector2(
                (float) Math.Round(percentagePos.X * screen.X / 100),
                (float) Math.Round(percentagePos.Y * screen.Y / 100)
            );

            // split the measured from into x and y parts
            var (xMeasure, yMeasure) = element.MeasuredFrom.ToParts();

            // determine subtraction values to make the coords point to the top left
            var subX = xMeasure switch {
                MeasuredX.Left => 0,
                MeasuredX.Middle => size.X / 2,
                MeasuredX.Right => size.X,
                _ => throw new ArgumentOutOfRangeException(),
            };

            var subY = yMeasure switch {
                MeasuredY.Top => 0,
                MeasuredY.Middle => size.Y / 2,
                MeasuredY.Bottom => size.Y,
                _ => throw new ArgumentOutOfRangeException(),
            };

            // transform coords to top left for ImGui
            pixelPos.X -= subX;
            pixelPos.Y -= subY;

            // round the coords
            pixelPos.X = (float) Math.Round(pixelPos.X);
            pixelPos.Y = (float) Math.Round(pixelPos.Y);

            return Tuple.Create(pixelPos, size);
        }

        private static Vector2 ConvertImGuiPos(Element element, Vector2 im) {
            // get the coordinates in pixels
            var pos = new Vector2(im.X, im.Y);

            // get the size of the element
            var size = new Vector2(element.Width, element.Height);
            // scale the size of the element
            size.X = (float) Math.Round(size.X * element.Scale);
            size.Y = (float) Math.Round(size.Y * element.Scale);

            // split the measured from into x and y parts
            var (xMeasure, yMeasure) = element.MeasuredFrom.ToParts();

            // determine how much to add to convert top left coords into the element's system
            var addX = xMeasure switch {
                MeasuredX.Left => 0,
                MeasuredX.Middle => size.X / 2,
                MeasuredX.Right => size.X,
                _ => throw new ArgumentOutOfRangeException(),
            };

            var addY = yMeasure switch {
                MeasuredY.Top => 0,
                MeasuredY.Middle => size.Y / 2,
                MeasuredY.Bottom => size.Y,
                _ => throw new ArgumentOutOfRangeException(),
            };

            // convert from top left to given type
            pos.X += addX;
            pos.Y += addY;

            // switch (element.MeasuredFrom) {
            //     case MeasuredFrom.TopLeft:
            //         // already top left
            //         break;
            //     case MeasuredFrom.TopMiddle:
            //         pos.X += size.X / 2;
            //         break;
            //     case MeasuredFrom.TopRight:
            //         pos.X += size.X;
            //         break;
            //     case MeasuredFrom.MiddleLeft:
            //         pos.Y += size.Y / 2;
            //         break;
            //     case MeasuredFrom.Middle:
            //         pos.X += size.X / 2;
            //         pos.Y += size.Y / 2;
            //         break;
            //     case MeasuredFrom.MiddleRight:
            //         pos.X += size.X;
            //         pos.Y += size.Y / 2;
            //         break;
            //     case MeasuredFrom.BottomLeft:
            //         pos.Y += size.Y;
            //         break;
            //     case MeasuredFrom.BottomMiddle:
            //         pos.X += size.X / 2;
            //         pos.Y += size.Y;
            //         break;
            //     case MeasuredFrom.BottomRight:
            //         pos.X += size.X;
            //         pos.Y += size.Y;
            //         break;
            //     default:
            //         throw new ArgumentOutOfRangeException();
            // }

            // convert the pixels into percentages
            var screen = ImGui.GetIO().DisplaySize;
            pos.X /= screen.X / 100;
            pos.Y /= screen.Y / 100;

            return pos;
        }

        private void DrawPreviews(ref bool update) {
            const float tolerance = 0.0001f;
            const ImGuiWindowFlags flags = ImGuiWindowFlags.NoTitleBar
                                           | ImGuiWindowFlags.NoResize
                                           | ImGuiWindowFlags.NoFocusOnAppearing
                                           | ImGuiWindowFlags.NoScrollbar;

            if (this._selectedEditLayout == Guid.Empty) {
                return;
            }

            if (!this.Plugin.Config.Layouts.TryGetValue(this._selectedEditLayout, out var layout)) {
                return;
            }

            foreach (var element in layout.Elements.Values) {
                if (!this.Previews.Contains(element.Id)) {
                    continue;
                }

                var (pos, size) = CalcPosAndSize(element);
                if (this.UpdatePreviews.Remove(element.Id)) {
                    ImGui.SetNextWindowPos(pos);
                } else {
                    ImGui.SetNextWindowPos(pos, ImGuiCond.Appearing);
                }

                ImGui.SetNextWindowSize(size);

                if (!ImGui.Begin($"##uimanager-preview-{element.Id}", flags)) {
                    continue;
                }

                ImGui.TextUnformatted(element.Id.LocalisedName(this.Plugin.Interface.Data));

                // determine if the window has moved and update if it has
                var newPos = ConvertImGuiPos(element, ImGui.GetWindowPos());
                if (Math.Abs(newPos.X - element.X) > tolerance || Math.Abs(newPos.Y - element.Y) > tolerance) {
                    element.X = newPos.X;
                    element.Y = newPos.Y;
                    update = true;
                }

                ImGui.End();
            }
        }

        private void DrawSettings() {
            if (!this.SettingsVisible) {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(500, 475), ImGuiCond.FirstUseEver);

            if (!ImGui.Begin(this.Plugin.Name, ref this._settingsVisible)) {
                return;
            }

            if (ImGui.BeginTabBar("##hudmanager-tabs")) {
                if (!this.Plugin.Config.UnderstandsRisks) {
                    if (ImGui.BeginTabItem("About")) {
                        ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), "Read this first");
                        ImGui.Separator();
                        ImGui.PushTextWrapPos(ImGui.GetFontSize() * 20f);
                        ImGui.TextUnformatted("HUD Manager will use the configured staging slot as its own slot to make changes to. This means the staging slot will be overwritten whenever any swap happens.");
                        ImGui.Spacing();
                        ImGui.TextUnformatted("Any HUD layout changes you make while HUD Manager is enabled may potentially be lost, no matter what slot. If you want to make changes to your HUD layout, TURN OFF HUD Manager first.");
                        ImGui.Spacing();
                        ImGui.TextUnformatted("When editing or making a new layout, to be completely safe, turn off swaps, set up your layout, import the layout into HUD Manager, then turn on swaps.");
                        ImGui.Spacing();
                        ImGui.TextUnformatted("If you are a new user, HUD Manager auto-imported your existing layouts on startup.");
                        ImGui.Spacing();
                        ImGui.TextUnformatted("Finally, HUD Manager is beta software. Back up your character data before using this plugin. You may lose some to all of your HUD layouts while testing this plugin.");
                        ImGui.Separator();
                        ImGui.TextUnformatted("If you have read all of the above and are okay with continuing, check the box below to enable HUD Manager. You only need to do this once.");
                        ImGui.PopTextWrapPos();
                        var understandsRisks = this.Plugin.Config.UnderstandsRisks;
                        if (ImGui.Checkbox("I understand", ref understandsRisks)) {
                            this.Plugin.Config.UnderstandsRisks = understandsRisks;
                            this.Plugin.Config.Save();
                        }

                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                    ImGui.End();
                    return;
                }

                this.DrawLayoutEditor();

                this.DrawSwaps();

                this.DrawHelp();

                #if DEBUG
                this.DrawDebug();
                #endif

                ImGui.EndTabBar();
            }

            ImGui.End();
        }

        private void DrawSwaps() {
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
            HelpMarker("The staging slot is the HUD layout slot that will be used as your HUD layout. All changes will be written to this slot when swaps are enabled.");

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

        private void DrawLayoutEditor() {
            if (!ImGui.BeginTabItem("Layout editor")) {
                return;
            }

            if (this.Plugin.Config.SwapsEnabled) {
                ImGui.TextUnformatted("Cannot edit layouts while swaps are enabled.");

                if (ImGui.Button("Disable swaps")) {
                    this.Plugin.Config.SwapsEnabled = false;
                    this.Plugin.Config.Save();
                }

                goto EndTabItem;
            }

            var charConfig = this.Plugin.Interface.Framework.Gui.GetAddonByName("ConfigCharacter", 1);
            if (charConfig != null && charConfig.Visible) {
                ImGui.TextUnformatted("Please close the Character Configuration window before continuing.");
                goto EndTabItem;
            }

            var update = false;

            this.DrawPreviews(ref update);

            ImGui.TextUnformatted("Layout");

            var nodes = Node<SavedLayout>.BuildTree(this.Plugin.Config.Layouts);

            this.Plugin.Config.Layouts.TryGetValue(this._selectedEditLayout, out var selected);
            var selectedName = selected?.Name ?? "<none>";

            if (ImGui.BeginCombo("##edit-layout", selectedName)) {
                if (ImGui.Selectable("<none>")) {
                    this._selectedEditLayout = Guid.Empty;
                }

                foreach (var node in nodes) {
                    foreach (var (child, depth) in node.TraverseWithDepth()) {
                        var indent = new string(' ', (int) depth * 4);
                        if (!ImGui.Selectable($"{indent}{child.Value.Name}##edit-{child.Id}", child.Id == this._selectedEditLayout)) {
                            continue;
                        }

                        this._selectedEditLayout = child.Id;
                        update = true;
                    }
                }

                ImGui.EndCombo();
            }

            ImGui.SameLine();
            if (IconButton(FontAwesomeIcon.Plus, "uimanager-add-layout")) {
                ImGui.OpenPopup(Popups.AddLayout);
            }

            HoverTooltip("Add a new layout");

            this.SetUpAddLayoutPopup();

            ImGui.SameLine();
            if (IconButton(FontAwesomeIcon.TrashAlt, "uimanager-delete-layout") && this._selectedEditLayout != Guid.Empty) {
                ImGui.OpenPopup(Popups.DeleteVerify);
            }

            this.SetUpDeleteVerifyPopup(nodes);

            HoverTooltip("Delete the selected layout");

            ImGui.SameLine();
            if (IconButton(FontAwesomeIcon.PencilAlt, "uimanager-rename-layout") && this._selectedEditLayout != Guid.Empty) {
                this._renameLayout = this.Plugin.Config.Layouts[this._selectedEditLayout].Name;
                ImGui.OpenPopup(Popups.RenameLayout);
            }

            HoverTooltip("Rename the selected layout");

            this.SetUpRenameLayoutPopup();

            ImGui.SameLine();
            if (IconButton(FontAwesomeIcon.FileImport, "uimanager-import-layout")) {
                ImGui.OpenPopup(Popups.ImportLayout);
            }

            HoverTooltip("Import a layout from an in-game HUD slot or the clipboard");

            this.SetUpImportLayoutPopup();

            ImGui.SameLine();
            if (IconButton(FontAwesomeIcon.FileExport, "uimanager-export-layout")) {
                ImGui.OpenPopup(Popups.ExportLayout);
            }

            HoverTooltip("Export a layout to an in-game HUD slot or the clipboard");

            this.SetUpExportLayoutPopup();

            if (this._selectedEditLayout == Guid.Empty) {
                goto EndTabItem;
            }

            var layout = this.Plugin.Config.Layouts[this._selectedEditLayout];

            this.Plugin.Config.Layouts.TryGetValue(layout.Parent, out var parent);
            var parentName = parent?.Name ?? "<none>";

            var ourChildren = nodes.Find(this._selectedEditLayout)
                ?.Traverse()
                .Select(el => el.Id)
                .ToArray() ?? new Guid[0];

            if (ImGui.BeginCombo("Parent", parentName)) {
                if (ImGui.Selectable("<none>")) {
                    layout.Parent = Guid.Empty;
                    this.Plugin.Config.Save();
                }

                foreach (var node in nodes) {
                    foreach (var (child, depth) in node.TraverseWithDepth()) {
                        var selectedParent = child.Id == this._selectedEditLayout;
                        var disabled = selectedParent || ourChildren.Contains(child.Id);
                        var flags = disabled ? ImGuiSelectableFlags.Disabled : ImGuiSelectableFlags.None;

                        var indent = new string(' ', (int) depth * 4);
                        if (!ImGui.Selectable($"{indent}{child.Value.Name}##parent-{child.Id}", selectedParent, flags)) {
                            continue;
                        }

                        layout.Parent = child.Id;
                        this.Plugin.Config.Save();
                    }
                }

                ImGui.EndCombo();
            }

            ImGui.Separator();

            if (ImGui.CollapsingHeader("Options##uimanager-options")) {
                ImGui.DragFloat("Slider speed", ref this._dragSpeed, 0.01f, 0.01f, 10f);

                if (ImGui.BeginCombo("Positioning mode", this.Plugin.Config.PositioningMode.ToString())) {
                    foreach (var mode in (PositioningMode[]) Enum.GetValues(typeof(PositioningMode))) {
                        if (!ImGui.Selectable($"{mode}##positioning", this.Plugin.Config.PositioningMode == mode)) {
                            continue;
                        }

                        this.Plugin.Config.PositioningMode = mode;
                        this.Plugin.Config.Save();
                    }

                    ImGui.EndCombo();
                }
            }

            ImGui.Separator();

            if (ImGui.BeginTabBar("uimanager-positioning")) {
                if (ImGui.BeginTabItem("HUD Elements")) {
                    this.DrawHudElementsTab(layout, ref update);

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Windows")) {
                    this.DrawWindowsTab(layout);

                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }

            EndTabItem:
            ImGui.EndTabItem();
        }

        private void DrawHudElementsTab(SavedLayout layout, ref bool update) {
            if (IconButton(FontAwesomeIcon.Plus, "uimanager-add-hud-element")) {
                ImGui.OpenPopup(Popups.AddElement);
            }

            HoverTooltip("Add a new HUD element to this layout");

            if (ImGui.BeginPopup(Popups.AddElement)) {
                var kinds = ElementKindExt.All()
                    .OrderBy(el => el.LocalisedName(this.Plugin.Interface.Data));
                foreach (var kind in kinds) {
                    if (!ImGui.Selectable($"{kind.LocalisedName(this.Plugin.Interface.Data)}##{kind}")) {
                        continue;
                    }

                    var currentLayout = this.Plugin.Hud.ReadLayout(this.Plugin.Hud.GetActiveHudSlot());
                    var element = currentLayout.elements.FirstOrDefault(el => el.id == kind);
                    this.Plugin.Config.Layouts[this._selectedEditLayout].Elements[kind] = new Element(element);

                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }

            var search = this._editorSearch ?? string.Empty;
            if (ImGui.InputText("Search##ui-editor-search", ref search, 100)) {
                this._editorSearch = string.IsNullOrWhiteSpace(search) ? null : search;
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
                if (this._editorSearch != null && !name.ContainsIgnoreCase(this._editorSearch)) {
                    continue;
                }

                if (!ImGui.CollapsingHeader($"{name}##{kind}-{this._selectedEditLayout}")) {
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
                if (IconButton(FontAwesomeIcon.Search, $"uimanager-preview-element-{kind}")) {
                    if (this.Previews.Contains(kind)) {
                        this.Previews.Remove(kind);
                    } else {
                        this.Previews.Add(kind);
                    }
                }

                HoverTooltip("Toggle a movable preview for this element");

                ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X * 3);
                if (IconButton(FontAwesomeIcon.TrashAlt, $"uimanager-remove-element-{kind}")) {
                    toRemove.Add(kind);
                }

                HoverTooltip("Remove this element from this layout");

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
                    if (IconCheckbox(FontAwesomeIcon.Keyboard, ref keyboard, $"{kind}")) {
                        element[VisibilityFlags.Keyboard] = keyboard;
                        update = true;
                    }

                    ImGui.SameLine();
                    var gamepad = element[VisibilityFlags.Gamepad];
                    if (IconCheckbox(FontAwesomeIcon.Gamepad, ref gamepad, $"{kind}")) {
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
                    if (ImGui.DragFloat($"##x-{kind}", ref x, this._dragSpeed)) {
                        element.X = x;
                        update = true;
                        if (this.Previews.Contains(kind)) {
                            this.UpdatePreviews.Add(kind);
                        }
                    }

                    ImGui.PopItemWidth();

                    DrawEnabledCheckbox(element.Id, ElementComponent.Y, ref update);
                    DrawSettingName("Y");

                    ImGui.PushItemWidth(-1);
                    var y = element.Y;
                    if (ImGui.DragFloat($"##y-{kind}", ref y, this._dragSpeed)) {
                        element.Y = y;
                        update = true;
                        if (this.Previews.Contains(kind)) {
                            this.UpdatePreviews.Add(kind);
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
                        if (this.Previews.Contains(kind)) {
                            this.UpdatePreviews.Add(kind);
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
                        if (this.Previews.Contains(kind)) {
                            this.UpdatePreviews.Add(kind);
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
                this.Plugin.Hud.WriteEffectiveLayout(this.Plugin.Config.StagingSlot, this._selectedEditLayout);
                this.Plugin.Hud.SelectSlot(this.Plugin.Config.StagingSlot, true);
            }

            ImGui.EndChild();
        }

        private void DrawWindowsTab(SavedLayout layout) {
            if (IconButton(FontAwesomeIcon.Plus, "uimanager-add-window")) {
                ImGui.OpenPopup(Popups.AddWindow);
            }

            if (ImGui.BeginPopup(Popups.AddWindow)) {
                ImGui.TextUnformatted("Windows must be open to add them");
                ImGui.Separator();

                foreach (var window in WindowKindExt.All) {
                    var addon = this.Plugin.Interface.Framework.Gui.GetAddonByName(window, 1);
                    var flags = addon?.Visible == true ? ImGuiSelectableFlags.None : ImGuiSelectableFlags.Disabled;

                    if (!ImGui.Selectable(window, false, flags)) {
                        continue;
                    }

                    var pos = this.Plugin.GameFunctions.GetAddonPosition(window);
                    if (pos != null && !layout.Windows.ContainsKey(window)) {
                        layout.Windows.Add(window, new Window(pos));
                    }
                }

                ImGui.EndPopup();
            }

            if (!ImGui.BeginChild("uimanager-layout-editor-windows", new Vector2(0, 0))) {
                return;
            }

            foreach (var entry in layout.Windows) {
                if (!ImGui.CollapsingHeader($"{entry.Key}##uimanager-window-{entry.Key}")) {
                    continue;
                }

                var pos = entry.Value.Position;

                var x = (int) pos.X;
                if (ImGui.InputInt($"X##uimanager-window-{entry.Key}", ref x)) {
                    pos.X = (short) x;
                    this.Plugin.GameFunctions.SetAddonPosition(entry.Key, pos.X, pos.Y);
                }

                var y = (int) pos.Y;
                if (ImGui.InputInt($"Y##uimanager-window-{entry.Key}", ref y)) {
                    pos.Y = (short) y;
                    this.Plugin.GameFunctions.SetAddonPosition(entry.Key, pos.X, pos.Y);
                }
            }

            ImGui.EndChild();
        }

        private void SetUpImportLayoutPopup() {
            if (!ImGui.BeginPopup(Popups.ImportLayout)) {
                return;
            }

            var importName = this._importName ?? "";
            if (ImGui.InputText("Imported layout name", ref importName, 100)) {
                this._importName = string.IsNullOrWhiteSpace(importName) ? null : importName;
            }

            var exists = this.Plugin.Config.Layouts.Values.Any(layout => layout.Name == this._importName);
            if (exists) {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, .8f, .2f, 1f));
                ImGui.TextUnformatted("This will overwrite an existing layout.");
                ImGui.PopStyleColor();
            }

            var current = this.Plugin.Hud.GetActiveHudSlot();
            foreach (var slot in (HudSlot[]) Enum.GetValues(typeof(HudSlot))) {
                var name = current == slot ? $"({(int) slot + 1})" : $"{(int) slot + 1}";
                if (ImGui.Button($"{name}##import-{slot}") && this._importName != null) {
                    Guid id;
                    string newName;
                    Dictionary<string, Window> windows;
                    if (exists) {
                        var overwriting = this.Plugin.Config.Layouts.First(entry => entry.Value.Name == this._importName);
                        id = overwriting.Key;
                        newName = overwriting.Value.Name;
                        windows = overwriting.Value.Windows;
                    } else {
                        id = Guid.NewGuid();
                        newName = this._importName;
                        windows = new Dictionary<string, Window>();
                    }

                    var currentLayout = this.Plugin.Hud.ReadLayout(slot);
                    var newLayout = new SavedLayout(newName, currentLayout, windows);
                    this.Plugin.Config.Layouts[id] = newLayout;
                    this.Plugin.Config.Save();

                    this._selectedEditLayout = id;

                    ImGui.CloseCurrentPopup();
                }

                ImGui.SameLine();
            }

            if (IconButton(FontAwesomeIcon.Clipboard, "import-clipboard") && this._importName != null) {
                SavedLayout? saved;
                try {
                    saved = JsonConvert.DeserializeObject<SavedLayout>(ImGui.GetClipboardText());
                } catch (Exception) {
                    saved = null;
                }

                if (saved != null) {
                    saved.Name = this._importName;

                    var id = Guid.NewGuid();
                    this.Plugin.Config.Layouts[id] = saved;
                    this.Plugin.Config.Save();

                    this._selectedEditLayout = id;

                    ImGui.CloseCurrentPopup();
                }
            }

            ImGui.EndPopup();
        }

        private void SetUpExportLayoutPopup() {
            if (!ImGui.BeginPopup(Popups.ExportLayout)) {
                return;
            }

            if (!this.Plugin.Config.Layouts.TryGetValue(this._selectedEditLayout, out var layout)) {
                return;
            }

            var current = this.Plugin.Hud.GetActiveHudSlot();
            foreach (var slot in (HudSlot[]) Enum.GetValues(typeof(HudSlot))) {
                var name = current == slot ? $"({(int) slot + 1})" : $"{(int) slot + 1}";
                if (ImGui.Button($"{name}##export-{slot}")) {
                    this.Plugin.Hud.WriteEffectiveLayout(slot, this._selectedEditLayout);

                    ImGui.CloseCurrentPopup();
                }

                ImGui.SameLine();
            }

            if (IconButton(FontAwesomeIcon.Clipboard, "export-clipboard")) {
                var json = JsonConvert.SerializeObject(layout);
                ImGui.SetClipboardText(json);
            }

            ImGui.EndPopup();
        }

        private void SetUpRenameLayoutPopup() {
            if (!ImGui.BeginPopup(Popups.RenameLayout)) {
                return;
            }

            var name = this._renameLayout ?? "<none>";
            if (ImGui.InputText("Name", ref name, 100)) {
                this._renameLayout = string.IsNullOrWhiteSpace(name) ? null : name;
            }

            if (ImGui.Button("Rename") && this._renameLayout != null) {
                this.Plugin.Config.Layouts[this._selectedEditLayout].Name = this._renameLayout;
                this.Plugin.Config.Save();

                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        private void SetUpDeleteVerifyPopup(IEnumerable<Node<SavedLayout>> nodes) {
            if (!ImGui.BeginPopupModal(Popups.DeleteVerify)) {
                return;
            }

            if (this.Plugin.Config.Layouts.TryGetValue(this._selectedEditLayout, out var deleting)) {
                ImGui.TextUnformatted($"Are you sure you want to delete the layout \"{deleting.Name}\"?");

                if (ImGui.Button("Yes")) {
                    // unset the parent of any child layouts
                    var node = nodes.Find(this._selectedEditLayout);
                    if (node != null) {
                        foreach (var child in node.Children) {
                            child.Parent = null;
                            child.Value.Parent = Guid.Empty;
                        }
                    }

                    this.Plugin.Config.HudConditionMatches.RemoveAll(match => match.LayoutId == this._selectedEditLayout);

                    this.Plugin.Config.Layouts.Remove(this._selectedEditLayout);
                    this._selectedEditLayout = Guid.Empty;
                    this.Plugin.Config.Save();

                    ImGui.CloseCurrentPopup();
                }

                ImGui.SameLine();
                if (ImGui.Button("No")) {
                    ImGui.CloseCurrentPopup();
                }
            }

            ImGui.EndPopup();
        }

        private void SetUpAddLayoutPopup() {
            if (!ImGui.BeginPopup(Popups.AddLayout)) {
                return;
            }

            var name = this._newLayoutName ?? string.Empty;
            if (ImGui.InputText("Name", ref name, 100)) {
                this._newLayoutName = string.IsNullOrWhiteSpace(name) ? null : name;
            }

            var exists = this.Plugin.Config.Layouts.Values.Any(layout => layout.Name == this._newLayoutName);
            if (exists) {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0f, 0f, 1f));
                ImGui.TextUnformatted("A layout with that name already exists.");
                ImGui.PopStyleColor();
            }

            if (!exists && ImGui.Button("Add") && this._newLayoutName != null) {
                // create the layout
                var saved = new SavedLayout(this._newLayoutName, new Dictionary<ElementKind, Element>(), new Dictionary<string, Window>(), Guid.Empty);
                // reset the new layout name
                this._newLayoutName = null;

                // generate a new id
                var id = Guid.NewGuid();

                // add the layout and save the config
                this.Plugin.Config.Layouts[id] = saved;
                this.Plugin.Config.Save();

                // switch the editor to the new layout
                this._selectedEditLayout = id;

                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        private void DrawHelp() {
            if (!ImGui.BeginTabItem("Help")) {
                return;
            }

            ImGui.PushTextWrapPos();

            foreach (var entry in this.Plugin.Help.Help) {
                if (ImGui.CollapsingHeader(entry.Name)) {
                    ImGui.TextUnformatted(entry.Description.Replace("\n", "\n\n"));
                }
            }

            ImGui.PopTextWrapPos();

            ImGui.EndTabItem();
        }

        #if DEBUG
        private Layout PreviousLayout { get; set; }

        private void DrawDebug() {
            if (!ImGui.BeginTabItem("Debug")) {
                return;
            }

            ImGui.TextUnformatted("Print layout pointer address");

            if (ImGui.Button("1")) {
                var ptr = this.Plugin.Hud.GetLayoutPointer(HudSlot.One);
                this.Plugin.Interface.Framework.Gui.Chat.Print($"{ptr.ToInt64():x}");
            }

            ImGui.SameLine();

            if (ImGui.Button("2")) {
                var ptr = this.Plugin.Hud.GetLayoutPointer(HudSlot.Two);
                this.Plugin.Interface.Framework.Gui.Chat.Print($"{ptr.ToInt64():x}");
            }

            ImGui.SameLine();

            if (ImGui.Button("3")) {
                var ptr = this.Plugin.Hud.GetLayoutPointer(HudSlot.Three);
                this.Plugin.Interface.Framework.Gui.Chat.Print($"{ptr.ToInt64():x}");
            }

            ImGui.SameLine();

            if (ImGui.Button("4")) {
                var ptr = this.Plugin.Hud.GetLayoutPointer(HudSlot.Four);
                this.Plugin.Interface.Framework.Gui.Chat.Print($"{ptr.ToInt64():x}");
            }

            ImGui.SameLine();

            if (ImGui.Button("Default")) {
                var ptr = this.Plugin.Hud.GetDefaultLayoutPointer();
                this.Plugin.Interface.Framework.Gui.Chat.Print($"{ptr.ToInt64():x}");
            }

            if (ImGui.Button("File pointer 0")) {
                var ptr = this.Plugin.Hud.GetFilePointer(0);
                this.Plugin.Interface.Framework.Gui.Chat.Print($"{ptr.ToInt64():x}");
            }

            if (ImGui.Button("Save layout")) {
                var ptr = this.Plugin.Hud.GetLayoutPointer(HudSlot.One);
                var layout = Marshal.PtrToStructure<Layout>(ptr);
                this.PreviousLayout = layout;
            }

            ImGui.SameLine();

            if (ImGui.Button("Find difference")) {
                var ptr = this.Plugin.Hud.GetLayoutPointer(HudSlot.One);
                var layout = Marshal.PtrToStructure<Layout>(ptr);

                foreach (var prevElem in this.PreviousLayout.elements) {
                    var currElem = layout.elements.FirstOrDefault(el => el.id == prevElem.id);
                    if (currElem.visibility == prevElem.visibility && !(Math.Abs(currElem.x - prevElem.x) > .01)) {
                        continue;
                    }

                    PluginLog.Log(currElem.id.ToString());
                    this.Plugin.Interface.Framework.Gui.Chat.Print(currElem.id.ToString());
                }
            }

            if (ImGui.Button("Print current slot")) {
                var slot = this.Plugin.Hud.GetActiveHudSlot();
                this.Plugin.Interface.Framework.Gui.Chat.Print($"{slot}");
            }

            ImGui.Separator();

            // var layoutPtr = this.Plugin.Hud.GetDefaultLayoutPointer() + 8;
            //
            // for (var i = 0; i < 291; i++) {
            //     var rawElement = Marshal.PtrToStructure<RawElement>(layoutPtr + i * Marshal.SizeOf<RawElement>());
            //     var element = new Element(rawElement);
            //
            //     if ((WindowKind) rawElement.id != WindowKind.FreeCompany) {
            //         continue;
            //     }
            //
            //     ImGui.TextUnformatted($"{(WindowKind) rawElement.id}");
            //     ImGui.TextUnformatted($"Measured from: {rawElement.measuredFrom.Name()}");
            //     ImGui.TextUnformatted($"Width: {rawElement.width}");
            //     ImGui.TextUnformatted($"Height: {rawElement.height}");
            //
            //     var screen = ImGui.GetIO().DisplaySize;
            //     var (pos, _) = CalcPosAndSize(element);
            //
            //     var x = pos.X;
            //     if (ImGui.DragFloat($"X##addon-{rawElement.id}", ref x, this._dragSpeed)) {
            //         this.Plugin.GameFunctions.SetAddonPosition("FreeCompany", (short) x, (short) pos.Y);
            //     }
            //
            //     var y = pos.Y;
            //     if (ImGui.DragFloat($"Y##addon-{rawElement.id}", ref y, this._dragSpeed)) {
            //         this.Plugin.GameFunctions.SetAddonPosition("FreeCompany", (short) pos.X, (short) y);
            //     }
            //
            //     ImGui.TextUnformatted($"X: {rawElement.x}/{(short) Math.Round(rawElement.x * screen.X / 100)}");
            //     ImGui.TextUnformatted($"Y: {rawElement.y}/{(short) Math.Round(rawElement.y * screen.Y / 100)}");
            //
            //     var opacity = (int) rawElement.opacity;
            //     if (ImGui.InputInt($"Opacity##addon-{rawElement.id}", ref opacity)) {
            //         rawElement.opacity = (byte) Math.Max(0, Math.Min(255, opacity));
            //         Marshal.StructureToPtr(rawElement, layoutPtr + i * Marshal.SizeOf<RawElement>(), false);
            //         this.Plugin.GameFunctions.SetAddonAlpha("FreeCompany", rawElement.opacity);
            //     }
            //
            //     if (ImGui.Button("Print addon address")) {
            //         var ptr = this.Plugin.Interface.Framework.Gui.GetAddonByName("FreeCompany", 1).Address;
            //         this.Plugin.Interface.Framework.Gui.Chat.Print($"{ptr.ToInt64():x}");
            //     }
            //
            //     if (ImGui.Button("Print base UI object address")) {
            //         var ptr = this.Plugin.Interface.Framework.Gui.GetBaseUIObject();
            //         this.Plugin.Interface.Framework.Gui.Chat.Print($"{ptr.ToInt64():x}");
            //     }
            //
            //     ImGui.Separator();
            // }

            ImGui.EndTabItem();
        }
        #endif

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
                        if (IconButton(FontAwesomeIcon.Check, "condition-edit")) {
                            addCondition = true;
                        }

                        ImGui.SameLine();
                    }

                    if (IconButton(FontAwesomeIcon.Times, "condition-stop")) {
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

                    if (IconButton(FontAwesomeIcon.PencilAlt, $"{item.i}")) {
                        this._editingConditionIndex = item.i;
                        this._editingCondition = item.cond;
                    }

                    ImGui.SameLine();
                    if (IconButton(FontAwesomeIcon.Trash, $"{item.i}")) {
                        actionedItemIndex = item.i;
                    }

                    ImGui.SameLine();
                    if (IconButton(FontAwesomeIcon.ArrowUp, $"{item.i}")) {
                        actionedItemIndex = item.i;
                        action = -1;
                    }

                    ImGui.SameLine();
                    if (IconButton(FontAwesomeIcon.ArrowDown, $"{item.i}")) {
                        actionedItemIndex = item.i;
                        action = 1;
                    }
                }

                ImGui.NextColumn();
            }

            ImGui.Columns(1);

            ImGui.Separator();

            ImGui.EndChild();

            if (IconButton(FontAwesomeIcon.Plus, "condition")) {
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

        private static bool IconButton(FontAwesomeIcon icon, string? id = null) {
            ImGui.PushFont(UiBuilder.IconFont);

            var text = icon.ToIconString();
            if (id != null) {
                text += $"##{id}";
            }

            var result = ImGui.Button(text);

            ImGui.PopFont();

            return result;
        }

        public static bool IconCheckbox(FontAwesomeIcon icon, ref bool value, string? id = null) {
            ImGui.PushFont(UiBuilder.IconFont);

            var text = icon.ToIconString();
            if (id != null) {
                text += $"##{id}";
            }

            var result = ImGui.Checkbox(text, ref value);

            ImGui.PopFont();

            return result;
        }

        private static void HoverTooltip(string text) {
            if (!ImGui.IsItemHovered()) {
                return;
            }

            ImGui.BeginTooltip();
            ImGui.TextUnformatted(text);
            ImGui.EndTooltip();
        }

        private static void HelpMarker(string text) {
            ImGui.TextDisabled("(?)");
            if (!ImGui.IsItemHovered()) {
                return;
            }

            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 20f);
            ImGui.TextUnformatted(text);
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }

        public void Draw() {
            this.DrawSettings();
        }

        public void ImportSlot(string name, HudSlot slot, bool save = true) {
            this.Import(name, this.Plugin.Hud.ReadLayout(slot), save);
        }

        private void Import(string name, Layout layout, bool save = true) {
            var guid = this.Plugin.Config.Layouts.FirstOrDefault(kv => kv.Value.Name == name).Key;
            guid = guid != default ? guid : Guid.NewGuid();

            this.Plugin.Config.Layouts[guid] = new SavedLayout(name, layout);
            if (save) {
                this.Plugin.Config.Save();
            }
        }
    }


    public static class Popups {
        public const string AddLayout = "uimanager-add-layout-popup";
        public const string RenameLayout = "uimanager-rename-layout-popup";
        public const string ImportLayout = "uimanager-import-layout-popup";
        public const string ExportLayout = "uimanager-export-layout-popup";
        public const string AddElement = "uimanager-add-element-popup";
        public const string AddWindow = "uimanager-add-window-popup";
        public const string DeleteVerify = "Delete layout?##uimanager-delete-layout-modal";
    }
}
