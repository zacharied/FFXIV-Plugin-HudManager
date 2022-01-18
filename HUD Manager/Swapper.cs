using System;
using System.Collections.Generic;
using Dalamud.Game;
using Dalamud.Game.Internal;

namespace HUD_Manager {
    public class Swapper : IDisposable {
        private Plugin Plugin { get; }

        private bool swapSupressed = false;

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

            if (this.Plugin.Config.PreventSwapsWhilePetHotbarActive) {
                var petHotbarActive = Util.PetHotbarActive();
                if (updated && petHotbarActive) {
                    // There is a swap we want to do but we're in pet mode.
                    swapSupressed = true;
                } else if (swapSupressed && !petHotbarActive) {
                    // Perform the swap that has been suppressed during the pet bar.
                    swapSupressed = false;
                    this.Plugin.Statuses.SetHudLayout(null);
                } else if (!petHotbarActive && updated) {
                    // Swap in reaction to a state change like normal.
                    this.Plugin.Statuses.SetHudLayout(null);
                }
            } else if (updated) {
                this.Plugin.Statuses.SetHudLayout(null);
            }
        }
    }
}
