using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using HUD_Manager.Configuration;
using Resourcer;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace HUD_Manager {
    public class Plugin : IDalamudPlugin {
        public string Name => "HUD Manager";

        private PluginUi Ui { get; set; } = null!;
        private Swapper Swapper { get; set; } = null!;

        public DalamudPluginInterface Interface { get; private set; } = null!;
        public Hud Hud { get; private set; } = null!;
        public Statuses Statuses { get; private set; } = null!;
        public GameFunctions GameFunctions { get; private set; } = null!;
        public Config Config { get; private set; } = null!;
        public HelpFile Help { get; private set; } = null!;

        public void Initialize(DalamudPluginInterface pluginInterface) {
            this.Interface = pluginInterface;

            // it's time to do a murder
            _ = new HudSwapMurderer(this);

            this.Config = Migrator.LoadConfig(this);
            this.Config.Initialize(this.Interface);
            this.Config.Save();

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();
            this.Help = deserializer.Deserialize<HelpFile>(Resource.AsString("help.yaml"));

            this.Ui = new PluginUi(this);
            this.Hud = new Hud(this);
            this.Statuses = new Statuses(this);
            this.GameFunctions = new GameFunctions(this);
            this.Swapper = new Swapper(this);

            if (this.Config.FirstRun) {
                this.Config.FirstRun = false;
                if (this.Config.Layouts.Count == 0) {
                    foreach (HudSlot slot in Enum.GetValues(typeof(HudSlot))) {
                        this.Ui.ImportSlot($"Auto-import {(int) slot + 1}", slot, false);
                    }
                }

                this.Config.Save();
            }

            this.Interface.UiBuilder.OnBuildUi += this.Ui.Draw;
            this.Interface.UiBuilder.OnOpenConfigUi += this.Ui.ConfigUi;
            this.Interface.Framework.OnUpdateEvent += this.Swapper.OnFrameworkUpdate;

            this.Interface.CommandManager.AddHandler("/hudman", new CommandInfo(this.OnCommand) {
                HelpMessage = "Open the HUD Manager settings or swap to layout name",
            });
        }

        public void Dispose() {
            this.Interface.UiBuilder.OnBuildUi -= this.Ui.Draw;
            this.Interface.UiBuilder.OnOpenConfigUi -= this.Ui.ConfigUi;
            this.Interface.Framework.OnUpdateEvent -= this.Swapper.OnFrameworkUpdate;
            this.Interface.CommandManager.RemoveHandler("/hudman");
        }

        private void OnCommand(string command, string args) {
            if (string.IsNullOrWhiteSpace(args)) {
                this.Ui.SettingsVisible = true;
                return;
            }

            var entry = this.Config.Layouts.FirstOrDefault(e => e.Value.Name == args);
            if (entry.Equals(default(KeyValuePair<Guid, SavedLayout>))) {
                return;
            }

            this.Hud.WriteEffectiveLayout(this.Config.StagingSlot, entry.Key);
            this.Hud.SelectSlot(this.Config.StagingSlot, true);
        }
    }
}
