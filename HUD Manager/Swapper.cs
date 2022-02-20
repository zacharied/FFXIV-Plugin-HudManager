using Dalamud.Game;
using System;

namespace HUD_Manager
{
    public class Swapper : IDisposable {
        private Plugin Plugin { get; }

        public Swapper(Plugin plugin) {
            this.Plugin = plugin;

            this.Plugin.Framework.Update += this.OnFrameworkUpdate;
        }

        public void Dispose() {
            this.Plugin.Framework.Update -= this.OnFrameworkUpdate;
        }

        public void OnFrameworkUpdate(Framework framework) {
            if (!this.Plugin.Config.SwapsEnabled || !this.Plugin.Config.UnderstandsRisks) {
                return;
            }

            var player = this.Plugin.ClientState.LocalPlayer;
            if (player == null) {
                return;
            }

            var updated = this.Plugin.Statuses.Update(player) || this.Plugin.Statuses.CustomConditionStatusUpdated;

            if (updated) {
                this.Plugin.Statuses.SetHudLayout(null);
                this.Plugin.Statuses.CustomConditionStatusUpdated = false;
            }
        }
    }
}
