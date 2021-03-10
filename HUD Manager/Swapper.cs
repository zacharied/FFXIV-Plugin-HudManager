using Dalamud.Game.Internal;

namespace HUD_Manager {
    public class Swapper {
        private Plugin Plugin { get; }

        public Swapper(Plugin plugin) {
            this.Plugin = plugin;
        }

        public void OnFrameworkUpdate(Framework framework) {
            if (!this.Plugin.Config.SwapsEnabled || !this.Plugin.Config.UnderstandsRisks) {
                return;
            }

            var player = this.Plugin.Interface.ClientState.LocalPlayer;
            if (player == null) {
                return;
            }

            if (this.Plugin.Statuses.Update(player)) {
                this.Plugin.Statuses.SetHudLayout(null);
            }
        }
    }
}
