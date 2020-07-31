using Dalamud.Plugin;
using System;

namespace HudSwap {
    public class HudSwapPlugin : IDalamudPlugin {
        public string Name => "HudSwap";

        private DalamudPluginInterface pi;
        private PluginUI ui;
        public HUD Hud { get; private set; }
        public PluginConfig Config { get; private set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "nah")]
        public void Initialize(DalamudPluginInterface pluginInterface) {
            this.pi = pluginInterface ?? throw new ArgumentNullException(nameof(pluginInterface), "DalamudPluginInterface cannot be null");
            try {
                this.Config = this.pi.GetPluginConfig() as PluginConfig ?? new PluginConfig();
            } catch (Exception) {
                this.pi.UiBuilder.OnBuildUi += PluginUI.ConfigError;
                return;
            }
            this.Config.Initialize(this.pi);

            this.ui = new PluginUI(this, this.pi);
            this.Hud = new HUD(this.pi);

            if (this.Config.FirstRun) {
                this.Config.FirstRun = false;
                if (this.Config.Layouts.Count == 0) {
                    foreach (HudSlot slot in Enum.GetValues(typeof(HudSlot))) {
                        this.ui.ImportSlot(slot, $"Auto-import {(int)slot + 1}", false);
                    }
                }
                this.Config.Save();
            }

            this.pi.UiBuilder.OnBuildUi += this.ui.Draw;
            this.pi.UiBuilder.OnOpenConfigUi += this.ui.ConfigUI;

            this.pi.CommandManager.AddHandler("/phudswap", new Dalamud.Game.Command.CommandInfo(OnCommand) {
                HelpMessage = "Open the HudSwap settings"
            });
        }

        protected virtual void Dispose(bool all) {
            this.pi.UiBuilder.OnBuildUi -= this.ui.Draw;
            this.pi.UiBuilder.OnOpenConfigUi -= this.ui.ConfigUI;
            this.pi.CommandManager.RemoveHandler("/phudswap");
        }

        public void Dispose() {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void OnCommand(string command, string args) {
            this.ui.SettingsVisible = true;
        }
    }
}
