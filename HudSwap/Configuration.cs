using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;

namespace HudSwap {
    [Serializable]
    public class Configuration : IPluginConfiguration {
        public int Version { get; set; } = 1;

        [NonSerialized]
        private DalamudPluginInterface pi;

        public bool FirstRun { get; set; } = true;
        public bool UnderstandsRisks { get; set; } = false;

        public bool SwapsEnabled { get; set; } = false;

        public HudSlot StagingSlot { get; set; } = HudSlot.Four;

        public Guid defaultLayout = Guid.Empty;

        public Guid combatLayout = Guid.Empty;
        public Guid weaponDrawnLayout = Guid.Empty;
        public Guid instanceLayout = Guid.Empty;
        public Guid craftingLayout = Guid.Empty;
        public Guid gatheringLayout = Guid.Empty;
        public Guid fishingLayout = Guid.Empty;
        public Guid roleplayingLayout = Guid.Empty;

        public Dictionary<string, Guid> JobLayouts { get; set; } = new Dictionary<string, Guid>();
        public bool HighPriorityJobs { get; set; } = false;
        public bool JobsCombatOnly { get; set; } = false;

        public Dictionary<Guid, Tuple<string, byte[]>> Layouts { get; } = new Dictionary<Guid, Tuple<string, byte[]>>();

        public void Initialize(DalamudPluginInterface pluginInterface) {
            this.pi = pluginInterface;
        }

        public void Save() {
            this.pi.SavePluginConfig(this);
        }
    }
}
