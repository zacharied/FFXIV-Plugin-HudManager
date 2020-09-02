using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Plugin;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

// TODO: Zone swaps?

namespace HudSwap {
    public class Statuses {
        private readonly HudSwapPlugin plugin;
        private readonly DalamudPluginInterface pi;

        private readonly Dictionary<Status, bool> condition = new Dictionary<Status, bool>();
        private ClassJob job;

        internal static byte GetStatus(DalamudPluginInterface pi, Actor actor) {
            IntPtr statusPtr = pi.TargetModuleScanner.ResolveRelativeAddress(actor.Address, 0x1906);
            return Marshal.ReadByte(statusPtr);
        }

        public Statuses(HudSwapPlugin plugin, DalamudPluginInterface pi) {
            this.plugin = plugin;
            this.pi = pi;
        }

        public bool Update(PlayerCharacter player) {
            if (player == null) {
                return false;
            }

            bool anyChanged = false;

            ClassJob currentJob = this.pi.Data.GetExcelSheet<ClassJob>().GetRow(player.ClassJob.Id);
            if (this.job != null && this.job != currentJob) {
                anyChanged = true;
            }
            this.job = currentJob;

            foreach (Status status in Enum.GetValues(typeof(Status))) {
                var old = this.condition.ContainsKey(status) && this.condition[status];
                this.condition[status] = status.Active(player, this.pi);
                anyChanged |= old != this.condition[status];
            }

            return anyChanged;
        }

        public Guid CalculateCurrentHud() {
            PlayerCharacter player = this.pi.ClientState.LocalPlayer;
            if (player == null) {
                return Guid.Empty;
            }

            foreach (var match in this.plugin.Config.HudConditionMatches) {
                if ((!match.Status.HasValue || this.condition[match.Status.Value]) &&
                    (match.ClassJob == null || this.job.Abbreviation == match.ClassJob))
                    return match.LayoutId;
            }

            return this.plugin.Config.DefaultLayout;
        }

        public void SetHudLayout(PlayerCharacter player, bool update = false) {
            if (update && player != null) {
                this.Update(player);
            }

            Guid layoutId = this.CalculateCurrentHud();
            if (layoutId == Guid.Empty) {
                return; // FIXME: do something better
            }
            if (!this.plugin.Config.Layouts2.TryGetValue(layoutId, out Layout layout)) {
                return; // FIXME: do something better
            }
            this.plugin.Hud.WriteLayout(this.plugin.Config.StagingSlot, layout.Hud);
            this.plugin.Hud.SelectSlot(this.plugin.Config.StagingSlot, true);

            foreach (KeyValuePair<string, Vector2<short>> entry in layout.Positions) {
                this.plugin.GameFunctions.MoveWindow(entry.Key, entry.Value.X, entry.Value.Y);
            }
        }
    }

    public struct HudConditionMatch {
        /// <summary>
        /// Values stored here should be the abbreviation of the class/job name (all caps).
        /// We do this because using <see cref="ClassJob"/> results in circular dependency errors when serializing.
        /// </summary>
        public string ClassJob { get; set; }

        public Status? Status { get; set; }

        public Guid LayoutId { get; set; }
    }

    public enum Status {
        InCombat = ConditionFlag.InCombat,
        WeaponDrawn = ConditionFlag.None,
        InInstance = ConditionFlag.BoundByDuty,
        Crafting = ConditionFlag.Crafting,
        Gathering = ConditionFlag.Gathering,
        Fishing = ConditionFlag.Fishing,
        Roleplaying = ConditionFlag.RolePlaying,
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
            if (pi == null) {
                throw new ArgumentNullException(nameof(pi), "DalamudPluginInterface cannot be null");
            }

            ConditionFlag flag = (ConditionFlag)status;
            if (flag != ConditionFlag.None) {
                return pi.ClientState.Condition[flag];
            }

            switch (status) {
                case Status.WeaponDrawn:
                    return (Statuses.GetStatus(pi, player) & 4) > 0;
            }
            return false;
        }
    }
}
