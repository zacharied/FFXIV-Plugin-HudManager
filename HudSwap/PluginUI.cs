using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Plugin;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

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
        private Guid selectedLayout = Guid.Empty;

        public void DrawSettings() {
            if (!this.SettingsVisible) {
                return;
            }

            PlayerCharacter player = this.pi.ClientState.LocalPlayer;

            if (ImGui.Begin("HudSwap", ref this._settingsVisible, ImGuiWindowFlags.AlwaysAutoResize)) {
                if (ImGui.BeginTabBar("##hudswap-tabs")) {
                    if (ImGui.BeginTabItem("Layouts")) {
                        ImGui.Text("Saved layouts");
                        if (this.plugin.config.Layouts.Keys.Count == 0) {
                            ImGui.Text("None saved!");
                        } else {
                            if (ImGui.ListBoxHeader("##saved-layouts")) {
                                foreach (KeyValuePair<Guid, Tuple<string, byte[]>> entry in this.plugin.config.Layouts) {
                                    if (ImGui.Selectable(entry.Value.Item1, this.selectedLayout == entry.Key)) {
                                        this.selectedLayout = entry.Key;
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
                                this.plugin.config.Save();
                            }
                        }

                        ImGui.Separator();

                        ImGui.Text("Import");

                        ImGui.InputText("Imported layout name", ref this.importName, 100);

                        foreach (HudSlot slot in Enum.GetValues(typeof(HudSlot))) {
                            string buttonName = $"{(int)slot + 1}##import";
                            if (ImGui.Button(buttonName) && this.importName != "") {
                                this.plugin.config.Layouts[Guid.NewGuid()] = new Tuple<string, byte[]>(this.importName, this.plugin.hud.ReadLayout(slot));
                                this.importName = "";
                                this.plugin.config.Save();
                            }
                            if (slot != HudSlot.Four) {
                                ImGui.SameLine();
                            }
                        }

                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Swaps")) {
                        ImGui.Text("Disable swaps when editing your HUD.");

                        bool enabled = this.plugin.config.SwapsEnabled;
                        if (ImGui.Checkbox("Enabled", ref enabled)) {
                            this.plugin.config.SwapsEnabled = enabled;
                            this.plugin.config.Save();
                        }

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

                        ImGui.Columns(1);

                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                }

                ImGui.End();
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

            if (!this.plugin.config.SwapsEnabled) {
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
                    this.statuses.SetHudLayout(player, true);
                }
                ImGui.Separator();
                foreach (KeyValuePair<Guid, Tuple<string, byte[]>> entry in this.plugin.config.Layouts) {
                    if (ImGui.Selectable(entry.Value.Item1)) {
                        layout = entry.Key;
                        this.plugin.config.Save();
                        this.statuses.SetHudLayout(player, true);
                    }
                }
                ImGui.EndCombo();
            }
            ImGui.NextColumn();
        }
    }

    public class Statuses {
        private HudSwapPlugin plugin;
        private DalamudPluginInterface pi;

        public bool InCombat { get; private set; } = false;
        public bool WeaponDrawn { get; private set; } = false;
        public bool InInstance { get; private set; } = false;
        public bool Crafting { get; private set; } = false;
        public bool Gathering { get; private set; } = false;

        public Statuses(HudSwapPlugin plugin, DalamudPluginInterface pi) {
            this.plugin = plugin;
            this.pi = pi;
        }

        public bool SetInCombat(bool inCombat) {
            bool old = this.InCombat;
            this.InCombat = inCombat;
            return old != this.InCombat;
        }

        public bool SetWeaponDrawn(bool weaponDrawn) {
            bool old = this.WeaponDrawn;
            this.WeaponDrawn = weaponDrawn;
            return old != this.WeaponDrawn;
        }

        public bool SetInInstance(bool inInstance) {
            bool old = this.InInstance;
            this.InInstance = inInstance;
            return old != this.InInstance;
        }

        public bool SetCrafting(bool crafting) {
            bool old = this.Crafting;
            this.Crafting = crafting;
            return old != this.Crafting;
        }

        public bool SetGathering(bool gathering) {
            bool old = this.Gathering;
            this.Gathering = gathering;
            return old != this.Gathering;
        }

        public bool Update(PlayerCharacter player) {
            if (player == null) {
                return false;
            }

            bool anyChanged = false;

            Condition condition = this.pi.ClientState.Condition;

            anyChanged |= this.SetGathering(condition[ConditionFlag.Gathering]) && this.plugin.config.gatheringLayout != Guid.Empty;
            anyChanged |= this.SetCrafting(condition[ConditionFlag.Crafting]) && this.plugin.config.craftingLayout != Guid.Empty;
            anyChanged |= this.SetInInstance(condition[ConditionFlag.BoundByDuty]) && this.plugin.config.instanceLayout != Guid.Empty;
            anyChanged |= this.SetWeaponDrawn(this.IsWeaponDrawn(player)) && this.plugin.config.weaponDrawnLayout != Guid.Empty;
            anyChanged |= this.SetInCombat(condition[ConditionFlag.InCombat]) && this.plugin.config.combatLayout != Guid.Empty;

            return anyChanged;
        }

        public Guid CalculateCurrentHud() {
            PlayerCharacter player = this.pi.ClientState.LocalPlayer;
            if (player == null) {
                return Guid.Empty;
            }
            this.Update(player);

            Guid layout = this.plugin.config.defaultLayout;

            if (this.Gathering && this.plugin.config.gatheringLayout != Guid.Empty) {
                layout = this.plugin.config.gatheringLayout;
            }
            if (this.Crafting && this.plugin.config.craftingLayout != Guid.Empty) {
                layout = this.plugin.config.craftingLayout;
            }
            if (this.InInstance && this.plugin.config.instanceLayout != Guid.Empty) {
                layout = this.plugin.config.instanceLayout;
            }
            if (this.WeaponDrawn && this.plugin.config.weaponDrawnLayout != Guid.Empty) {
                layout = this.plugin.config.weaponDrawnLayout;
            }
            if (this.InCombat && this.plugin.config.combatLayout != Guid.Empty) {
                layout = this.plugin.config.combatLayout;
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
            byte[] layoutBytes = this.plugin.config.Layouts[layout]?.Item2;
            if (layoutBytes == null) {
                return; // FIXME: do something better
            }
            this.plugin.hud.WriteLayout(HudSlot.Four, layoutBytes);
            this.plugin.hud.SelectSlot(HudSlot.Four, true);
        }

        private byte GetStatus(Actor actor) {
            IntPtr statusPtr = this.pi.TargetModuleScanner.ResolveRelativeAddress(actor.Address, 0x1901);
            return Marshal.ReadByte(statusPtr);
        }

        private bool IsWeaponDrawn(Actor actor) {
            return (GetStatus(actor) & 4) > 0;
        }
    }
}
