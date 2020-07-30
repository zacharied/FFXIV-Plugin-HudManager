using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Plugin;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

// TODO: Job swaps?
// TODO: Zone swaps?

namespace HudSwap {
    public class PluginUI {
        private readonly HudSwapPlugin plugin;
        private readonly DalamudPluginInterface pi;
        private readonly Statuses statuses;

        private bool _settingsVisible = false;
        public bool SettingsVisible { get => this._settingsVisible; set => this._settingsVisible = value; }

        public PluginUI(HudSwapPlugin plugin, DalamudPluginInterface pi) {
            this.plugin = plugin;
            this.pi = pi;
            this.statuses = new Statuses(this.plugin, this.pi);
        }

        public void ConfigUI(object sender, EventArgs args) {
            this.SettingsVisible = true;
        }

        private string importName = "";
        private string renameName = "";
        private Guid selectedLayout = Guid.Empty;

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
                    if (!this.plugin.config.UnderstandsRisks) {
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
                            bool understandsRisks = this.plugin.config.UnderstandsRisks;
                            if (ImGui.Checkbox("I understand", ref understandsRisks)) {
                                this.plugin.config.UnderstandsRisks = understandsRisks;
                                this.plugin.config.Save();
                            }

                            ImGui.EndTabItem();
                        }

                        ImGui.EndTabBar();
                        ImGui.End();
                        return;
                    }

                    if (ImGui.BeginTabItem("Layouts")) {
                        ImGui.Text("Saved layouts");
                        if (this.plugin.config.Layouts.Keys.Count == 0) {
                            ImGui.Text("None saved!");
                        } else {
                            if (ImGui.ListBoxHeader("##saved-layouts")) {
                                foreach (KeyValuePair<Guid, Tuple<string, byte[]>> entry in this.plugin.config.Layouts) {
                                    if (ImGui.Selectable(entry.Value.Item1, this.selectedLayout == entry.Key)) {
                                        this.selectedLayout = entry.Key;
                                        this.renameName = entry.Value.Item1;
                                    }
                                }
                                ImGui.ListBoxFooter();
                            }

                            ImGui.Text("Copy onto slot...");
                            foreach (HudSlot slot in Enum.GetValues(typeof(HudSlot))) {
                                string buttonName = $"{(int)slot + 1}##copy";
                                if (ImGui.Button(buttonName) && this.selectedLayout != null) {
                                    byte[] layout = this.plugin.config.Layouts[this.selectedLayout].Item2;
                                    this.plugin.hud.WriteLayout(slot, layout);
                                }
                                ImGui.SameLine();
                            }

                            if (ImGui.Button("Delete") && this.selectedLayout != null) {
                                this.plugin.config.Layouts.Remove(this.selectedLayout);
                                this.selectedLayout = Guid.Empty;
                                this.renameName = "";
                                this.plugin.config.Save();
                            }

                            ImGui.InputText("##rename-input", ref this.renameName, 100);
                            ImGui.SameLine();
                            if (ImGui.Button("Rename") && this.renameName != "" && this.selectedLayout != null) {
                                Tuple<string, byte[]> entry = this.plugin.config.Layouts[this.selectedLayout]; ;
                                this.plugin.config.Layouts[this.selectedLayout] = new Tuple<string, byte[]>(this.renameName, entry.Item2);
                                this.plugin.config.Save();
                            }
                        }

                        ImGui.Separator();

                        ImGui.Text("Import");

                        ImGui.InputText("Imported layout name", ref this.importName, 100);

                        foreach (HudSlot slot in Enum.GetValues(typeof(HudSlot))) {
                            string buttonName = $"{(int)slot + 1}##import";
                            if (ImGui.Button(buttonName) && this.importName != "") {
                                this.ImportSlot(slot, this.importName);
                                this.importName = "";
                            }
                            if (slot != HudSlot.Four) {
                                ImGui.SameLine();
                            }
                        }

                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Swaps")) {
                        bool enabled = this.plugin.config.SwapsEnabled;
                        if (ImGui.Checkbox("Enable swaps", ref enabled)) {
                            this.plugin.config.SwapsEnabled = enabled;
                            this.plugin.config.Save();
                        }
                        ImGui.Text("Note: Disable swaps when editing your HUD.");

                        ImGui.Spacing();
                        string staging = ((int)this.plugin.config.StagingSlot + 1).ToString();
                        if (ImGui.BeginCombo("Staging slot", staging)) {
                            foreach (HudSlot slot in Enum.GetValues(typeof(HudSlot))) {
                                if (ImGui.Selectable(((int)slot + 1).ToString())) {
                                    this.plugin.config.StagingSlot = slot;
                                    this.plugin.config.Save();
                                }
                            }
                            ImGui.EndCombo();
                        }
                        ImGui.SameLine();
                        HelpMarker("The staging slot is the HUD layout slot that will be used as your HUD layout. All changes will be written to this slot when swaps are enabled.");

                        ImGui.Separator();

                        ImGui.Text("This is the default layout. If none of the below conditions are\nsatisfied, this layout will be enabled.");

                        if (ImGui.BeginCombo("##default-layout", this.LayoutNameOrDefault(this.plugin.config.defaultLayout))) {
                            foreach (KeyValuePair<Guid, Tuple<string, byte[]>> entry in this.plugin.config.Layouts) {
                                if (ImGui.Selectable(entry.Value.Item1)) {
                                    this.plugin.config.defaultLayout = entry.Key;
                                    this.plugin.config.Save();
                                }
                            }
                            ImGui.EndCombo();
                        }

                        ImGui.Spacing();
                        ImGui.Text("These settings are ordered from highest priority to lowest priority.\nHigher priorities overwrite lower priorities when enabled.");
                        ImGui.Spacing();

                        ImGui.Columns(2);

                        this.LayoutBox("In combat", ref this.plugin.config.combatLayout, player);
                        this.LayoutBox("Weapon drawn", ref this.plugin.config.weaponDrawnLayout, player);
                        this.LayoutBox("In instance", ref this.plugin.config.instanceLayout, player);
                        this.LayoutBox("Crafting", ref this.plugin.config.craftingLayout, player);
                        this.LayoutBox("Gathering", ref this.plugin.config.gatheringLayout, player);
                        this.LayoutBox("Fishing", ref this.plugin.config.fishingLayout, player);

                        ImGui.Columns(1);

                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                }

