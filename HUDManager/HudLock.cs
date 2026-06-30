using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using System;

namespace HUDManager;

public sealed class HudLock : IDisposable {
    private Plugin Plugin { get; }

    private bool EditLock { get; set; }

    public event Action? OnEditLockRemoved;

    public HudLock(Plugin plugin) {
        Plugin = plugin;
        Plugin.Framework.Update += OnUpdate;
    }

    public void Dispose() {
        Plugin.Framework.Update -= OnUpdate;
    }

    public BlockReason PluginBlockReason = BlockReason.None;
    public BlockReason EvaluateBlockReason = BlockReason.None;
    public BlockReason WriteBlockReason = BlockReason.None;
    public BlockReason StageBlockReason = BlockReason.None;
    public BlockReason SwapBlockReason = BlockReason.None;

    public bool CanUsePlugin() => PluginBlockReason == BlockReason.None;
    public bool CanEvaluate() => EvaluateBlockReason == BlockReason.None;
    public bool CanWrite() => WriteBlockReason == BlockReason.None;
    public bool CanApply() => StageBlockReason == BlockReason.None;
    public bool CanSwap() => SwapBlockReason == BlockReason.None;

    public bool SetEditLock(bool value) {
        if (EditLock && !value)
            OnEditLockRemoved?.Invoke();

        var oldValue = EditLock;
        EditLock = value;
        return oldValue != value;
    }

    private void OnUpdate(IFramework framework) {
        PluginBlockReason = CheckPlugin();
        if (PluginBlockReason != BlockReason.None) {
            SwapBlockReason = StageBlockReason = WriteBlockReason = EvaluateBlockReason = PluginBlockReason;
            return;
        }

        EvaluateBlockReason = CheckEvaluate();
        if (EvaluateBlockReason != BlockReason.None) {
            SwapBlockReason = StageBlockReason = WriteBlockReason = EvaluateBlockReason;
            return;
        }

        WriteBlockReason = CheckWrite();
        if (WriteBlockReason != BlockReason.None) {
            SwapBlockReason = StageBlockReason = WriteBlockReason;
            return;
        }

        StageBlockReason = CheckStage();
        if (StageBlockReason != BlockReason.None) {
            SwapBlockReason = StageBlockReason;
            return;
        }

        SwapBlockReason = CheckSwap();
    }

    private BlockReason CheckPlugin() {
        if (!Plugin.Ready || !Plugin.Config.UnderstandsRisks)
            return BlockReason.PluginNotReady;

        return BlockReason.None;
    }

    private BlockReason CheckEvaluate() {
        if (!Plugin.PlayerState.IsLoaded || Plugin.ObjectTable.LocalPlayer == null)
            return BlockReason.PlayerUnavailable;

        return BlockReason.None;
    }

    private BlockReason CheckWrite() {
        if (Util.IsCharacterConfigOpen())
            return BlockReason.CharacterConfigOpen;

        return BlockReason.None;
    }

    private BlockReason CheckStage() {
        if (Plugin.Condition[ConditionFlag.OccupiedInCutSceneEvent]
            || Plugin.Condition[ConditionFlag.WatchingCutscene78] // Used in Dalamud's cutscene check
            || Plugin.Condition[ConditionFlag.BoundByDuty95]      // GATE: Air Force One
            || Plugin.Condition[ConditionFlag.PlayingLordOfVerminion]
           )
            return BlockReason.InSpecialEvent;

        // if (Plugin.Condition[ConditionFlag.BetweenAreas51]) // Loading Lord of Verminion?
        //     return BlockReason.InLoadingScreen;

        if (Hud.IsEditingHudLayout())
            return BlockReason.EditingHudLayout;

        if (Hud.GetActiveHudSlot() != Plugin.Config.StagingSlot)
            return BlockReason.NotUsingStagingSlot;

        return BlockReason.None;
    }

    private BlockReason CheckSwap() {
        if (EditLock)
            return BlockReason.EditLock;

        return BlockReason.None;
    }
}

public enum BlockReason {
    None,
    PluginNotReady,
    PlayerUnavailable,
    CharacterConfigOpen,
    EditLock,
    InSpecialEvent,
    InLoadingScreen,
    NotUsingStagingSlot,
    EditingHudLayout
}
