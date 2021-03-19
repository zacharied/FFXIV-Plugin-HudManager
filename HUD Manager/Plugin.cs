using System;
using Dalamud.Plugin;
using HUD_Manager.Configuration;
using HUD_Manager.Ui;
using Resourcer;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace HUD_Manager {
    public class Plugin : IDalamudPlugin {
        public string Name => "HUD Manager";

        private Swapper Swapper { get; set; } = null!;
        private Commands Commands { get; set; } = null!;
        public DalamudPluginInterface Interface { get; private set; } = null!;
        public Interface Ui { get; private set; } = null!;
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

            this.Ui = new Interface(this);
            this.Hud = new Hud(this);
            this.Statuses = new Statuses(this);
            this.GameFunctions = new GameFunctions(this);
            this.Swapper = new Swapper(this);
            this.Commands = new Commands(this);

            if (!this.Config.FirstRun) {
                return;
            }

            this.Config.FirstRun = false;
            if (this.Config.Layouts.Count == 0) {
                foreach (HudSlot slot in Enum.GetValues(typeof(HudSlot))) {
                    this.Hud.ImportSlot($"Auto-import {(int) slot + 1}", slot, false);
                }
            }

            this.Config.Save();
        }

        public void Dispose() {
            this.Commands.Dispose();
            this.Ui.Dispose();
            this.Swapper.Dispose();
        }
    }
}
