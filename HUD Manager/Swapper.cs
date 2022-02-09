using System;
using System.Collections.Generic;
using Dalamud.Game;
using Dalamud.Game.Internal;

namespace HUD_Manager {
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

            var updated = this.Plugin.Statuses.Update(player);

            if (updated) {
                this.Plugin.Statuses.SetHudLayout(null);
            }
        }
    }
}
