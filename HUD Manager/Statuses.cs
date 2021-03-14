using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Plugin;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

// TODO: Zone swaps?

namespace HUD_Manager {
    public class Statuses {
        private Plugin Plugin { get; }

        private readonly Dictionary<Status, bool> _condition = new();
        private ClassJob? _job;

        internal static byte GetStatus(Actor actor) {
            return Marshal.ReadByte(actor.Address + 0x1980);
        }

        internal static byte GetOnlineStatus(Actor actor) {
            return Marshal.ReadByte(actor.Address + 0x195F);
        }

        public Statuses(Plugin plugin) {
            this.Plugin = plugin;
        }

        public bool Update(PlayerCharacter? player) {
            if (player == null) {
                return false;
            }

            var anyChanged = false;

            var currentJob = this.Plugin.Interface.Data.GetExcelSheet<ClassJob>().GetRow(player.ClassJob.Id);
            if (this._job != null && this._job != currentJob) {
                anyChanged = true;
            }

            this._job = currentJob;

            foreach (Status status in Enum.GetValues(typeof(Status))) {
                var old = this._condition.ContainsKey(status) && this._condition[status];
                this._condition[status] = status.Active(player, this.Plugin.Interface);
                anyChanged |= old != this._condition[status];
            }

            return anyChanged;
        }

        private Guid CalculateCurrentHud() {
            var player = this.Plugin.Interface.ClientState.LocalPlayer;
            if (player == null) {
                return Guid.Empty;
            }

            foreach (var match in this.Plugin.Config.HudConditionMatches) {
                if ((!match.Status.HasValue || this._condition[match.Status.Value]) &&
                    (match.ClassJob == null || this._job?.Abbreviation == match.ClassJob)) {
                    return match.LayoutId;
                }
            }

            return Guid.Empty;
        }

        public void SetHudLayout(PlayerCharacter? player, bool update = false) {
            if (update && player != null) {
                this.Update(player);
            }

            var layoutId = this.CalculateCurrentHud();
            if (layoutId == Guid.Empty) {
                return; // FIXME: do something better
            }

            if (!this.Plugin.Config.Layouts.TryGetValue(layoutId, out var layout)) {
                return; // FIXME: do something better
            }

            this.Plugin.Hud.WriteEffectiveLayout(this.Plugin.Config.StagingSlot, layoutId);
            this.Plugin.Hud.SelectSlot(this.Plugin.Config.StagingSlot, true);

            foreach (var entry in layout.Positions) {
                this.Plugin.GameFunctions.MoveWindow(entry.Key, entry.Value.X, entry.Value.Y);
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
            }

            throw new ApplicationException($"No name was set up for {status}");
        }

        public static bool Active(this Status status, PlayerCharacter player, DalamudPluginInterface pi) {
            if (player == null) {
                throw new ArgumentNullException(nameof(player), "PlayerCharacter cannot be null");
            }

            if (status > 0) {
                var flag = (ConditionFlag) status;
                return pi.ClientState.Condition[flag];
            }

            switch (status) {
                case Status.WeaponDrawn:
                    return (Statuses.GetStatus(player) & 4) > 0;
                case Status.Roleplaying:
                    return Statuses.GetOnlineStatus(player) == 22;
            }

            return false;
        }
    }
}
