﻿using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Plugin;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

// TODO: Zone swaps?

namespace HudSwap {
    public class PluginUI {
        private static readonly string[] SAVED_WINDOWS = {
            "AreaMap",
            "ChatLog",
            "ChatLogPanel_0",
            "ChatLogPanel_1",
            "ChatLogPanel_2",
            "ChatLogPanel_3",
        };

        private readonly HudSwapPlugin plugin;
        private readonly DalamudPluginInterface pi;

        private bool _settingsVisible = false;
        public bool SettingsVisible { get => this._settingsVisible; set => this._settingsVisible = value; }

        public PluginUI(HudSwapPlugin plugin, DalamudPluginInterface pi) {
            this.plugin = plugin;
            this.pi = pi;
        }

        public void ConfigUI(object sender, EventArgs args) {
            this.SettingsVisible = true;
        }

        private string importName = "";
        private string renameName = "";
        private Guid selectedLayout = Guid.Empty;

        private string jobFilter = "";

        private int editingConditionIndex = -1;
        private HudConditionMatch editingCondition;

        private static bool configErrorOpen = true;
        public static void ConfigError() {
            if (ImGui.Begin("HudSwap error", ref configErrorOpen)) {
                ImGui.Text("Could not load HudSwap configuration.");
                ImGui.Spacing();
                ImGui.Text("If you are updating from a previous version, please\ndelete your configuration file and restart the game.");

                ImGui.End();
            }
        }

        public void DrawSettings() {
            if (!this.SettingsVisible) {
                return;
            }

            PlayerCharacter player = this.pi.ClientState.LocalPlayer;

            if (ImGui.Begin("HudSwap", ref this._settingsVisible, ImGuiWindowFlags.AlwaysAutoResize)) {
                if (ImGui.BeginTabBar("##hudswap-tabs")) {
                    if (!this.plugin.Config.UnderstandsRisks) {
                        if (ImGui.BeginTabItem("About")) {
                            ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), "Read this first");
                            ImGui.Separator();
                            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 20f);
                            ImGui.Text("HudSwap will use the configured staging slot as its own slot to make changes to. This means the staging slot will be overwritten whenever any swap happens.");
                            ImGui.Spacing();
                            ImGui.Text("Any HUD layout changes you make while HudSwap is enabled may potentially be lost, no matter what slot. If you want to make changes to your HUD layout, TURN OFF HudSwap first.");
                            ImGui.Spacing();
                            ImGui.Text("When editing or making a new layout, to be completely safe, turn off swaps, set up your layout, import the layout into HudSwap, then turn on swaps.");
                            ImGui.Spacing();
                            ImGui.Text("If you are a new user, HudSwap auto-imported your existing layouts on startup.");
                            ImGui.Spacing();
                            ImGui.Text("Finally, HudSwap is beta software. Back up your character data before using this plugin. You may lose some to all of your HUD layouts while testing this plugin.");
                            ImGui.Separator();
                            ImGui.Text("If you have read all of the above and are okay with continuing, check the box below to enable HudSwap. You only need to do this once.");
                            ImGui.PopTextWrapPos();
                            bool understandsRisks = this.plugin.Config.UnderstandsRisks;
                            if (ImGui.Checkbox("I understand", ref understandsRisks)) {
                                this.plugin.Config.UnderstandsRisks = understandsRisks;
                                this.plugin.Config.Save();
                            }

                            ImGui.EndTabItem();
                        }

                        ImGui.EndTabBar();
                        ImGui.End();
                        return;
                    }

