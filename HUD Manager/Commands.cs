using Dalamud.Game.Command;
using Dalamud.Logging;
using HUD_Manager.Configuration;
using HUDManager.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HUD_Manager
{
    public class Commands : IDisposable
    {
        private Plugin Plugin { get; }

        public Commands(Plugin plugin)
        {
            this.Plugin = plugin;

            this.Plugin.CommandManager.AddHandler("/hudman", new CommandInfo(this.OnCommand)
            {
                HelpMessage = "Open the HUD Manager settings or swap to layout name"
                            + "\n\t/hudman → open config window"
                            + "\n\t/hudman swap <layout> → switch to a layout"
                            + "\n\t/hudman condition <condition> true|false|toggle → modify a custom condition"
                            + "\n\t/hudman swapper on|off|toggle → enable or disable automatic layout swapping"
            });
        }

        public void Dispose()
        {
            this.Plugin.CommandManager.RemoveHandler("/hudman");
        }

        private void OnCommand(string command, string args)
        {
            if (string.IsNullOrWhiteSpace(args)) {
                this.Plugin.Ui.OpenConfig();
                return;
            }

            var argsList = args.Split(' ');

            if (argsList[0] == "swap") {
                if (Plugin.Config.SwapsEnabled) {
                    Plugin.ChatGui.PrintError("You must first disable swaps in order to manually swap layouts.");
                    return;
                }

                if (argsList.Length != 2) {
                    Plugin.ChatGui.PrintError("Invalid arguments.");
                    return;
                }

                var entry = this.Plugin.Config.Layouts.FirstOrDefault(e => e.Value.Name == argsList[1]);
                if (entry.Equals(default(KeyValuePair<Guid, SavedLayout>))) {
                    Plugin.ChatGui.PrintError($"Invalid layout \"{argsList[1]}\".");
                    return;
                }

                this.Plugin.Hud.WriteEffectiveLayout(this.Plugin.Config.StagingSlot, entry.Key);
                this.Plugin.Hud.SelectSlot(this.Plugin.Config.StagingSlot, true);
            } else if (argsList[0] == "condition") {
                if (argsList.Length != 3) {
                    Plugin.ChatGui.PrintError("Invalid arguments.");
                    return;
                }

                var cond = Plugin.Config.CustomConditions.Find(c => c.Name == argsList[1]);
                if (cond is null) {
                    Plugin.ChatGui.PrintError($"Invalid condition \"{argsList[1]}\".");
                    return;
                } else if (cond.ConditionType != CustomConditionType.ConsoleToggle) {
                    Plugin.ChatGui.PrintError("That condition cannot be toggled by commands.");
                    return;
                }

                bool? val = null;
                if (argsList[2] == "true" || argsList[2] == "on") {
                    val = true;
                } else if (argsList[2] == "false" || argsList[2] == "off") {
                    val = false;
                } else if (argsList[2] == "toggle") {
                    if (!Plugin.Statuses.CustomConditionStatus.ContainsKey(cond)) {
                        // Default value for toggling a condition we haven't registered.
                        val = true;
                    } else {
                        val = !Plugin.Statuses.CustomConditionStatus[cond];
                    }
                }

                if (!val.HasValue) {
                    Plugin.ChatGui.PrintError($"Invalid setting \"{argsList[2]}\".");
                    return;
                }

                Plugin.Statuses.CustomConditionStatus[cond] = val.Value;
            } else if (argsList[0] == "swapper") {
                if (argsList.Length != 2) {
                    Plugin.ChatGui.PrintError("Invalid arguments.");
                    return;
                }

                bool? val = null;
                if (argsList[1] == "true" || argsList[1] == "on") {
                    val = true;
                } else if (argsList[1] == "false" || argsList[1] == "off") {
                    val = false;
                } else if (argsList[1] == "toggle") {
                    val = !Plugin.Config.SwapsEnabled;
                }

                if (!val.HasValue) {
                    Plugin.ChatGui.PrintError($"Invalid setting \"{argsList[1]}\".");
                    return;
                }

                Plugin.Config.SwapsEnabled = val.Value;
            } else {
                Plugin.ChatGui.PrintError($"Invalid subcommand \"{argsList[0]}\".");
            }
        }
    }
}
