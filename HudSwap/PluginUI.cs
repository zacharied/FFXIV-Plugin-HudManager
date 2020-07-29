using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Plugin;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

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
        public void DrawSettings() {
            if (!this.SettingsVisible) {
                return;
            }

            PlayerCharacter player = this.pi.ClientState.LocalPlayer;

            if (ImGui.Begin("HudSwap", ref this._settingsVisible, ImGuiWindowFlags.AlwaysAutoResize)) {
                ImGui.Text("This is the default layout. If none of the below conditions are\nsatisfied, this layout will be enabled.");
                int defaultLayout = (int)this.plugin.config.DefaultLayout + 1;
                if (ImGui.InputInt("##default-layout", ref defaultLayout)) {
                    defaultLayout = Math.Max(1, Math.Min(4, defaultLayout));
                    this.plugin.config.DefaultLayout = (uint)defaultLayout - 1;
                    this.plugin.config.Save();
                    this.statuses.SetHudLayout(player, true);
                }

                ImGui.Spacing();
                ImGui.Text("These settings are ordered from highest priority to lowest priority.\nHigher priorities overwrite lower priorities when enabled.");

                bool onCombat = this.plugin.config.ChangeOnCombat;
                if (ImGui.Checkbox("Change HUD when combat begins", ref onCombat)) {
                    this.plugin.config.ChangeOnCombat = onCombat;
                    this.plugin.config.Save();
                    this.statuses.SetHudLayout(player, true);
                    this.statuses.SetHudLayout(player, true);
                }
                int combatLayout = (int)this.plugin.config.CombatLayout + 1;
                if (ImGui.InputInt("##combat-layout", ref combatLayout)) {
                    combatLayout = Math.Max(1, Math.Min(4, combatLayout));
                    this.plugin.config.CombatLayout = (uint)combatLayout - 1;
                    this.plugin.config.Save();
                    this.statuses.SetHudLayout(player, true);
                }

                ImGui.Spacing();

                bool onWeaponDrawn = this.plugin.config.ChangeOnWeaponDrawn;
                if (ImGui.Checkbox("Change HUD when weapon is drawn", ref onWeaponDrawn)) {
                    this.plugin.config.ChangeOnWeaponDrawn = onWeaponDrawn;
                    this.plugin.config.Save();
                    this.statuses.SetHudLayout(player, true);
                }
                int weaponLayout = (int)this.plugin.config.WeaponDrawnLayout + 1;
                if (ImGui.InputInt("##weapon-layout", ref weaponLayout)) {
                    weaponLayout = Math.Max(1, Math.Min(4, weaponLayout));
                    this.plugin.config.WeaponDrawnLayout = (uint)weaponLayout - 1;
                    this.plugin.config.Save();
                    this.statuses.SetHudLayout(player, true);
                }

                ImGui.Spacing();

                bool inInstance = this.plugin.config.ChangeOnInstance;
                if (ImGui.Checkbox("Change HUD when in instance", ref inInstance)) {
                    this.plugin.config.ChangeOnInstance = inInstance;
                    this.plugin.config.Save();
                    this.statuses.SetHudLayout(player, true);
                }
                int instanceLayout = (int)this.plugin.config.InstanceLayout + 1;
                if (ImGui.InputInt("##instance-layout", ref instanceLayout)) {
                    instanceLayout = Math.Max(1, Math.Min(4, instanceLayout));
                    this.plugin.config.InstanceLayout = (uint)instanceLayout - 1;
                    this.plugin.config.Save();
                    this.statuses.SetHudLayout(player, true);
                }

                ImGui.End();
            }
        }

        public void Draw() {
            this.DrawSettings();

            PlayerCharacter player = this.pi.ClientState.LocalPlayer;
            if (player == null) {
                return;
            }

            if (this.statuses.Update(player)) {
                this.statuses.SetHudLayout(null);
            }
        }


    }

    public class Statuses {
        private HudSwapPlugin plugin;
        private DalamudPluginInterface pi;

        public bool InCombat { get; private set; } = false;
        public bool WeaponDrawn { get; private set; } = false;
        public bool InInstance { get; private set; } = false;

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

        public bool Update(PlayerCharacter player) {
            if (player == null) {
                return false;
            }

            bool anyChanged = false;

            anyChanged |= this.SetInInstance(this.pi.ClientState.Condition[ConditionFlag.BoundByDuty]) && this.plugin.config.ChangeOnInstance;

            anyChanged |= this.SetWeaponDrawn(this.IsWeaponDrawn(player)) && this.plugin.config.ChangeOnWeaponDrawn;

            anyChanged |= this.SetInCombat(this.pi.ClientState.Condition[ConditionFlag.InCombat]) && this.plugin.config.ChangeOnCombat;

            return anyChanged;
        }

        public uint CalculateCurrentHud() {
            PlayerCharacter player = this.pi.ClientState.LocalPlayer;
            if (player == null) {
                return 0;
            }
            this.Update(player);

            uint layout = this.plugin.config.DefaultLayout;

            if (this.InInstance && this.plugin.config.ChangeOnInstance) {
                layout = this.plugin.config.InstanceLayout;
            }

            if (this.WeaponDrawn && this.plugin.config.ChangeOnWeaponDrawn) {
                layout = this.plugin.config.WeaponDrawnLayout;
            }

            if (this.InCombat && this.plugin.config.ChangeOnCombat) {
                layout = this.plugin.config.CombatLayout;
            }

            return layout;
        }

        public void SetHudLayout(PlayerCharacter player, bool update = false) {
            if (update && player != null) {
                this.Update(player);
            }

            uint layout = this.CalculateCurrentHud();
            this.plugin.hud.SetHudLayout(layout);
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