                    if (ImGui.BeginTabItem("Layouts")) {
                        ImGui.Text("Saved layouts");
                        if (this.plugin.Config.Layouts2.Count == 0) {
                            ImGui.Text("None saved!");
                        } else {
                            if (ImGui.ListBoxHeader("##saved-layouts")) {
                                foreach (KeyValuePair<Guid, Layout> entry in this.plugin.Config.Layouts2) {
                                    if (ImGui.Selectable($"{entry.Value.Name}##{entry.Key}", this.selectedLayout == entry.Key)) {
                                        this.selectedLayout = entry.Key;
                                        this.renameName = entry.Value.Name;
                                    }
                                }
                                ImGui.ListBoxFooter();
                            }

                            ImGui.Text("Copy onto slot...");
                            foreach (HudSlot slot in Enum.GetValues(typeof(HudSlot))) {
                                string buttonName = $"{(int)slot + 1}##copy";
                                if (ImGui.Button(buttonName) && this.selectedLayout != null) {
                                    Layout layout = this.plugin.Config.Layouts2[this.selectedLayout];
                                    this.plugin.Hud.WriteLayout(slot, layout.Hud.ToArray());
                                }
                                ImGui.SameLine();
                            }

                            if (ImGui.Button("Delete") && this.selectedLayout != null) {
                                this.plugin.Config.Layouts2.Remove(this.selectedLayout);
                                this.plugin.Config.HudConditionMatches.RemoveAll(m => m.LayoutId == this.selectedLayout);
                                this.selectedLayout = Guid.Empty;
                                this.renameName = "";
                                this.plugin.Config.Save();
                            }
                            ImGui.SameLine();

                            if (ImGui.Button("Copy to clipboard") && this.selectedLayout != null) {
                                if (this.plugin.Config.Layouts2.TryGetValue(this.selectedLayout, out Layout layout)) {
                                    SharedLayout shared = new SharedLayout(layout);
                                    string json = JsonConvert.SerializeObject(shared);
                                    ImGui.SetClipboardText(json);
                                }
                            }

                            ImGui.InputText("##rename-input", ref this.renameName, 100);
                            ImGui.SameLine();
                            if (ImGui.Button("Rename") && this.renameName.Length != 0 && this.selectedLayout != null) {
                                Layout layout = this.plugin.Config.Layouts2[this.selectedLayout];
                                Layout newLayout = new Layout(this.renameName, layout.Hud, layout.Positions);
                                this.plugin.Config.Layouts2[this.selectedLayout] = newLayout;
                                this.plugin.Config.Save();
                            }
                        }

                        ImGui.Separator();

                        ImGui.Text("Import");

                        ImGui.InputText("Imported layout name", ref this.importName, 100);

                        bool importPositions = this.plugin.Config.ImportPositions;
                        if (ImGui.Checkbox("Import window positions", ref importPositions)) {
                            this.plugin.Config.ImportPositions = importPositions;
                            this.plugin.Config.Save();
                        }
                        ImGui.SameLine();
                        HelpMarker("If this is checked, the position of the chat box and the map will be saved with the imported layout.");

                        foreach (HudSlot slot in Enum.GetValues(typeof(HudSlot))) {
                            string buttonName = $"{(int)slot + 1}##import";
                            if (ImGui.Button(buttonName) && this.importName.Length != 0) {
                                this.ImportSlot(this.importName, slot);
                                this.importName = "";
                            }
                            ImGui.SameLine();
                        }

                        if (ImGui.Button("Clipboard") && this.importName.Length != 0) {
                            SharedLayout shared = null;
                            try {
                                shared = (SharedLayout)JsonConvert.DeserializeObject(ImGui.GetClipboardText(), typeof(SharedLayout));
#pragma warning disable CA1031 // Do not catch general exception types
                            } catch (Exception) {
#pragma warning restore CA1031 // Do not catch general exception types
                            }
                            if (shared != null) {
                                byte[] layout = shared.Layout();
                                if (layout != null) {
                                    this.Import(this.importName, layout, shared.Positions);
                                    this.importName = "";
                                }
                            }
                        }

                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Swaps")) {
                        bool enabled = this.plugin.Config.SwapsEnabled;
                        if (ImGui.Checkbox("Enable swaps", ref enabled)) {
                            this.plugin.Config.SwapsEnabled = enabled;
                            this.plugin.Config.Save();
                        }
                        ImGui.Text("Note: Disable swaps when editing your HUD.");

                        ImGui.Spacing();
                        string staging = ((int)this.plugin.Config.StagingSlot + 1).ToString();
                        if (ImGui.BeginCombo("Staging slot", staging)) {
                            foreach (HudSlot slot in Enum.GetValues(typeof(HudSlot))) {
                                if (ImGui.Selectable(((int)slot + 1).ToString())) {
                                    this.plugin.Config.StagingSlot = slot;
                                    this.plugin.Config.Save();
                                }
                            }
                            ImGui.EndCombo();
                        }
                        ImGui.SameLine();
                        HelpMarker("The staging slot is the HUD layout slot that will be used as your HUD layout. All changes will be written to this slot when swaps are enabled.");

                        ImGui.Separator();

                        if (this.plugin.Config.Layouts2.Count == 0) {
                            ImGui.Text("Create at least one layout to begin setting up swaps.");
                        } else {
                            ImGui.Text("Add conditions below for when to swap.\nThe topmost condition in the list has priority.");
                            this.DrawConditionsTable();
                        }

                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                }

