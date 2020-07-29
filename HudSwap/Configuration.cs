using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace HudSwap {
    [Serializable]
    public class Configuration : IPluginConfiguration {
        public int Version { get; set; } = 1;

        [NonSerialized]
        private DalamudPluginInterface pi;

        public uint DefaultLayout { get; set; } = 0;

        public bool ChangeOnCombat { get; set; } = false;
        public uint CombatLayout { get; set; } = 0;

        public bool ChangeOnWeaponDrawn { get; set; } = false;
        public uint WeaponDrawnLayout { get; set; } = 0;

        public bool ChangeOnInstance { get; set; } = false;
        public uint InstanceLayout { get; set; } = 0;

        public void Initialize(DalamudPluginInterface pluginInterface) {
            this.pi = pluginInterface;
        }

        public void Save() {
            this.pi.SavePluginConfig(this);
        }
    }
}
