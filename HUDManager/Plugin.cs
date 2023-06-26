using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using HUD_Manager.Configuration;
using HUD_Manager.Structs;
using HUD_Manager.Ui;
using HUDManager;
using Resourcer;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dalamud.Game.Config;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace HUD_Manager
{
    public class Plugin : IDalamudPlugin
    {
        public string Name => "HUD Manager";

        public DalamudPluginInterface Interface { get; init; }
        public CommandManager CommandManager { get; init; }
        public DataManager DataManager { get; init; }
        public ClientState ClientState { get; init; }
        public Condition Condition { get; init; }
        public Framework Framework { get; init; }
        public SigScanner SigScanner { get; init; }
        public GameGui GameGui { get; init; }
        public ChatGui ChatGui { get; init; }
        public KeyState KeyState { get; init; }
        public GameConfig GameConfig { get; init; }

        public Swapper Swapper { get; set; } = null!;
        private Commands Commands { get; set; } = null!;

        public Interface Ui { get; private set; } = null!;
        public Hud Hud { get; private set; } = null!;
        public Statuses Statuses { get; private set; } = null!;
        public Config Config { get; private set; } = null!;
        public HelpFile Help { get; private set; } = null!;
        public GameFunctions GameFunctions { get; init; }
        public PetHotbar PetHotbar { get; init; }
        public Keybinder Keybinder { get; init; }

        public bool Ready;

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager,
            [RequiredVersion("1.0")] DataManager dataManager,
            [RequiredVersion("1.0")] ClientState clientState,
            [RequiredVersion("1.0")] Dalamud.Game.ClientState.Conditions.Condition condition,
            [RequiredVersion("1.0")] Framework framework,
            [RequiredVersion("1.0")] SigScanner sigScanner,
            [RequiredVersion("1.0")] GameGui gameGui,
            [RequiredVersion("1.0")] ChatGui chatGui,
            [RequiredVersion("1.0")] KeyState keyState,
            [RequiredVersion("1.0")] GameConfig gameConfig)
        {
            this.Interface = pluginInterface;
            this.CommandManager = commandManager;
            this.DataManager = dataManager;
            this.ClientState = clientState;
            this.Condition = condition;
            this.Framework = framework;
            this.SigScanner = sigScanner;
            this.GameGui = gameGui;
            this.ChatGui = chatGui;
            this.KeyState = keyState;
            this.GameConfig = gameConfig;

            ClassJobCategoryIdExtensions.Initialize(this);
            ElementKindExt.Initialize(this.DataManager);

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
            this.PetHotbar = new PetHotbar(this);
            this.Keybinder = new Keybinder(this);

            if (!this.Config.FirstRun) {
                this.Ready = true;
                return;
            }

            this.Config.FirstRun = false;
            if (this.Config.Layouts.Count == 0) {
                foreach (HudSlot slot in Enum.GetValues(typeof(HudSlot))) {
                    this.Hud.ImportSlot(
                        $"Auto-import {(int)slot + 1} ({DateTime.Now.ToString("yyyy-MM-dd HH\\:mm\\:ss", CultureInfo.InvariantCulture)})", slot, false);
                }
            }

            this.Config.Save();

            this.Ready = true;
        }

        public void Dispose()
        {
            this.Commands.Dispose();
            this.Ui.Dispose();
            this.Swapper.Dispose();
            this.PetHotbar.Dispose();
            this.Hud.Dispose();
        }
    }
}
