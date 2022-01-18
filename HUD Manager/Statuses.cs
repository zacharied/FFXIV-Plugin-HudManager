using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;
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

        private Guid CalculateCurrentHud() {
            var player = this.Plugin.ClientState.LocalPlayer;
            if (player == null) {
                return Guid.Empty;
            }

            foreach (var match in this.Plugin.Config.HudConditionMatches) {
                if ((!match.Status.HasValue || this._condition[match.Status.Value]) &&
                    (match.ClassJob == null || this._job?.Abbreviation.ToString() == match.ClassJob)) {
                    return match.LayoutId;
                }
            }

            return Guid.Empty;
        }

        public void SetHudLayout(Character? player, bool update = false) {
            if (update && player != null) {
                this.Update(player);
            }

            var layoutId = this.CalculateCurrentHud();
            if (layoutId == Guid.Empty) {
                return; // FIXME: do something better
            }

            if (!this.Plugin.Config.Layouts.ContainsKey(layoutId)) {
                return; // FIXME: do something better
            }

            this.Plugin.Hud.WriteEffectiveLayout(this.Plugin.Config.StagingSlot, layoutId);
            this.Plugin.Hud.SelectSlot(this.Plugin.Config.StagingSlot, true);
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
    }

    // Note: Changing the names of these is a breaking change
    public enum Status {
        InCombat = ConditionFlag.InCombat,
        WeaponDrawn = -1,
        InInstance = ConditionFlag.BoundByDuty,
        Crafting = ConditionFlag.Crafting,
        Gathering = ConditionFlag.Gathering,
        Fishing = ConditionFlag.Fishing,
        Roleplaying = -2,
        PlayingMusic = -3,
        InPvp = -4
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
                case Status.Roleplaying:
                    return "Roleplaying";
                case Status.PlayingMusic:
                    return "Playing music";
                case Status.InPvp:
                    return "In PvP";
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
            }

            return false;
        }
    }
}
