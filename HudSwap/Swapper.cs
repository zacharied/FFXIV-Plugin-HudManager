using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Game.Internal;
using Dalamud.Plugin;
using System;

namespace HudSwap {
    public class Swapper {
        private readonly HudSwapPlugin plugin;
        private readonly DalamudPluginInterface pi;

        public Swapper(HudSwapPlugin plugin, DalamudPluginInterface pi) {
            this.plugin = plugin;
            this.pi = pi;
        }

        public void OnFrameworkUpdate(Framework framework) {
            if (framework == null) {
                throw new ArgumentNullException(nameof(framework), "Framework cannot be null");
            }

            if (!(this.plugin.Config.SwapsEnabled && this.plugin.Config.UnderstandsRisks)) {
                return;
            }

            PlayerCharacter player = this.pi.ClientState.LocalPlayer;
            if (player == null) {
                return;
            }

            if (this.plugin.Statuses.Update(player)) {
                this.plugin.Statuses.SetHudLayout(null);
            }
        }
    }
}
