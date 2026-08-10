using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HUDManager;

public sealed class HudStage : IDisposable {
    private Plugin Plugin { get; }

    public HudDescriptor? Applied { get; private set; }

    private ApplyTask? pendingTask;
    private bool reapplyAfterFade;

    private readonly record struct ApplyTask(ApplyAction Action, HudDescriptor HudDescriptor, StageFlags Flags);

    private enum ApplyAction {
        All,
        Job,
    }

    private enum ApplyDecision : byte {
        Initial,
        LayoutChanged,
        Force,
        Skip,
    }

    public HudStage(Plugin plugin) {
        Plugin = plugin;
        Plugin.Framework.Update += OnFrameworkUpdate;
    }

    public void Dispose() {
        Plugin.Framework.Update -= OnFrameworkUpdate;
    }

    private void OnFrameworkUpdate(IFramework framework) {
        WritePending();
    }

    public void Apply(HudDescriptor desc, StageReason reason, StageFlags flags = StageFlags.None) {
        Apply(desc, reason, ForceStageReason.None, flags);
    }

    public void Apply(HudDescriptor desc, StageReason reason, ForceStageReason forceReason, StageFlags flags = StageFlags.None) {
        ApplyDecision decision;
        if (forceReason != ForceStageReason.None) {
            decision = ApplyDecision.Force;
        } else if (Applied is { } prev) {
            // var sameJob = prev.SameJob(desc.JobId);
            var sameLayers = prev.SameLayers(desc.LayoutId, desc.LayerIds);
            if (!sameLayers) {
                decision = ApplyDecision.LayoutChanged;
                // } else if (!sameJob) {
                //     decision = ApplyDecision.JobChanged;
            } else {
                decision = ApplyDecision.Skip;
            }
        } else {
            decision = ApplyDecision.Initial;
        }

        var decisionChar = decision switch {
            ApplyDecision.Initial => 'I',
            ApplyDecision.LayoutChanged => 'L',
            ApplyDecision.Force => 'F',
            ApplyDecision.Skip => '-',
            _ => throw new ArgumentOutOfRangeException($"Unknown decision: {decision}")
        };

        char writeChar;
        if (decision == ApplyDecision.Skip) {
            writeChar = '-';
        } else {
            var task = new ApplyTask(ApplyAction.All, desc, flags);
            if (Write(task)) {
                writeChar = 'W';
                if (IsUiFading()) {
                    writeChar = 'w';
                    reapplyAfterFade = true;
                }
            } else {
                writeChar = '>';
                pendingTask = task;
            }
        }

        if (decision == ApplyDecision.Force) {
            Plugin.Log.Debug($"Stage {decisionChar}{writeChar} {GetDebugName(desc)} [{reason}] F=[{forceReason}]");
        } else {
            Plugin.Log.Debug($"Stage {decisionChar}{writeChar} {GetDebugName(desc)} [{reason}]");
        }
    }

    private void WritePending() {
        if (reapplyAfterFade && Plugin.HudLock.CanApply() && !IsUiFading()) {
            if (Applied is { } applied) {
                Plugin.Log.Debug($"Stage -A {GetDebugName(applied)}");
                Hud.ApplyHudLayout();
            }
            reapplyAfterFade = false;
            return;
        }

        if (pendingTask is not { } task)
            return;

        if (Write(task)) {
            var writeChar = 'W';
            if (IsUiFading()) {
                writeChar = 'w';
                reapplyAfterFade = true;
            }

            Plugin.Log.Debug($"Stage -{writeChar} {GetDebugName(task.HudDescriptor)}");
            pendingTask = null;
        }
    }

    private bool Write(ApplyTask task) {
        if (task.Flags.HasFlag(StageFlags.IgnoreEditLock)) {
            if (!Plugin.HudLock.CanApply())
                return false;
        } else if (!Plugin.HudLock.CanSwap()) {
            return false;
        }

        var changeSlot = task.Flags.HasFlag(StageFlags.ChangeSlot);
        if (!changeSlot && Hud.GetActiveHudSlot() != Plugin.Config.StagingSlot) {
            return false;
        }

        var layout = Plugin.Hud.GetEffectiveLayout(task.HudDescriptor.LayoutId, task.HudDescriptor.LayerIds);
        if (layout is null)
            return false;

        if (changeSlot)
            Hud.ChangeSlot(Plugin.Config.StagingSlot);

        if (task.Action == ApplyAction.Job) {
            Plugin.Hud.ApplyAllJobGaugeVisibility(layout);
        } else if (task.Action == ApplyAction.All) {
            Plugin.Hud.WriteAll(Plugin.Config.StagingSlot, layout);
        }

        Applied = task.HudDescriptor;

        return true;
    }

    private string GetDebugName(HudDescriptor desc) {
        if (desc.LayerIds.Count > 0) {
            return $"[{GetLayoutName(desc.LayoutId)}|{(string.Join(",", desc.LayerIds.Select(GetLayoutName)))}] ({desc.JobId})";
        }
        return $"[{GetLayoutName(desc.LayoutId)}] ({desc.JobId})";
    }

    private string GetLayoutName(Guid id) => Plugin.Config.Layouts.GetValueOrDefault(id)?.Name ?? id.ToString();

    private static unsafe bool IsUiFading()
        => RaptureAtkUnitManager.Instance()->IsUiFading;
        // => false;
}

[Flags]
public enum StageReason {
    None = 0,
    Command = 1 << 0,
    StatusChanged = 1 << 1,
    CustomConditionChanged = 1 << 2,
    KeyStateChanged = 1 << 3,
    QoLBarConditionChanged = 1 << 4,
    StatusTimerExpired = 1 << 5,
    TerritoryChanged = 1 << 6,
}

[Flags]
public enum ForceStageReason {
    None = 0,
    Editing = 1 << 0,
    EditLockRemoved = 1 << 1,
    SwapEnabled = 1 << 2,
    SwapConfigurationChanged = 1 << 3,
}

[Flags]
public enum StageFlags {
    None = 0,
    ChangeSlot = 1 << 0,
    IgnoreEditLock = 1 << 1,
}

public record HudDescriptor(uint JobId, Guid LayoutId, List<Guid> LayerIds) {
    public bool SameLayers(Guid layoutId, List<Guid> layerIds) => LayoutId == layoutId && LayerIds.SequenceEqual(layerIds);
    // public bool SameJob(uint playerJobId) => JobId == playerJobId;
}
