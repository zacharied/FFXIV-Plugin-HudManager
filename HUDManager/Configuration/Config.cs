using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;

namespace HUDManager.Configuration;

[Serializable]
public class Config : IPluginConfiguration
{
    public const int LatestVersion = 7;

    public int Version { get; set; } = LatestVersion;

    private IDalamudPluginInterface Interface { get; set; } = null!;

    public bool FirstRun { get; set; } = true;
    public bool UnderstandsRisks { get; set; }

    public bool SwapsEnabled { get; set; }

    public bool AdvancedSwapMode { get; set; }

    public bool UseLayoutListTreeView { get; set; }

    public bool DisableHelpPanels { get; set; }

    public HudSlot StagingSlot { get; set; } = HudSlot.Four;

    public PositioningMode PositioningMode { get; set; } = PositioningMode.Percentage;

    public float DragSpeed { get; set; } = 1f;

    public OrderedDictionary<Guid, SavedLayout> Layouts { get; } = new();

    public List<HudConditionMatch> HudConditionMatches { get; } = [];

    public List<CustomCondition> CustomConditions { get; } = [];

    public void Initialize(IDalamudPluginInterface pluginInterface)
    {
        Interface = pluginInterface;
    }

    public void Save()
    {
        Interface.SavePluginConfig(this);
    }
}