                ImGui.End();
            }
        }

        private void HelpMarker(string text) {
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
            Tuple<string, byte[]> tuple;
            if (this.plugin.config.Layouts.TryGetValue(key, out tuple)) {
                return tuple.Item1;
            } else {
                return "";
            }
        }

        public void Draw() {
            this.DrawSettings();

            if (!(this.plugin.config.SwapsEnabled && this.plugin.config.UnderstandsRisks)) {
                return;
            }

            PlayerCharacter player = this.pi.ClientState.LocalPlayer;
            if (player == null) {
                return;
            }

            if (this.statuses.Update(player)) {
                this.statuses.SetHudLayout(null);
            }
        }

        private void LayoutBox(string name, ref Guid layout, PlayerCharacter player) {
            ImGui.Text(name);
            ImGui.NextColumn();
            if (ImGui.BeginCombo($"##{name}-layout", this.LayoutNameOrDefault(layout))) {
                if (ImGui.Selectable("Not set")) {
                    layout = Guid.Empty;
                    this.plugin.config.Save();
                    if (this.plugin.config.SwapsEnabled) {
                        this.statuses.SetHudLayout(player, true);
                    }
                }
                ImGui.Separator();
                foreach (KeyValuePair<Guid, Tuple<string, byte[]>> entry in this.plugin.config.Layouts) {
                    if (ImGui.Selectable(entry.Value.Item1)) {
                        layout = entry.Key;
                        this.plugin.config.Save();
                        if (this.plugin.config.SwapsEnabled) {
                            this.statuses.SetHudLayout(player, true);
                        }
                    }
                }
                ImGui.EndCombo();
            }
            ImGui.NextColumn();
        }

        public void ImportSlot(HudSlot slot, string name, bool save = true) {
            this.plugin.config.Layouts[Guid.NewGuid()] = new Tuple<string, byte[]>(name, this.plugin.hud.ReadLayout(slot));
            if (save) {
                this.plugin.config.Save();
            }
        }
    }

    public class Statuses {
        private readonly HudSwapPlugin plugin;
        private readonly DalamudPluginInterface pi;

        private readonly bool[] condition = new bool[ORDER.Length];

        // Order: lowest to highest priority
        // For conditions that require custom logic, use ConditionFlag.None
        private static readonly ConditionFlag[] ORDER = {
            ConditionFlag.Fishing,
            ConditionFlag.Gathering,
            ConditionFlag.Crafting,
            ConditionFlag.BoundByDuty,
            ConditionFlag.None, // weapon drawn
            ConditionFlag.InCombat,
        };

        private delegate bool CustomCondition(HudSwapPlugin plugin, DalamudPluginInterface pi, PlayerCharacter player);

        // Add handlers in the order that ConditionFlag.None flags appear in ORDER.
        private static readonly CustomCondition[] CUSTOM = {
            // weapon drawn
            (plugin, pi, player) => (GetStatus(pi, player) & 4) > 0,
        };

        protected static byte GetStatus(DalamudPluginInterface pi, Actor actor) {
            IntPtr statusPtr = pi.TargetModuleScanner.ResolveRelativeAddress(actor.Address, 0x1901);
            return Marshal.ReadByte(statusPtr);
        }

        public Statuses(HudSwapPlugin plugin, DalamudPluginInterface pi) {
            this.plugin = plugin;
            this.pi = pi;
            if (ORDER.Length != this.GetLayouts().Length) {
                throw new ApplicationException("Statuses.ORDER is not the same length as the array returned by Statuses.GetLayouts()");
            }
            if (ORDER.Where(flag => flag == ConditionFlag.None).Count() != CUSTOM.Length) {
                throw new ApplicationException("Statuses.CUSTOM does not have an amount of handlers equalling the amount of ConditionFlag.None in Statuses.ORDER");
            }
        }

        private Guid[] GetLayouts() {
            // These layouts must be in the same order as the flags in ORDER are defined
            Guid[] layouts = {
                this.plugin.config.fishingLayout,
                this.plugin.config.gatheringLayout,
                this.plugin.config.craftingLayout,
                this.plugin.config.instanceLayout,
                this.plugin.config.weaponDrawnLayout,
                this.plugin.config.combatLayout,
            };
            return layouts;
        }

        public bool Update(PlayerCharacter player) {
            if (player == null) {
                return false;
            }

            int customs = 0;
            bool[] old = (bool[])this.condition.Clone();
            Condition condition = this.pi.ClientState.Condition;

            bool anyChanged = false;

            for (int i = 0; i < ORDER.Length; i++) {
                ConditionFlag flag = ORDER[i];
                if (flag == ConditionFlag.None) {
                    this.condition[i] = CUSTOM[customs].Invoke(this.plugin, this.pi, player);
                    customs += 1;
                } else {
                    this.condition[i] = condition[flag];
                }
                anyChanged |= old[i] != this.condition[i];
            }

            return anyChanged;
        }

        public Guid CalculateCurrentHud() {
            PlayerCharacter player = this.pi.ClientState.LocalPlayer;
            if (player == null) {
                return Guid.Empty;
            }
            this.Update(player);

            Guid layout = this.plugin.config.defaultLayout;
            Guid[] layouts = this.GetLayouts();

            for (int i = 0; i < ORDER.Length; i++) {
                Guid flagLayout = layouts[i];

                if (this.condition[i] && flagLayout != Guid.Empty) {
                    layout = flagLayout;
                }
            }

            return layout;
        }

        public void SetHudLayout(PlayerCharacter player, bool update = false) {
            if (update && player != null) {
                this.Update(player);
            }

            Guid layout = this.CalculateCurrentHud();
            if (layout == Guid.Empty) {
                return; // FIXME: do something better
            }
            if (!this.plugin.config.Layouts.TryGetValue(layout, out Tuple<string, byte[]> entry)) {
                return; // FIXME: do something better
            }
            this.plugin.hud.WriteLayout(this.plugin.config.StagingSlot, entry.Item2);
            this.plugin.hud.SelectSlot(this.plugin.config.StagingSlot, true);
        }
    }
}
