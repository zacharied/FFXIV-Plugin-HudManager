using Dalamud.Configuration;
using Dalamud.Plugin;
using HUDManager.Configuration;
using System;
using System.Collections.Generic;

namespace HUD_Manager.Configuration
{
    [Serializable]
    public class Config : IPluginConfiguration
    {
        public const int LatestVersion = 6;

        public int Version { get; set; } = LatestVersion;

        private DalamudPluginInterface Interface { get; set; } = null!;

        public bool FirstRun { get; set; } = true;
        public bool UnderstandsRisks { get; set; }

        public bool SwapsEnabled { get; set; }

        public bool AdvancedSwapMode { get; set; }

        [Obsolete("No need to use this as a jank fix anymore.")]
        public bool PreventSwapsWhilePetHotbarActive { get; set; } = true;

        public bool DisableHelpPanels { get; set; } = false;

        public HudSlot StagingSlot { get; set; } = HudSlot.Four;

        public PositioningMode PositioningMode { get; set; } = PositioningMode.Percentage;

        public Dictionary<Guid, SavedLayout> Layouts { get; } = new();

        public List<HudConditionMatch> HudConditionMatches { get; } = new();

        public List<CustomCondition> CustomConditions { get; } = new();

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.Interface = pluginInterface;
        }

        public void Save()
        {
            this.Interface.SavePluginConfig(this);
        }
    }
}
