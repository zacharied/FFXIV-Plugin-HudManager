﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using Condition = Dalamud.Game.ClientState.Conditions.Condition;

// TODO: Zone swaps?

namespace HUD_Manager {
    public class Statuses : IDisposable {
        private Plugin Plugin { get; }

        private readonly Dictionary<Status, bool> _condition = new();
        private ClassJob? _job;

        public bool InPvpZone { get; private set; } = false;

        public static byte GetStatus(GameObject actor) {
            // Updated: 6.0
            // 40 57 48 83 EC 70 48 8B F9 E8 ?? ?? ?? ?? 81 BF ?? ?? ?? ?? ?? ?? ?? ??
            const int offset = 0x19DF;
            return Marshal.ReadByte(actor.Address + offset);
        }

        internal static byte GetOnlineStatus(GameObject actor) {
            // Updated: 6.05
            // E8 ?? ?? ?? ?? 48 85 C0 75 54
            const int offset = 0x19C2;
            return Marshal.ReadByte(actor.Address + offset);
        }

        internal static byte GetBardThing(GameObject actor) {
            // Updated: 5.5
            // E8 ?? ?? ?? ?? 48 8B CB E8 ?? ?? ?? ?? 0F B6 43 50
            const int offset = 0x197C;
            return Marshal.ReadByte(actor.Address + offset);
        }

        public Statuses(Plugin plugin) {
            this.Plugin = plugin;

            this.Plugin.ClientState.TerritoryChanged += OnTerritoryChange;
        }
        public void Dispose()
        {
            this.Plugin.ClientState.TerritoryChanged -= OnTerritoryChange;
        }

        public bool Update(Character? player) {
            if (player == null) {
                return false;
            }

            var anyChanged = false;

            var currentJob = this.Plugin.DataManager.GetExcelSheet<ClassJob>()!.GetRow(player.ClassJob.Id);
            if (this._job != null && this._job != currentJob) {
                anyChanged = true;
            }

            this._job = currentJob;

            foreach (Status status in Enum.GetValues(typeof(Status))) {
                var old = this._condition.ContainsKey(status) && this._condition[status];
                this._condition[status] = status.Active(this.Plugin, player);
                anyChanged |= old != this._condition[status];
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

        private (Guid layoutId, List<HudConditionMatch> layers) CalculateCurrentHud() {
            List<HudConditionMatch> layers = new();
            var player = this.Plugin.ClientState.LocalPlayer;
            if (player == null) {
                return (Guid.Empty, layers);
            }

            foreach (var match in this.Plugin.Config.HudConditionMatches) {
                if ((!match.Status.HasValue || this._condition[match.Status.Value]) &&
                    (match.ClassJob == null || this._job?.Abbreviation.ToString() == match.ClassJob)) {
                    if (match.IsLayer) {
                        layers.Add(match);
                        continue;
                    }

                    // The first non-layer condition is the base
                    return (match.LayoutId, layers);
                }
            }

            return (Guid.Empty, layers);
        }

        public void SetHudLayout(Character? player, bool update = false) {
            if (update && player != null) {
                this.Update(player);
            }

            var (layoutId, layers) = this.CalculateCurrentHud();
            if (layoutId == Guid.Empty) {
                return; // FIXME: do something better
            }

            if (!this.Plugin.Config.Layouts.ContainsKey(layoutId)) {
                return; // FIXME: do something better
            }

            this.Plugin.Hud.WriteEffectiveLayout(this.Plugin.Config.StagingSlot, layoutId, layers.ConvertAll(match => match.LayoutId));
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
            var expBar = Plugin.GameGui.GetAtkUnitByName("_Exp", 1);
            if (expBar == null) {
                PluginLog.Warning("Unable to find EXP bar element");
                return false;
            }

            const int expBarAtkMoonIconIndex = 3;
            unsafe {
                // TODO Find a real memory address where this is stored instead of descending into UI elements LMAO
                int i = 0;
                var node = expBar.Value.RootNode;

                if (node->ChildCount < expBarAtkMoonIconIndex) {
                    PluginLog.Warning("Not enough child nodes in EXP bar element");
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
    }

    public class HudConditionMatch {
        /// <summary>
        /// Values stored here should be the abbreviation of the class/job name (all caps).
        /// We do this because using <see cref="ClassJob"/> results in circular dependency errors when serializing.
        /// </summary>
        public string? ClassJob { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Status? Status { get; set; }

        public Guid LayoutId { get; set; }

        public bool IsLayer { get; set; } = false;

        public void Deconstruct(out Guid layoutId, out bool isLayer)
        {
            layoutId = LayoutId;
            isLayer = IsLayer;
        }
    }

    // Note: Changing the names of these is a breaking change
    public enum Status {
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
    }

    public static class StatusExtensions {
        public static string Name(this Status status) {
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
            }

            throw new ApplicationException($"No name was set up for {status}");
        }

        public static bool Active(this Status status, Plugin plugin, Character player) {
            if (player == null) {
                throw new ArgumentNullException(nameof(player), "PlayerCharacter cannot be null");
            }

            if (status > 0) {
                var flag = (ConditionFlag) status;
                return plugin.Condition[flag];
            }

            switch (status) {
                case Status.WeaponDrawn:
                    return (Statuses.GetStatus(player) & 4) > 0;
                case Status.Roleplaying:
                    return Statuses.GetOnlineStatus(player) == 22;
                case Status.PlayingMusic:
                    return Statuses.GetBardThing(player) == 16;
                case Status.InPvp:
                    return plugin.Statuses.InPvpZone;
                case Status.InDialogue:
                    return plugin.Condition[ConditionFlag.OccupiedInEvent]
                        | plugin.Condition[ConditionFlag.OccupiedInQuestEvent]
                        | plugin.Condition[ConditionFlag.OccupiedSummoningBell];
                case Status.InFate:
                    return plugin.Statuses.IsInFate(player);
                case Status.InFateLevelSynced:
                    return plugin.Statuses.IsInFate(player) && plugin.Statuses.IsLevelSynced(player);
                case Status.InSanctuary:
                    return plugin.Statuses.IsInSanctuary();
            }

            return false;
        }
    }
}
