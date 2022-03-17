using Dalamud.Game;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Logging;
using HUDManager.Configuration;
using System;
using System.Collections.Generic;

namespace HUD_Manager
{
    public class Swapper : IDisposable
    {
        private Plugin Plugin { get; }

        private bool firstTerritoryChangeFired = false;

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
            firstTerritoryChangeFired = false;
        }

        public void OnTerritoryChange(object? sender, ushort tid)
        {
            if (this.firstTerritoryChangeFired)
                return;

            this.Plugin.Statuses.Update(this.Plugin.ClientState.LocalPlayer);
            this.Plugin.Statuses.SetHudLayout(null);
        }

        public void OnFrameworkUpdate(Framework framework)
        {
            if (!this.Plugin.Config.SwapsEnabled || !this.Plugin.Config.UnderstandsRisks) {
                return;
            }

            var player = this.Plugin.ClientState.LocalPlayer;
            if (player == null) {
                return;
            }

            var updated = this.Plugin.Statuses.Update(player) || this.Plugin.Keybinder.UpdateKeyState() || this.Plugin.Statuses.CustomConditionStatus.IsUpdated();

            if (updated) {
                this.Plugin.Statuses.SetHudLayout(null);
            }
        }
    }
}
