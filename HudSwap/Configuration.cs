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

        public bool SwapsEnabled { get; set; } = false;

        public Guid defaultLayout = Guid.Empty;

        public Guid combatLayout = Guid.Empty;
        public Guid weaponDrawnLayout = Guid.Empty;
        public Guid instanceLayout = Guid.Empty;
        public Guid craftingLayout = Guid.Empty;
        public Guid gatheringLayout = Guid.Empty;
        public Guid fishingLayout = Guid.Empty;

        public Dictionary<Guid, Tuple<string, byte[]>> Layouts { get; } = new Dictionary<Guid, Tuple<string, byte[]>>();

        public void Initialize(DalamudPluginInterface pluginInterface) {
            this.pi = pluginInterface;
        }

        public void Save() {
            this.pi.SavePluginConfig(this);
        }
    }
}
