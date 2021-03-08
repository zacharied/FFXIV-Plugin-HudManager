using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Interface;
using Dalamud.Plugin;
using HUD_Manager.Configuration;
using HUD_Manager.Structs;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;
using Action = System.Action;

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

        private Plugin Plugin { get; }

        private bool _settingsVisible;

        public bool SettingsVisible {
            get => this._settingsVisible;
            set => this._settingsVisible = value;
        }

        private string _importName = "";
        private string _renameName = "";
        private Guid _selectedLayoutId = Guid.Empty;

        private SavedLayout? SelectedSavedLayout => this._selectedLayoutId == Guid.Empty ? null : this.Plugin.Config.Layouts[this._selectedLayoutId];

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
            void DrawHudSlotButtons(string idSuffix, Action<HudSlot> hudSlotAction, Action clipboardAction) {
                var slotButtonSize = new Vector2(40, 0);
                foreach (HudSlot slot in Enum.GetValues(typeof(HudSlot))) {
                    // Surround the button with parentheses if this is the current slot
                    var slotText = slot == this.Plugin.Hud.GetActiveHudSlot() ? $"({(int) slot + 1})" : ((int) slot + 1).ToString();
                    var buttonName = $"{slotText}##${idSuffix}";
                    if (ImGui.Button(buttonName, slotButtonSize)) {
                        PluginLog.Log("Importing outer");
                        hudSlotAction(slot);
                    }

                    ImGui.SameLine();
                }

                ImGui.SameLine();

                if (ImGui.Button($"Clipboard##{idSuffix}")) {
                    clipboardAction();
                }
            }

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
                        ImGui.Text("HUD Manager will use the configured staging slot as its own slot to make changes to. This means the staging slot will be overwritten whenever any swap happens.");
                        ImGui.Spacing();
                        ImGui.Text("Any HUD layout changes you make while HUD Manager is enabled may potentially be lost, no matter what slot. If you want to make changes to your HUD layout, TURN OFF HUD Manager first.");
                        ImGui.Spacing();
                        ImGui.Text("When editing or making a new layout, to be completely safe, turn off swaps, set up your layout, import the layout into HUD Manager, then turn on swaps.");
                        ImGui.Spacing();
                        ImGui.Text("If you are a new user, HUD Manager auto-imported your existing layouts on startup.");
                        ImGui.Spacing();
                        ImGui.Text("Finally, HUD Manager is beta software. Back up your character data before using this plugin. You may lose some to all of your HUD layouts while testing this plugin.");
                        ImGui.Separator();
                        ImGui.Text("If you have read all of the above and are okay with continuing, check the box below to enable HUD Manager. You only need to do this once.");
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

                if (ImGui.BeginTabItem("Layouts")) {
                    ImGui.Text("Saved layouts");
                    if (this.Plugin.Config.Layouts.Count == 0) {
                        ImGui.Text("None saved!");
                    } else {
                        ImGui.PushItemWidth(-1);
                        if (ImGui.ListBoxHeader("##saved-layouts")) {
                            foreach (var entry in this.Plugin.Config.Layouts) {
                                if (!ImGui.Selectable($"{entry.Value.Name}##{entry.Key}", this._selectedLayoutId == entry.Key)) {
                                    continue;
                                }

                                this._selectedLayoutId = entry.Key;
                                this._renameName = entry.Value.Name;
                                this._importName = this._renameName;
                            }

                            ImGui.ListBoxFooter();
                        }

                        ImGui.PopItemWidth();

                        ImGui.PushItemWidth(200);
                        ImGui.InputText("##rename-input", ref this._renameName, 100);
                        ImGui.PopItemWidth();
                        ImGui.SameLine();
                        if (ImGui.Button("Rename") && this._renameName.Length != 0 && this.SelectedSavedLayout != null) {
                            var layout = this.Plugin.Config.Layouts[this._selectedLayoutId];
                            var newLayout = new SavedLayout(this._renameName, layout.ToLayout(), layout.Positions);
                            this.Plugin.Config.Layouts[this._selectedLayoutId] = newLayout;
                            this.Plugin.Config.Save();
                        }

                        const int layoutActionButtonWidth = 30;
                        // `layoutActionButtonWidth` must be multiplied by however many action buttons there are here
                        ImGui.SameLine(ImGui.GetWindowContentRegionWidth() - layoutActionButtonWidth * 1);
                        ImGui.PushFont(UiBuilder.IconFont);
                        if (ImGui.Button(FontAwesomeIcon.Trash.ToIconString(), new Vector2(layoutActionButtonWidth, 0)) &&
                            this.SelectedSavedLayout != null) {
                            this.Plugin.Config.Layouts.Remove(this._selectedLayoutId);
                            this.Plugin.Config.HudConditionMatches.RemoveAll(m => m.LayoutId == this._selectedLayoutId);
                            this._selectedLayoutId = Guid.Empty;
                            this._renameName = "";
                            this.Plugin.Config.Save();
                        }

                        ImGui.PopFont();

                        ImGui.Text("Copy to...");
                        DrawHudSlotButtons("copy", slot => {
                            if (this.SelectedSavedLayout == null) {
                                return;
                            }

                            this.Plugin.Hud.WriteLayout(slot, this.SelectedSavedLayout.ToLayout());
                        }, () => {
                            if (this.SelectedSavedLayout == null) {
                                return;
                            }

                            var json = JsonConvert.SerializeObject(this.SelectedSavedLayout);
                            ImGui.SetClipboardText(json);
                        });
                    }

                    ImGui.Separator();

                    ImGui.Text("Import");

                    ImGui.InputText("Imported layout name", ref this._importName, 100);

                    var importPositions = this.Plugin.Config.ImportPositions;
                    if (ImGui.Checkbox("Import window positions", ref importPositions)) {
                        this.Plugin.Config.ImportPositions = importPositions;
                        this.Plugin.Config.Save();
                    }

                    ImGui.SameLine();
                    HelpMarker("If this is checked, the position of the chat box and the map will be saved with the imported layout.");

                    var isOverwriting = this.Plugin.Config.Layouts.Values.Any(layout => layout.Name == this._importName);
                    ImGui.Text((isOverwriting ? "Overwrite" : "Import") + " from...");

                    DrawHudSlotButtons("import", slot => {
                        PluginLog.Log("Importing inner");
                        this.ImportSlot(this._importName, slot);
                        this._importName = "";
                    }, () => {
                        SavedLayout? shared = null;
                        try {
                            shared = JsonConvert.DeserializeObject<SavedLayout>(ImGui.GetClipboardText());
                        } catch (Exception) {
                            // ignored
                        }

                        if (shared == null) {
                            return;
                        }

                        this.Import(this._importName, shared.ToLayout(), shared.Positions);
                        this._importName = "";
                    });

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Swaps")) {
                    var enabled = this.Plugin.Config.SwapsEnabled;
                    if (ImGui.Checkbox("Enable swaps", ref enabled)) {
                        this.Plugin.Config.SwapsEnabled = enabled;
                        this.Plugin.Config.Save();
                    }

                    ImGui.Text("Note: Disable swaps when editing your HUD.");

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
                        ImGui.Text("Create at least one layout to begin setting up swaps.");
                    } else {
                        ImGui.Text("Add swap conditions below.\nThe first condition that is satisfied will be the layout that is used.");
                        ImGui.Separator();
                        this.DrawConditionsTable();
                    }

                    ImGui.EndTabItem();
                }

                #if DEBUG
                if (ImGui.BeginTabItem("Debug")) {
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

                    if (ImGui.BeginChild("ui-elements", new Vector2(0, 0))) {
                        var ptr = this.Plugin.Hud.GetLayoutPointer(this.Plugin.Hud.GetActiveHudSlot());
                        var layout = Marshal.PtrToStructure<Layout>(ptr);

                        var changed = false;

                        var elements = (ElementKind[]) Enum.GetValues(typeof(ElementKind));
                        foreach (var kind in elements.OrderBy(el => el.ToString())) {
                            for (var i = 0; i < layout.elements.Length; i++) {
                                if (layout.elements[i].id != kind) {
                                    continue;
                                }

                                ImGui.TextUnformatted(kind.ToString());

                                var x = layout.elements[i].x;
                                if (ImGui.DragFloat($"X##{kind}", ref x)) {
                                    layout.elements[i].x = x;
                                    changed = true;
                                }

                                var y = layout.elements[i].y;
                                if (ImGui.DragFloat($"Y##{kind}", ref y)) {
                                    layout.elements[i].y = y;
                                    changed = true;
                                }

                                var visible = layout.elements[i].visibility == Visibility.Visible;
                                if (ImGui.Checkbox($"Visible##{kind}", ref visible)) {
                                    layout.elements[i].visibility = visible ? Visibility.Visible : Visibility.Hidden;
                                    changed = true;
                                }

                                var scale = layout.elements[i].scale;
                                if (ImGui.DragFloat($"Scale##{kind}", ref scale)) {
                                    layout.elements[i].scale = scale;
                                    changed = true;
                                }

                                var opacity = (int) layout.elements[i].opacity;
                                if (ImGui.DragInt($"Opacity##{kind}", ref opacity, 1, 1, 255)) {
                                    layout.elements[i].opacity = (byte) opacity;
                                    changed = true;
                                }

                                ImGui.Separator();

                                break;
                            }
                        }

                        if (changed) {
                            Marshal.StructureToPtr(layout, ptr, false);
                            this.Plugin.Hud.SelectSlot(this.Plugin.Hud.GetActiveHudSlot(), true);
                        }

                        ImGui.EndChild();
                    }

                    ImGui.EndTabItem();
                }
                #endif

                ImGui.EndTabBar();
            }

            ImGui.End();
        }

        private Layout PreviousLayout { get; set; }

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

            ImGui.Text("Job");
            ImGui.NextColumn();

            ImGui.Text("State");
            ImGui.NextColumn();

            ImGui.Text("Layout");
            ImGui.NextColumn();

            ImGui.Text("Options");
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
                    ImGui.Text(item.cond.ClassJob ?? string.Empty);
                    ImGui.NextColumn();

                    ImGui.Text(item.cond.Status?.Name() ?? string.Empty);
                    ImGui.NextColumn();

                    this.Plugin.Config.Layouts.TryGetValue(item.cond.LayoutId, out var condLayout);
                    ImGui.Text(condLayout?.Name ?? string.Empty);
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

        private static bool IconButton(FontAwesomeIcon icon, string append = "") {
            ImGui.PushFont(UiBuilder.IconFont);
            var button = ImGui.Button($"{icon.ToIconString()}##{append}");
            ImGui.PopFont();
            return button;
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
}
