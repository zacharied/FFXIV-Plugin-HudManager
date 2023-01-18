﻿using Dalamud.Data;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;
using FFXIVClientStructs;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;
using HUDManager;
using HUDManager.Configuration;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

// TODO: Zone swaps?

namespace HUD_Manager
{
    public class Statuses : IDisposable
    {
        private Plugin Plugin { get; }

        public readonly Dictionary<Status, bool> Condition = new();
        public ClassJob? Job { get; private set; }

        public (HudConditionMatch? activeLayout, List<HudConditionMatch> layeredLayouts) ResultantLayout = (null, new());

        public CustomConditionStatusContainer CustomConditionStatus { get; } = new();

        public bool NeedsForceUpdate { get; private set; }

        public bool InPvpZone { get; private set; } = false;
        private bool SanctuaryDetectionFailed = false;

        public static byte GetStatus(GameObject actor)
        {
            // Updated: 6.3
            const int offset = 0x1B1B;
            return Marshal.ReadByte(actor.Address + offset);
        }

        internal static byte GetOnlineStatus(GameObject actor)
        {
            // Updated: 6.3
            const int offset = 0x1B02;
            return Marshal.ReadByte(actor.Address + offset);
        }

        internal static byte GetBardThing(GameObject actor)
        {
            // Updated: 6.3
            const int offset = 0x1B00;
            return Marshal.ReadByte(actor.Address + offset);
        }

        public Statuses(Plugin plugin)
        {
            this.Plugin = plugin;

            foreach (var cond in this.Plugin.Config.CustomConditions) {
                CustomConditionStatus[cond] = false;
            }

            this.Plugin.ClientState.TerritoryChanged += OnTerritoryChange; 
        }
        public void Dispose()
        {
            this.Plugin.ClientState.TerritoryChanged -= OnTerritoryChange;
        }

        public bool Update()
        {
            var player = Plugin.ClientState.LocalPlayer;
            if (player is null) {
                return false;
            }

            var anyChanged = false;

            var currentJob = this.Plugin.DataManager.GetExcelSheet<ClassJob>()!.GetRow(player.ClassJob.Id);
            if (this.Job != null && this.Job != currentJob) {
                anyChanged = true;
            }

            this.Job = currentJob;

            foreach (Status status in Enum.GetValues(typeof(Status))) {
                var old = this.Condition.ContainsKey(status) && this.Condition[status];
                this.Condition[status] = status.Active(this.Plugin, player);
                anyChanged |= old != this.Condition[status];
            }

            return anyChanged;
        }

        private void OnTerritoryChange(object? sender, ushort tid)
        {
            var territory = this.Plugin.DataManager.GetExcelSheet<TerritoryType>()!.GetRow(tid);
            if (territory == null) {
                PluginLog.Warning("Unable to get territory data for current zone");
                return;
            }
            this.InPvpZone = territory.IsPvpZone;
        }

        /// <summary>
        /// Get the current layout data according to the conditions that match the game state.
        /// </summary>
        private (HudConditionMatch? layoutId, List<HudConditionMatch> layers) CalculateResultantLayout()
        {
            List<HudConditionMatch> layers = new();
            var player = this.Plugin.ClientState.LocalPlayer;
            if (player == null) {
                return (null, layers);
            }

            foreach (var match in this.Plugin.Config.HudConditionMatches) {
                if (match.IsActivated(Plugin)) {
                    if (match.IsLayer && Plugin.Config.AdvancedSwapMode) {
                        layers.Add(match);
                        continue;
                    }

                    // The first non-layer condition is the base
                    return (match, layers);
                }
            }

            return (null, layers);
        }

        public void SetHudLayout()
        {
            NeedsForceUpdate = false;

            ResultantLayout = this.CalculateResultantLayout();
            if (ResultantLayout.activeLayout is null) {
                NeedsForceUpdate = true;
                return;
            }

            if (!this.Plugin.Config.Layouts.ContainsKey(ResultantLayout.activeLayout.LayoutId)) {
                PluginLog.Error($"Attempt to set nonexistent layout \"{ResultantLayout.activeLayout.LayoutId}\".");
                return;
            }

            this.Plugin.Hud.WriteEffectiveLayout(this.Plugin.Config.StagingSlot, ResultantLayout.activeLayout.LayoutId, ResultantLayout.layeredLayouts.ConvertAll(match => match.LayoutId));
            this.Plugin.Hud.SelectSlot(this.Plugin.Config.StagingSlot, true);
        }

