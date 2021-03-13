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
        private static readonly string[] SavedWindows = {
            "AreaMap",
            "ChatLog",
            "ChatLogPanel_0",
            "ChatLogPanel_1",
            "ChatLogPanel_2",
            "ChatLogPanel_3",
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

        public PluginUi(Plugin plugin) {
            this.Plugin = plugin;
        }

        public void ConfigUi(object sender, EventArgs args) {
            this.SettingsVisible = true;
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

            if (ImGui.BeginCombo("Parent", parentName)) {
                if (ImGui.Selectable("<none>")) {
                    layout.Parent = Guid.Empty;
                    this.Plugin.Config.Save();
                }

                foreach (var node in nodes) {
                    foreach (var (child, depth) in node.TraverseWithDepth()) {
                        var selectedParent = child.Id == layout.Parent;
                        var flags = child.Id == this._selectedEditLayout ? ImGuiSelectableFlags.Disabled : ImGuiSelectableFlags.None;

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

            ImGui.TextUnformatted("Search");

            ImGui.PushItemWidth(-1);
            var search = this._editorSearch ?? string.Empty;
            if (ImGui.InputText("##ui-editor-search", ref search, 100)) {
                this._editorSearch = string.IsNullOrWhiteSpace(search) ? null : search;
            }

            ImGui.PopItemWidth();

            ImGui.DragFloat("Slider speed", ref this._dragSpeed, 0.01f, 0.01f, 10f);

            ImGui.Separator();

            ImGui.TextUnformatted("HUD Elements");

            ImGui.SameLine();
            if (IconButton(FontAwesomeIcon.Plus, "uimanager-add-hud-element")) {
                ImGui.OpenPopup(Popups.AddElement);
            }

            HoverTooltip("Add a new HUD element to this layout");

            if (ImGui.BeginPopup(Popups.AddElement)) {
                var kinds = Enum.GetValues(typeof(ElementKind))
                    .Cast<ElementKind>()
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

            if (ImGui.BeginChild("uimanager-layout-editor-elements", new Vector2(0, 0))) {
                var toRemove = new List<ElementKind>();

                var sortedElements = layout.Elements
                    .Select(entry => Tuple.Create(entry.Key, entry.Value, entry.Key.LocalisedName(this.Plugin.Interface.Data)))
                    .OrderBy(tuple => tuple.Item3);
                foreach (var (kind, element, name) in sortedElements) {
                    if (this._editorSearch != null && !name.ContainsIgnoreCase(this._editorSearch)) {
                        continue;
                    }

                    if (!ImGui.CollapsingHeader(name)) {
                        continue;
                    }

                    var drawVisibility = !kind.IsJobGauge();

                    void DrawDelete() {
                        ImGui.SameLine(ImGui.GetContentRegionAvail().X - 30);
                        if (IconButton(FontAwesomeIcon.TrashAlt, $"uimanager-remove-element-{kind}")) {
                            toRemove.Add(kind);
                        }
                    }

                    if (drawVisibility) {
                        ImGui.TextUnformatted("Visibility");

                        DrawDelete();

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

                    var x = element.X;
                    if (ImGui.DragFloat($"X##{kind}", ref x, this._dragSpeed)) {
                        element.X = x;
                        update = true;
                    }

                    if (!drawVisibility) {
                        DrawDelete();
                    }

                    var y = element.Y;
                    if (ImGui.DragFloat($"Y##{kind}", ref y, this._dragSpeed)) {
                        element.Y = y;
                        update = true;
                    }

                    var currentScale = $"{element.Scale * 100}%";
                    if (ImGui.BeginCombo($"Scale##{kind}", currentScale)) {
                        foreach (var scale in ScaleOptions) {
                            if (!ImGui.Selectable($"{scale * 100}%")) {
                                continue;
                            }

                            element.Scale = scale;
                            update = true;
                        }

                        ImGui.EndCombo();
                    }

                    if (!kind.IsJobGauge()) {
                        var opacity = (int) element.Opacity;
                        if (ImGui.DragInt($"Opacity##{kind}", ref opacity, 1, 1, 255)) {
                            element.Opacity = (byte) opacity;
                            update = true;
                        }
                    }

                    if (kind == ElementKind.TargetBar) {
                        var targetBarOpts = new TargetBarOptions(element.Options);

                        var independent = targetBarOpts.ShowIndependently;
                        if (ImGui.Checkbox("Display target information independently", ref independent)) {
                            targetBarOpts.ShowIndependently = independent;
                            update = true;
                        }
                    }

                    if (kind == ElementKind.StatusEffects) {
                        var statusOpts = new StatusOptions(element.Options);

                        if (ImGui.BeginCombo($"Style##{kind}", statusOpts.Style.ToString())) {
                            foreach (var style in (StatusStyle[]) Enum.GetValues(typeof(StatusStyle))) {
                                if (!ImGui.Selectable($"{style}##{kind}")) {
                                    continue;
                                }

                                statusOpts.Style = style;
                                update = true;
                            }

                            ImGui.EndCombo();
                        }
                    }

                    if (kind == ElementKind.StatusInfoEnhancements || kind == ElementKind.StatusInfoEnfeeblements || kind == ElementKind.StatusInfoOther) {
                        var statusOpts = new StatusInfoOptions(kind, element.Options);

                        if (ImGui.BeginCombo($"Layout##{kind}", statusOpts.Layout.ToString())) {
                            foreach (var sLayout in (StatusLayout[]) Enum.GetValues(typeof(StatusLayout))) {
                                if (!ImGui.Selectable($"{sLayout}##{kind}")) {
                                    continue;
                                }

                                statusOpts.Layout = sLayout;
                                update = true;
                            }

                            ImGui.EndCombo();
                        }

                        if (ImGui.BeginCombo($"Alignment##{kind}", statusOpts.Alignment.ToString())) {
                            foreach (var alignment in (StatusAlignment[]) Enum.GetValues(typeof(StatusAlignment))) {
                                if (!ImGui.Selectable($"{alignment}##{kind}")) {
                                    continue;
                                }

                                statusOpts.Alignment = alignment;
                                update = true;
                            }

                            ImGui.EndCombo();
                        }

                        var focusable = statusOpts.Gamepad == StatusGamepad.Focusable;
                        if (ImGui.Checkbox($"Focusable by gamepad##{kind}", ref focusable)) {
                            statusOpts.Gamepad = focusable ? StatusGamepad.Focusable : StatusGamepad.NonFocusable;
                            update = true;
                        }
                    }

                    if (kind.IsHotbar()) {
                        var hotbarOpts = new HotbarOptions(element.Options);

                        if (kind != ElementKind.PetHotbar) {
                            var hotbarIndex = (int) hotbarOpts.Index;
                            if (ImGui.InputInt($"Hotbar number##{kind}", ref hotbarIndex)) {
                                hotbarOpts.Index = (byte) Math.Max(0, Math.Min(9, hotbarIndex));
                                update = true;
                            }
                        }

                        if (ImGui.BeginCombo($"Hotbar layout##{kind}", hotbarOpts.Layout.ToString())) {
                            foreach (var hotbarLayout in (HotbarLayout[]) Enum.GetValues(typeof(HotbarLayout))) {
                                if (!ImGui.Selectable($"{hotbarLayout}##{kind}")) {
                                    continue;
                                }

                                hotbarOpts.Layout = hotbarLayout;
                                update = true;
                            }

                            ImGui.EndCombo();
                        }
                    }

                    if (kind.IsJobGauge()) {
                        var gaugeOpts = new GaugeOptions(element.Options);

                        var simple = gaugeOpts.Style == GaugeStyle.Simple;
                        if (ImGui.Checkbox($"Simple##{kind}", ref simple)) {
                            gaugeOpts.Style = simple ? GaugeStyle.Simple : GaugeStyle.Normal;
                            update = true;
                        }
                    }
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

            EndTabItem:
            ImGui.EndTabItem();
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
                    Dictionary<string, Vector2<short>> positions;
                    if (exists) {
                        var overwriting = this.Plugin.Config.Layouts.First(entry => entry.Value.Name == this._importName);
                        id = overwriting.Key;
                        newName = overwriting.Value.Name;
                        positions = overwriting.Value.Positions;
                    } else {
                        id = Guid.NewGuid();
                        newName = this._importName;
                        positions = new Dictionary<string, Vector2<short>>();
                    }

                    var currentLayout = this.Plugin.Hud.ReadLayout(slot);
                    var newLayout = new SavedLayout(newName, currentLayout, positions);
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
                var saved = new SavedLayout(this._newLayoutName, new Dictionary<ElementKind, Element>(), new Dictionary<string, Vector2<short>>(), Guid.Empty);
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

            var current = this.Plugin.Hud.ReadLayout(this.Plugin.Hud.GetActiveHudSlot());
            foreach (var element in current.elements) {
                ImGui.TextUnformatted(element.id.LocalisedName(this.Plugin.Interface.Data));
                ImGui.TextUnformatted($"Width: {element.width}");
                ImGui.TextUnformatted($"Height: {element.height}");
                ImGui.Separator();
            }

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

        private Dictionary<string, Vector2<short>> GetPositions() {
            var positions = new Dictionary<string, Vector2<short>>();

            foreach (var name in SavedWindows) {
                var pos = this.Plugin.GameFunctions.GetWindowPosition(name);
                if (pos != null) {
                    positions[name] = pos;
                }
            }

            return positions;
        }

        public void ImportSlot(string name, HudSlot slot, bool save = true) {
            var positions = this.Plugin.Config.ImportPositions
                ? this.GetPositions()
                : new Dictionary<string, Vector2<short>>();

            this.Import(name, this.Plugin.Hud.ReadLayout(slot), positions, save);
        }

        private void Import(string name, Layout layout, Dictionary<string, Vector2<short>> positions, bool save = true) {
            var guid = this.Plugin.Config.Layouts.FirstOrDefault(kv => kv.Value.Name == name).Key;
            guid = guid != default ? guid : Guid.NewGuid();

            this.Plugin.Config.Layouts[guid] = new SavedLayout(name, layout, positions);
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
        public const string DeleteVerify = "Delete layout?##uimanager-delete-layout-modal";
    }
}
