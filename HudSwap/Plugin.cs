using Dalamud.Game.Command;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HudSwap {
    public class HudSwapPlugin : IDalamudPlugin {
        public string Name => "HudSwap";

        private DalamudPluginInterface pi;
        private PluginUI ui;
        private Swapper swapper;

        public HUD Hud { get; private set; }
        public Statuses Statuses { get; private set; }
        public GameFunctions GameFunctions { get; private set; }
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
            this.Statuses = new Statuses(this, this.pi);
            this.GameFunctions = new GameFunctions(this.pi);

            this.swapper = new Swapper(this, this.pi);

            if (this.Config.FirstRun) {
                this.Config.FirstRun = false;
                if (this.Config.Layouts2.Count == 0) {
                    foreach (HudSlot slot in Enum.GetValues(typeof(HudSlot))) {
                        this.ui.ImportSlot($"Auto-import {(int)slot + 1}", slot, false);
                    }
                }
                this.Config.Save();
            }

            this.pi.UiBuilder.OnBuildUi += this.ui.Draw;
            this.pi.UiBuilder.OnOpenConfigUi += this.ui.ConfigUI;
            this.pi.Framework.OnUpdateEvent += this.swapper.OnFrameworkUpdate;

            this.pi.CommandManager.AddHandler("/phudswap", new CommandInfo(OnSettingsCommand) {
                HelpMessage = "Open the HudSwap settings"
            });
            this.pi.CommandManager.AddHandler("/phud", new CommandInfo(OnSwapCommand) {
                HelpMessage = "/phud <name> - Swap to HUD layout called <name>"
            });
        }

        protected virtual void Dispose(bool all) {
            this.pi.UiBuilder.OnBuildUi -= this.ui.Draw;
            this.pi.UiBuilder.OnOpenConfigUi -= this.ui.ConfigUI;
            this.pi.Framework.OnUpdateEvent -= this.swapper.OnFrameworkUpdate;
            this.pi.CommandManager.RemoveHandler("/phudswap");
            this.pi.CommandManager.RemoveHandler("/phud");
        }

        public void Dispose() {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void OnSettingsCommand(string command, string args) {
            this.ui.SettingsVisible = true;
        }

        private void OnSwapCommand(string command, string args) {
            KeyValuePair<Guid, Layout> entry = this.Config.Layouts2.FirstOrDefault(e => e.Value.Name == args);
            if (entry.Equals(default(KeyValuePair<Guid, Layout>))) {
                return;
            }

            Layout layout = entry.Value;

            this.Hud.WriteLayout(this.Config.StagingSlot, layout.Hud);
            this.Hud.SelectSlot(this.Config.StagingSlot, true);
        }
    }
}