        public bool IsInFate(Character player)
        {
            unsafe {
                var fateManager = *FateManager.Instance();
                return (fateManager.FateJoined & 1) == 1;
            }
        }

        public bool IsLevelSynced(Character player)
        {
            unsafe {
                var uiPlayerState = UIState.Instance()->PlayerState;
                return (uiPlayerState.IsLevelSynced & 1) > 0;
            }
        }

        public bool IsInSanctuary()
        {
            if (SanctuaryDetectionFailed)
                return false;

            var expBar = Plugin.GameGui.GetAtkUnitByName("_Exp", 1);
            if (!expBar.HasValue) {
                PluginLog.Error("Unable to find EXP bar element for sanctuary detection");
                SanctuaryDetectionFailed = true;
                return false;
            }

            const int expBarAtkMoonIconIndex = 3;
            unsafe {
                // TODO Find a real memory address where this is stored instead of descending into UI elements LMAO
                int i = 0;
                var node = expBar.Value.RootNode;

                if (node->ChildCount < expBarAtkMoonIconIndex) {
                    PluginLog.Error("Not enough child nodes in EXP bar element for sanctuary detection");
                    SanctuaryDetectionFailed = true;
                    return false;
                }

                node = node->ChildNode;
                while (i < expBarAtkMoonIconIndex) {
                    node = node->PrevSiblingNode;
                    i++;
                }

                return node->IsVisible;
            }
        }

        public bool IsChatFocused()
        {
            const uint ChatLogNodeId = 5;
            const uint ChatEntryNodeId = 2;

            var chatLog = Plugin.GameGui.GetAtkUnitByName("ChatLog", 1);
            if (chatLog is null)
                return false;

            unsafe {
                // Updated 6.08
                var textInput = chatLog.Value.GetNodeById(ChatLogNodeId);
                if (textInput is null)
                    return false;

                var node = textInput->GetAsAtkComponentNode()->Component->UldManager.SearchNodeById(ChatEntryNodeId);
                if (node is null)
                    return false;

                return node->IsVisible;
            }
        }

        public bool GamepadModeStatus()
        {
            unsafe {
                var config = ConfigModule.Instance();
                return config->GetValueById((short)ConfigOption.PadMode)->UInt > 0;
            }
        }

        public class CustomConditionStatusContainer
        {
            private Dictionary<CustomCondition, bool> Status { get; } = new();
            private bool Updated { get; set; } = false;

            public bool this[CustomCondition c]
            {
                get
                {
                    return Status[c];
                }

                set
                {
                    Status[c] = value;
                    Updated = true;
                }
            }

            public bool IsUpdated()
            {
                var v = Updated;
                Updated = false;
                return v;
            }

            public bool ContainsKey(CustomCondition c) => Status.ContainsKey(c);

            public bool TryGetValue(CustomCondition c, out bool v) => Status.TryGetValue(c, out v);

            public void Toggle(CustomCondition c) => this[c] = !this[c];
        }
    }

    public class HudConditionMatch
    {
        public ClassJobCategoryId? ClassJobCategory { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Status? Status { get; set; }
        public CustomCondition? CustomCondition { get; set; }

        public Guid LayoutId { get; set; }

        public bool IsLayer { get; set; } = false;

        public bool IsActivated(Plugin plugin)
        {
            var player = plugin.ClientState.LocalPlayer;
            if (player is null) {
                PluginLog.Warning("can't check job activation when player is null");
                return false;
            }

            bool statusMet = !this.Status.HasValue || plugin.Statuses.Condition[this.Status.Value];
            bool customConditionMet = this.CustomCondition?.IsMet(plugin) ?? true;
            bool jobMet = this.ClassJobCategory is null 
                || this.ClassJobCategory.Value.IsActivated(plugin.ClientState.LocalPlayer!.ClassJob.GameData!);

            return statusMet && customConditionMet && jobMet;
        }
    }

