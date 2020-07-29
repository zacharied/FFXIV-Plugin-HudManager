using Dalamud.Plugin;
using System;

namespace HudSwap {
    public class HudSwapPlugin : IDalamudPlugin {
        public string Name => "HudSwap";

        private DalamudPluginInterface pi;
        private PluginUI ui;
        public HUD hud;
        public Configuration config;

        public void Initialize(DalamudPluginInterface pluginInterface) {
            this.pi = pluginInterface;

            this.config = this.pi.GetPluginConfig() as Configuration ?? new Configuration();
            this.config.Initialize(this.pi);

            this.ui = new PluginUI(this, this.pi);
            this.hud = new HUD(this.pi);

            this.pi.UiBuilder.OnBuildUi += this.ui.Draw;
            this.pi.UiBuilder.OnOpenConfigUi += this.ui.ConfigUI;

            this.pi.CommandManager.AddHandler("/phudswap", new Dalamud.Game.Command.CommandInfo(OnCommand) {
                HelpMessage = "Open the HudSwap settings"
            });
        }

        public void Dispose() {
            this.pi.UiBuilder.OnBuildUi -= this.ui.Draw;
            this.pi.UiBuilder.OnOpenConfigUi -= this.ui.ConfigUI;
            this.pi.CommandManager.RemoveHandler("/phudswap");
        }

        private void OnCommand(string command, string args) {
            this.ui.SettingsVisible = true;
        }
    }
}
