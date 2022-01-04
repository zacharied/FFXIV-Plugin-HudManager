using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Command;
using HUD_Manager.Configuration;

namespace HUD_Manager {
    public class Commands : IDisposable {
        private Plugin Plugin { get; }

        public Commands(Plugin plugin) {
            this.Plugin = plugin;

            this.Plugin.CommandManager.AddHandler("/hudman", new CommandInfo(this.OnCommand) {
                HelpMessage = "Open the HUD Manager settings or swap to layout name",
            });
        }

        public void Dispose() {
            this.Plugin.CommandManager.RemoveHandler("/hudman");
        }

        private void OnCommand(string command, string args) {
            if (string.IsNullOrWhiteSpace(args)) {
                this.Plugin.Ui.OpenConfig();
                return;
            }

            var entry = this.Plugin.Config.Layouts.FirstOrDefault(e => e.Value.Name == args);
            if (entry.Equals(default(KeyValuePair<Guid, SavedLayout>))) {
                return;
            }

            this.Plugin.Hud.WriteEffectiveLayout(this.Plugin.Config.StagingSlot, entry.Key);
            this.Plugin.Hud.SelectSlot(this.Plugin.Config.StagingSlot, true);
        }
    }
}
