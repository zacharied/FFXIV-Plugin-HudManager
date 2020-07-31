using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Plugin;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Runtime.InteropServices;

// TODO: Zone swaps?

namespace HudSwap {
    public class Statuses {
        public static readonly Status[] ORDER = {
            Status.Roleplaying,
            Status.Fishing,
            Status.Gathering,
            Status.Crafting,
            Status.InInstance,
            Status.WeaponDrawn,
            Status.InCombat,
        };

        private readonly HudSwapPlugin plugin;
        private readonly DalamudPluginInterface pi;

        private readonly bool[] condition = new bool[ORDER.Length];
        private ClassJob job;

        internal static byte GetStatus(DalamudPluginInterface pi, Actor actor) {
            IntPtr statusPtr = pi.TargetModuleScanner.ResolveRelativeAddress(actor.Address, 0x1901);
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

            bool[] old = (bool[])this.condition.Clone();

            bool anyChanged = false;

            ClassJob currentJob = this.pi.Data.GetExcelSheet<ClassJob>().GetRow(player.ClassJob.Id);
            if (this.job != null && this.job != currentJob) {
                anyChanged = true;
            }
            this.job = currentJob;

            for (int i = 0; i < ORDER.Length; i++) {
                Status status = ORDER[i];
                this.condition[i] = status.Active(player, this.pi);
                anyChanged |= old[i] != this.condition[i];
            }

            return anyChanged;
        }

        public Guid CalculateCurrentHud() {
            PlayerCharacter player = this.pi.ClientState.LocalPlayer;
            if (player == null) {
                return Guid.Empty;
            }

            // get the job layout if there is one and check if jobs are high priority
            if (this.plugin.Config.JobLayouts.TryGetValue(this.job.Abbreviation, out Guid jobLayout) && this.plugin.Config.HighPriorityJobs) {
                return jobLayout;
            }

            Guid layout = Guid.Empty;

            // check all status conditions and set layout as appropriate
            for (int i = 0; i < ORDER.Length; i++) {
                if (!this.condition[i]) {
                    continue;
                }
                Status status = ORDER[i];
                if (this.plugin.Config.StatusLayouts.TryGetValue(status, out Guid statusLayout)) {
                    layout = statusLayout;
                }
            }

            // if a job layout is set for the current job
            if (jobLayout != Guid.Empty) {
                // if jobs are combat only and the player is either in combat or has their weapon drawn, use the job layout
                if (this.plugin.Config.JobsCombatOnly && (this.condition[5] || this.condition[6])) {
                    layout = jobLayout;
                }

                // if the layout was going to be default, use job layout unless jobs are not combat only
                if (!this.plugin.Config.JobsCombatOnly && layout == Guid.Empty) {
                    layout = jobLayout;
                }
            }

            return layout == Guid.Empty ? this.plugin.Config.DefaultLayout : layout;
        }

        public void SetHudLayout(PlayerCharacter player, bool update = false) {
            if (update && player != null) {
                this.Update(player);
            }

            Guid layout = this.CalculateCurrentHud();
            if (layout == Guid.Empty) {
                return; // FIXME: do something better
            }
            if (!this.plugin.Config.Layouts.TryGetValue(layout, out Tuple<string, byte[]> entry)) {
                return; // FIXME: do something better
            }
            this.plugin.Hud.WriteLayout(this.plugin.Config.StagingSlot, entry.Item2);
            this.plugin.Hud.SelectSlot(this.plugin.Config.StagingSlot, true);
        }
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
