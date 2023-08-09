using Dalamud.Game;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Logging;
using HUDManager.Configuration;
using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Conditions;

namespace HUD_Manager
{
    public class Swapper : IDisposable
    {
        private Plugin Plugin { get; }

        public bool SwapsTemporarilyDisabled = false;

        public Swapper(Plugin plugin)
        {
            this.Plugin = plugin;

            this.Plugin.Framework.Update += this.OnFrameworkUpdate;
            this.Plugin.ClientState.Login += this.OnLogin;
            this.Plugin.ClientState.TerritoryChanged += this.OnTerritoryChange;
        }

        public void Dispose()
        {
            this.Plugin.Framework.Update -= this.OnFrameworkUpdate;
            this.Plugin.ClientState.Login -= this.OnLogin;
            this.Plugin.ClientState.TerritoryChanged -= this.OnTerritoryChange;
        }

        public void OnLogin(object? sender, EventArgs e)
        {
            // Player object is null here, not much to do
        }

        public void OnTerritoryChange(object? sender, ushort tid)
        {
            if (!this.Plugin.Ready) {
                return;
            }

            this.Plugin.Statuses.Update();
            this.Plugin.Statuses.SetHudLayout();
        }

        public void OnFrameworkUpdate(Framework framework)
        {
            if (!this.Plugin.Ready || !this.Plugin.Config.SwapsEnabled || SwapsTemporarilyDisabled || !this.Plugin.Config.UnderstandsRisks) {
                return;
            }

            var player = this.Plugin.ClientState.LocalPlayer;
            if (player == null) {
                return;
            }

            // Skipping due to bugs caused by HUD swaps while Character Config is open
            if (Util.IsCharacterConfigOpen()) {
                return;
            }

            // Skipping due to HUD swaps in cutscenes causing main menu to become visible
            if (Plugin.Condition[ConditionFlag.OccupiedInCutSceneEvent]
                || Plugin.Condition[ConditionFlag.WatchingCutscene78] // Used in Dalamud's cutscene check
                || Plugin.Condition[ConditionFlag.BoundByDuty95] // GATE: Air Force One
                || Plugin.Condition[ConditionFlag.PlayingLordOfVerminion]
                || Plugin.Condition[ConditionFlag.BetweenAreas51]) { // Loading Lord of Verminion
                return;
            }

            var updated = this.Plugin.Statuses.Update()
                || this.Plugin.Keybinder.UpdateKeyState()
                || this.Plugin.Statuses.CustomConditionStatus.IsUpdated();

            if (updated || this.Plugin.Statuses.NeedsForceUpdate) {
                this.Plugin.Statuses.SetHudLayout();
            }
        }
    }
}
