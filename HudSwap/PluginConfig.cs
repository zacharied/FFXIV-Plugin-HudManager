using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;

namespace HudSwap {
    [Serializable]
    public class PluginConfig : IPluginConfiguration {
        public int Version { get; set; } = 1;

        [NonSerialized]
        private DalamudPluginInterface pi;

        public bool FirstRun { get; set; } = true;
        public bool UnderstandsRisks { get; set; } = false;

        public bool SwapsEnabled { get; set; } = false;

        public HudSlot StagingSlot { get; set; } = HudSlot.Four;

        public Guid DefaultLayout { get; set; } = Guid.Empty;

#pragma warning disable CA1051 // Do not declare visible instance fields
        [Obsolete("Individual layout fields are deprecated; use StatusLayouts instead")]
        public Guid combatLayout = Guid.Empty;
        [Obsolete("Individual layout fields are deprecated; use StatusLayouts instead")]
        public Guid weaponDrawnLayout = Guid.Empty;
        [Obsolete("Individual layout fields are deprecated; use StatusLayouts instead")]
        public Guid instanceLayout = Guid.Empty;
        [Obsolete("Individual layout fields are deprecated; use StatusLayouts instead")]
        public Guid craftingLayout = Guid.Empty;
        [Obsolete("Individual layout fields are deprecated; use StatusLayouts instead")]
        public Guid gatheringLayout = Guid.Empty;
        [Obsolete("Individual layout fields are deprecated; use StatusLayouts instead")]
        public Guid fishingLayout = Guid.Empty;
        [Obsolete("Individual layout fields are deprecated; use StatusLayouts instead")]
        public Guid roleplayingLayout = Guid.Empty;
#pragma warning restore CA1051 // Do not declare visible instance fields

        public Dictionary<Status, Guid> StatusLayouts { get; } = new Dictionary<Status, Guid>();

        public Dictionary<string, Guid> JobLayouts { get; } = new Dictionary<string, Guid>();
        public bool HighPriorityJobs { get; set; } = false;
        public bool JobsCombatOnly { get; set; } = false;

        public Dictionary<Guid, Tuple<string, byte[]>> Layouts { get; } = new Dictionary<Guid, Tuple<string, byte[]>>();

        public void Initialize(DalamudPluginInterface pluginInterface) {
            this.pi = pluginInterface;
            this.Migrate();
            this.Save();
        }

        public void Save() {
            this.pi.SavePluginConfig(this);
        }

        private void Migrate() {
#pragma warning disable 618
            if (this.combatLayout != Guid.Empty) {
                this.StatusLayouts[Status.InCombat] = this.combatLayout;
                this.combatLayout = Guid.Empty;
            }

            if (this.weaponDrawnLayout != Guid.Empty) {
                this.StatusLayouts[Status.WeaponDrawn] = this.weaponDrawnLayout;
                this.weaponDrawnLayout = Guid.Empty;
            }

            if (this.instanceLayout != Guid.Empty) {
                this.StatusLayouts[Status.InInstance] = this.instanceLayout;
                this.instanceLayout = Guid.Empty;
            }

            if (this.craftingLayout != Guid.Empty) {
                this.StatusLayouts[Status.Crafting] = this.craftingLayout;
                this.craftingLayout = Guid.Empty;
            }

            if (this.gatheringLayout != Guid.Empty) {
                this.StatusLayouts[Status.Gathering] = this.gatheringLayout;
                this.gatheringLayout = Guid.Empty;
            }

            if (this.fishingLayout != Guid.Empty) {
                this.StatusLayouts[Status.Fishing] = this.fishingLayout;
                this.fishingLayout = Guid.Empty;
            }

            if (this.roleplayingLayout != Guid.Empty) {
                this.StatusLayouts[Status.Roleplaying] = this.roleplayingLayout;
                this.roleplayingLayout = Guid.Empty;
            }
#pragma warning restore 618
        }
    }
}