                ImGui.End();
            }
        }

        private void DrawConditionsTable()
        {
            ImGui.Columns(4);

            var conditions = new List<HudConditionMatch>(plugin.Config.HudConditionMatches);
            if (this.editingConditionIndex == conditions.Count)
                conditions.Add(new HudConditionMatch());

            ImGui.Text("Job");
            ImGui.NextColumn();

            ImGui.Text("State");
            ImGui.NextColumn();

            ImGui.Text("Layout");
            ImGui.NextColumn();

            ImGui.Text("Options");
            ImGui.NextColumn();

            ImGui.Separator();

            bool addCondition = false;
            int actionedItemIndex = -1;
            int action = 0; // 0 for delete, otherwise move.
            foreach (var item in conditions.Select((cond, i) => new { cond, i })) {
                if (this.editingConditionIndex == item.i) {
                    if (ImGui.BeginCombo("##condition-edit-job", this.editingCondition.ClassJob ?? "Any")) {
                        if (ImGui.Selectable("Any##condition-edit-job"))
                            this.editingCondition.ClassJob = null;
                        foreach (ClassJob job in this.pi.Data.GetExcelSheet<ClassJob>().ToList())
                            if (ImGui.Selectable($"{job.Abbreviation}##condition-edit-job"))
                                this.editingCondition.ClassJob = job.Abbreviation;
                        ImGui.EndCombo();
                    }
                    ImGui.NextColumn();

                    if (ImGui.BeginCombo("##condition-edit-status", this.editingCondition.Status?.Name() ?? "Any")) {
                        if (ImGui.Selectable("Any##condition-edit-status"))
                            this.editingCondition.Status = null;
                        foreach (Status status in Enum.GetValues(typeof(Status)))
                            if (ImGui.Selectable($"{status.Name()}##condition-edit-status"))
                                this.editingCondition.Status = status;
                        ImGui.EndCombo();
                    }
                    ImGui.NextColumn();

                    if (ImGui.BeginCombo("##condition-edit-layout", this.editingCondition.LayoutId == Guid.Empty ? string.Empty : this.plugin.Config.Layouts2[this.editingCondition.LayoutId].Name)) {
                        if (ImGui.Selectable("##condition-edit-layout-empty"))
                            this.editingCondition.LayoutId = Guid.Empty;
                        foreach (var layout in this.plugin.Config.Layouts2)
                            if (ImGui.Selectable($"{layout.Value.Name}##condition-edit-layout"))
                                this.editingCondition.LayoutId = layout.Key;
                        ImGui.EndCombo();
                    }
                    ImGui.NextColumn();

                    if (this.editingCondition.LayoutId != Guid.Empty)
                        if (ImGui.Button("Confirm##condition-edit"))
                            addCondition = true;
                } else {
                    ImGui.Text(item.cond.ClassJob ?? string.Empty);
                    ImGui.NextColumn();

                    ImGui.Text(item.cond.Status?.Name() ?? string.Empty);
                    ImGui.NextColumn();

                    ImGui.Text(this.plugin.Config.Layouts2[item.cond.LayoutId].Name);
                    ImGui.NextColumn();

                    if (ImGui.Button($"E##{item.i}")) {
                        this.editingConditionIndex = item.i;
                        this.editingCondition = item.cond;
                    }

                    ImGui.SameLine();
                    if (ImGui.Button($"D##{item.i}"))
                        actionedItemIndex = item.i;
                    ImGui.SameLine();
                    if (ImGui.Button($"↑##{item.i}")) {
                        actionedItemIndex = item.i;
                        action = -1;
                    }

                    ImGui.SameLine();
                    if (ImGui.Button($"↓##{item.i}")) {
                        actionedItemIndex = item.i;
                        action = 1;
                    }
                }

                ImGui.NextColumn();
            }

            ImGui.Columns();

            ImGui.Separator();

            if (ImGui.Button("Add##condition")) {
                this.editingConditionIndex = this.plugin.Config.HudConditionMatches.Count;
                this.editingCondition = new HudConditionMatch();
            }

            if (addCondition) {
                if (this.editingConditionIndex == this.plugin.Config.HudConditionMatches.Count)
                    this.plugin.Config.HudConditionMatches.Add(this.editingCondition);
                else {
                    this.plugin.Config.HudConditionMatches.RemoveAt(this.editingConditionIndex);
                    this.plugin.Config.HudConditionMatches.Insert(this.editingConditionIndex, this.editingCondition);
                }
                this.plugin.Config.Save();
                this.editingConditionIndex = -1;
            }

            if (actionedItemIndex >= 0) {
                if (action == 0)
                    this.plugin.Config.HudConditionMatches.RemoveAt(actionedItemIndex);
                else {
                    if (actionedItemIndex + action >= 0 && actionedItemIndex + action < this.plugin.Config.HudConditionMatches.Count) {
                        // Move the condition.
                        var c = this.plugin.Config.HudConditionMatches[actionedItemIndex];
                        this.plugin.Config.HudConditionMatches.RemoveAt(actionedItemIndex);
                        this.plugin.Config.HudConditionMatches.Insert(actionedItemIndex + action, c);
                    }
                }
            }
        }

        private static void HelpMarker(string text) {
            ImGui.TextDisabled("(?)");
            if (ImGui.IsItemHovered()) {
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(ImGui.GetFontSize() * 20f);
                ImGui.TextUnformatted(text);
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }
        }

        private string LayoutNameOrDefault(Guid key) {
            if (this.plugin.Config.Layouts2.TryGetValue(key, out Layout layout)) {
                return layout.Name;
            } else {
                return "";
            }
        }

        public void Draw() {
            this.DrawSettings();
        }

        private bool LayoutBox(string name, Guid currentLayout, out Guid newLayout) {
            newLayout = Guid.Empty;
            bool updated = false;
            ImGui.Text(name);
            ImGui.NextColumn();
            if (ImGui.BeginCombo($"##{name}-layout", this.LayoutNameOrDefault(currentLayout))) {
                if (ImGui.Selectable("Not set")) {
                    updated = true;
                }
                ImGui.Separator();
                foreach (KeyValuePair<Guid, Layout> entry in this.plugin.Config.Layouts2) {
                    if (ImGui.Selectable(entry.Value.Name)) {
                        updated = true;
                        newLayout = entry.Key;
                    }
                }
                ImGui.EndCombo();
            }
            ImGui.NextColumn();

            return updated;
        }

        private Dictionary<string, Vector2<short>> GetPositions() {
            Dictionary<string, Vector2<short>> positions = new Dictionary<string, Vector2<short>>();

            foreach (string name in SAVED_WINDOWS) {
                Vector2<short> pos = this.plugin.GameFunctions.GetWindowPosition(name);
                if (pos != null) {
                    positions[name] = pos;
                }
            }

            return positions;
        }

        public void ImportSlot(string name, HudSlot slot, bool save = true) {
            Dictionary<string, Vector2<short>> positions;
            if (this.plugin.Config.ImportPositions) {
                positions = this.GetPositions();
            } else {
                positions = new Dictionary<string, Vector2<short>>();
            }
            this.Import(name, this.plugin.Hud.ReadLayout(slot), positions, save);
        }

        public void Import(string name, byte[] layout, Dictionary<string, Vector2<short>> positions, bool save = true) {
            this.plugin.Config.Layouts2[Guid.NewGuid()] = new Layout(name, layout, positions);
            if (save) {
                this.plugin.Config.Save();
            }
        }
    }
}