    // Note: Changing the names of these is a breaking change
    public enum Status
    {
        InCombat = ConditionFlag.InCombat,
        WeaponDrawn = -1,
        InInstance = ConditionFlag.BoundByDuty,
        Crafting = ConditionFlag.Crafting,
        Gathering = ConditionFlag.Gathering,
        Fishing = ConditionFlag.Fishing,
        Mounted = ConditionFlag.Mounted,
        Roleplaying = -2,
        PlayingMusic = -3,
        InPvp = -4,
        InDialogue = -5,
        InFate = -6,
        InFateLevelSynced = -7,
        InSanctuary = -8,
        ChatFocused = -9,
        InputModeKbm = -10,
        InputModeGamepad = -11,
    }

    public static class StatusExtensions
    {
        public static string Name(this Status status)
        {
            switch (status) {
                case Status.InCombat:
                    return "In combat";
                case Status.WeaponDrawn:
                    return "Weapon drawn";
                case Status.InInstance:
                    return "In instance";
                case Status.Crafting:
                    return "Crafting";
                case Status.Gathering:
                    return "Gathering";
                case Status.Fishing:
                    return "Fishing";
                case Status.Mounted:
                    return "Mounted";
                case Status.Roleplaying:
                    return "Roleplaying";
                case Status.PlayingMusic:
                    return "Performing music";
                case Status.InPvp:
                    return "In PvP";
                case Status.InDialogue:
                    return "In dialogue";
                case Status.InFate:
                    return "In FATE area";
                case Status.InFateLevelSynced:
                    return "Level-synced for FATE";
                case Status.InSanctuary:
                    return "In a sanctuary";
                case Status.ChatFocused:
                    return "Chat focused";
                case Status.InputModeKbm:
                    return "Keyboard/mouse mode";
                case Status.InputModeGamepad:
                    return "Gamepad mode";
            }

            throw new ApplicationException($"No name was set up for {status}");
        }

        public static bool Active(this Status status, Plugin plugin, Character? player = null)
        {
            // Temporary stopgap until we remove the argument entirely
            if (player == null)
                player = plugin.ClientState.LocalPlayer;

            // Player being null is a common enough edge case that callers of this function shouldn't have
            //  to catch an exception on their own. We can't really do anything useful if it's null so we
            //  might as well just return false here; it makes no difference to the caller.
            if (player == null) {
                if (RequiresPlayer.Contains(status))
                    return false;
            }

            if (status > 0) {
                var flag = (ConditionFlag)status;
                return plugin.Condition[flag];
            }

            switch (status) {
                case Status.WeaponDrawn:
                    return (Statuses.GetStatus(player!) & 4) > 0;
                case Status.Roleplaying:
                    return Statuses.GetOnlineStatus(player!) == 22;
                case Status.PlayingMusic:
                    return Statuses.GetBardThing(player!) == 16;
                case Status.InPvp:
                    return plugin.Statuses.InPvpZone;
                case Status.InDialogue:
                    return plugin.Condition[ConditionFlag.OccupiedInEvent]
                        | plugin.Condition[ConditionFlag.OccupiedInQuestEvent]
                        | plugin.Condition[ConditionFlag.OccupiedSummoningBell];
                ////case Status.InFate:
                ////    return plugin.Statuses.IsInFate(player!);
                ////case Status.InFateLevelSynced:
                ////    return plugin.Statuses.IsInFate(player!) && plugin.Statuses.IsLevelSynced(player!);
                case Status.InSanctuary:
                    return plugin.Statuses.IsInSanctuary();
                case Status.ChatFocused:
                    return plugin.Statuses.IsChatFocused();
                case Status.InputModeKbm:
                    return !plugin.Statuses.GamepadModeStatus();
                case Status.InputModeGamepad:
                    return plugin.Statuses.GamepadModeStatus();
            }

            return false;
        }

        private static readonly ReadOnlyCollection<Status> RequiresPlayer = new(new List<Status>()
        {
            Status.WeaponDrawn,
            Status.Roleplaying,
            Status.PlayingMusic,
            Status.InFate,
            Status.InFateLevelSynced
        });
    }
}
