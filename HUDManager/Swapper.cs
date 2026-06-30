using Dalamud.Plugin.Services;
using HUDManager.Configuration;
using System;

namespace HUDManager;

public sealed class Swapper : IDisposable {
    private Plugin Plugin { get; }

    public ForceStageReason PendingForceUpdate { get; set; } = ForceStageReason.None;
    public StageReason PendingUpdate { get; set; } = StageReason.None;

    private bool editLockRemoved;

    public Swapper(Plugin plugin) {
        Plugin = plugin;

        Plugin.Framework.Update += OnFrameworkUpdate;
        Plugin.ClientState.TerritoryChanged += OnTerritoryChange;
        Plugin.HudLock.OnEditLockRemoved += OnEditLockRemoved;
    }

    private void OnEditLockRemoved() {
        editLockRemoved = true;
    }

    private bool CheckEditLockRemoved() {
        if (editLockRemoved) {
            editLockRemoved = false;
            return true;
        }
        return false;
    }

    public void Dispose() {
        Plugin.Framework.Update -= OnFrameworkUpdate;
        Plugin.ClientState.TerritoryChanged -= OnTerritoryChange;
    }

    private void OnTerritoryChange(uint u) {
        if (!Plugin.Ready)
            return;

        PendingUpdate = StageReason.TerritoryChanged;
    }

    private void OnFrameworkUpdate(IFramework framework) {
        if (!Plugin.HudLock.CanSwap() || !Plugin.Config.SwapsEnabled)
            return;

        // Update

        var update = StageReason.None;

        if (Plugin.Statuses.UpdateConditionHoldTimers())
            update |= StageReason.StatusTimerExpired;

        if (Plugin.Statuses.Update())
            update |= StageReason.StatusChanged;

        if (Plugin.Keybinder.UpdateKeyState())
            update |= StageReason.KeyStateChanged;

        if (Plugin.Statuses.CustomConditionStatus.IsUpdated())
            update |= StageReason.CustomConditionChanged;

        if (CheckQoLBarConditions())
            update |= StageReason.QoLBarConditionChanged;

        if (PendingUpdate != StageReason.None) {
            update |= PendingUpdate;
            PendingUpdate = StageReason.None;
        }

        // Force update

        var forceUpdate = ForceStageReason.None;

        if (CheckEditLockRemoved())
            forceUpdate |= ForceStageReason.EditLockRemoved;

        if (PendingForceUpdate != ForceStageReason.None) {
            forceUpdate |= PendingForceUpdate;
            PendingForceUpdate = ForceStageReason.None;
        }

        // Apply

        if (update != StageReason.None || forceUpdate != ForceStageReason.None) {
            if (Plugin.Statuses.CalculateLayout() is { } desc) {
                Plugin.HudStage.Apply(desc, update, forceUpdate);
            }
        }
    }

    private bool CheckQoLBarConditions() {
        var updated = false;
        foreach (var cond in Plugin.Config.CustomConditions) {
            if (cond.ConditionType == CustomConditionType.QoLBarCondition) {
                var state = Plugin.QoLBarIpc.GetConditionChange(cond.ExternalIndex, out var oldState);
                if (state != oldState) {
                    // Plugin.Log.Warning($"changed! index={cond.ExternalIndex} old={oldState} new={state}");
                    updated = true;
                }
            }
        }
        return updated;
    }
}
