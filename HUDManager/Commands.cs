using Dalamud.Game.Command;
using HUDManager.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HUDManager;

public sealed class Commands : IDisposable
{
    public const string QuoteCharacter = "\"";
    private Plugin Plugin { get; }

    public Commands(Plugin plugin)
    {
        Plugin = plugin;

        Plugin.CommandManager.AddHandler("/hudman", new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the HUD Manager settings or swap to layout name"
                + "\n\t/hudman → open config window"
                + "\n\t/hudman swap <layout> → switch to a layout"
                + "\n\t/hudman condition <condition> true|false|toggle → modify a custom condition"
                + "\n\t/hudman swapper on|off|toggle → enable or disable automatic layout swapping",
        });
    }

    public void Dispose()
    {
        Plugin.CommandManager.RemoveHandler("/hudman");
    }

    private void OnCommand(string command, string args)
    {
        if (string.IsNullOrWhiteSpace(args)) {
            Plugin.WindowManager.Settings.Toggle();
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

            var entry = Plugin.Config.Layouts.FirstOrDefault(e => e.Value.Name == argsList[1]);
            if (entry.Equals(default(KeyValuePair<Guid, SavedLayout>))) {
                Plugin.ChatGui.PrintError($"Invalid layout \"{argsList[1]}\".");
                return;
            }

            Plugin.WindowManager.Settings.SelectedLayout = entry.Key;
            if (Plugin.PlayerState is { IsLoaded : true } playerState) {
                Plugin.HudStage.Apply(new HudDescriptor(playerState.ClassJob.RowId, entry.Key, []), StageReason.Command, StageFlags.ChangeSlot);
            } else {
                Plugin.ChatGui.PrintError("Player is not loaded.");
            }
        } else if (argsList[0] == "condition") {
            var quotedArgs = GetArgsWithQuotes(args);
            if (quotedArgs is null) {
                Plugin.ChatGui.PrintError("Malformed quotation marks.");
                return;
            }

            if (quotedArgs.Length != 3) {
                Plugin.ChatGui.PrintError("Invalid arguments.");
                return;
            }

            var cond = Plugin.Config.CustomConditions.Find(c => c.Name == quotedArgs[1]);
            if (cond is null) {
                Plugin.ChatGui.PrintError($"Invalid condition \"{quotedArgs[1]}\".");
                return;
            } else if (cond.ConditionType != CustomConditionType.ConsoleToggle) {
                Plugin.ChatGui.PrintError("That condition cannot be toggled by commands.");
                return;
            }

            bool? val = null;
            if (quotedArgs[2] == "true" || quotedArgs[2] == "on") {
                val = true;
            } else if (quotedArgs[2] == "false" || quotedArgs[2] == "off") {
                val = false;
            } else if (quotedArgs[2] == "toggle") {
                if (!Plugin.Statuses.CustomConditionStatus.ContainsKey(cond)) {
                    // Default value for toggling a condition we haven't registered.
                    val = true;
                } else {
                    val = !Plugin.Statuses.CustomConditionStatus[cond];
                }
            }

            if (!val.HasValue) {
                Plugin.ChatGui.PrintError($"Invalid setting \"{quotedArgs[2]}\".");
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

    private static string[]? GetArgsWithQuotes(string argString)
    {
        var newArgs = new List<string>();

        var parts = argString.Split(QuoteCharacter);
        if (parts.Length % 2 == 0)
            return null;

        for (var i = 0; i < parts.Length; i++) {
            if (i % 2 == 0) {
                // non-quoted
                var localParts = parts[i].Split(" ");
                if (parts.Length > 2)
                    if ((i - 2) % 4 == 0)
                        newArgs.AddRange(localParts.Skip(1));
                    else
                        newArgs.AddRange(localParts.SkipLast(1));
                else
                    newArgs.AddRange(localParts);
            } else {
                // quoted
                newArgs.Add(parts[i]);
            }
        }

        return newArgs.ToArray();
    }
}
